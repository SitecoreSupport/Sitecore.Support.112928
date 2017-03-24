using System.Linq;
using System.Threading.Tasks;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Data;
using Sitecore.Form.Core.Utility;
using Sitecore.Globalization;
using FieldDefinition = Sitecore.Support.Form.Data.FieldDefinition;

namespace Sitecore.Support.Forms.Core.Data
{
  internal class FormItemSynchronizer
  {
    private readonly Database database;
    private readonly FormDefinition definition;
    private readonly Language language;
    private Item formItem;

    public FormItemSynchronizer(Database database, Language language, FormDefinition definition)
    {
      Assert.ArgumentNotNull(database, "database");
      Assert.ArgumentNotNull(language, "language");
      Assert.ArgumentNotNull(definition, "definition");
      this.database = database;
      this.language = language;
      this.definition = definition;
    }

    public Item Form
    {
      get
      {
        if ((formItem == null) && !string.IsNullOrEmpty(definition.FormID))
          formItem = database.GetItem(definition.FormID, language);
        return formItem;
      }
    }

    protected bool DeleteFieldIsEmpty(FieldDefinition field)
    {
      if (field == null)
        return false;
      var item = database.GetItem(field.FieldID, language);
      if (field.Deleted == "1")
      {
        if (item != null)
          item.Delete();
        return true;
      }
      if (!string.IsNullOrEmpty(field.Name))
        return false;
      field.Deleted = "1";
      if (item != null)
        Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(item);
      return true;
    }

    protected bool DeleteSectionIsEmpty(SectionDefinition section)
    {
      if (section != null)
      {
        var flag = section.Deleted == "1";
        if (string.IsNullOrEmpty(section.Name))
          if (section.IsHasOnlyEmptyField)
            section.Deleted = "1";
          else
            section.Name = string.Empty;
        if (section.Deleted == "1")
        {
          var item = database.GetItem(section.SectionID, language);
          if (item != null)
            if (flag)
              item.Delete();
            else
              Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(item);
          return true;
        }
      }
      return false;
    }

    public static ID FindMatch(ID oldID, Sitecore.Forms.Core.Data.FormItem oldForm,
      Sitecore.Forms.Core.Data.FormItem newForm)
    {
      Assert.ArgumentNotNull(oldID, "oldID");
      Assert.ArgumentNotNull(oldForm, "oldForm");
      Assert.ArgumentNotNull(newForm, "newForm");
      var item = oldForm.Database.GetItem(oldID);
      if ((item != null) && item.Paths.LongID.Contains(oldForm.ID.ToString()))
      {
        var index = -1;
        if (item.ParentID == oldForm.ID)
        {
          index = oldForm.InnerItem.Children.IndexOf(item);
          if ((index > -1) && (newForm.InnerItem.Children.Count() > index))
            return newForm.InnerItem.Children[index].ID;
        }
        if (item.Parent.ParentID == oldForm.ID)
        {
          index = oldForm.InnerItem.Children.IndexOf(item.Parent);
          var num2 = item.Parent.Children.IndexOf(item);
          if ((index > -1) && (num2 > -1) && (newForm.InnerItem.Children.Count() > index) &&
              (newForm.InnerItem.Children[index].Children.Count() > num2))
            return newForm.InnerItem.Children[index].Children[num2].ID;
        }
      }
      return ID.Null;
    }

    public void Synchronize()
    {
      foreach (SectionDefinition definition in this.definition.Sections)
      {
        Item sectionItem = null;
        if (!DeleteSectionIsEmpty(definition))
          sectionItem = UpdateSection(definition);
        else if (!string.IsNullOrEmpty(definition.SectionID))
          sectionItem = definition.UpdateSharedFields(database, null);
        Parallel.ForEach(definition.Fields.OfType<Sitecore.Form.Core.Data.FieldDefinition>(),
          f => SynchronizeField(sectionItem, new FieldDefinition(f)));
        if ((sectionItem != null) && !sectionItem.HasChildren)
          sectionItem.Delete();
      }
    }

    private void SynchronizeField(Item sectionItem, FieldDefinition field)
    {
      if (!DeleteFieldIsEmpty(field))
        UpdateField(field, sectionItem);
      else
        field.UpdateSharedFields(sectionItem, null, database);
    }

    protected void UpdateField(FieldDefinition field, Item sectionItem)
    {
      Assert.ArgumentNotNull(field, "field");
      field.CreateCorrespondingItem(sectionItem ?? Form, language);
    }

    public static void UpdateIDReferences(Sitecore.Forms.Core.Data.FormItem oldForm,
      Sitecore.Forms.Core.Data.FormItem newForm)
    {
      Assert.ArgumentNotNull(oldForm, "oldForm");
      Assert.ArgumentNotNull(newForm, "newForm");
      newForm.SaveActions = UpdateIDs(newForm.SaveActions, oldForm, newForm);
      newForm.CheckActions = UpdateIDs(newForm.CheckActions, oldForm, newForm);
    }

    private static string UpdateIDs(string text, Sitecore.Forms.Core.Data.FormItem oldForm,
      Sitecore.Forms.Core.Data.FormItem newForm)
    {
      var str = text;
      if (!string.IsNullOrEmpty(str))
        foreach (var id in IDUtil.GetIDs(str))
        {
          var id2 = FindMatch(id, oldForm, newForm);
          if (!ID.IsNullOrEmpty(id2))
            str = str.Replace(id.ToString(), id2.ToString());
        }
      return str;
    }

    protected Item UpdateSection(SectionDefinition section)
    {
      if ((section != null) && (Form != null) &&
          (!string.IsNullOrEmpty(section.SectionID) || definition.IsHasVisibleSection()))
        return section.CreateCorrespondingItem(Form, language);
      return null;
    }
  }
}