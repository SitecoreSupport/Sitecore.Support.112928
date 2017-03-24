using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.UI;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.ContentEditor.Data;
using Sitecore.Form.Core.Data;
using Sitecore.Forms.Core.Data;
using Sitecore.Globalization;
using Sitecore.WFFM.Abstractions;
using Sitecore.WFFM.Abstractions.ContentEditor;
using Sitecore.WFFM.Abstractions.Data;
using Sitecore.WFFM.Abstractions.Data.Enums;
using Version = Sitecore.Data.Version;

namespace Sitecore.Support.Forms.Core.Data
{
  public class FormItem : CustomItemBase, IFormItem
  {
    private Tracking traking;

    public FormItem(Item innerItem) : base(innerItem)
    {
    }

    public Item AddFormField(string fieldName, string type, bool isValidate)
    {
      Error.AssertString(fieldName, "fieldName", false);
      Error.AssertString(fieldName, "fieldtype", false);
      TemplateItem item = base.InnerItem.Database.GetItem(type);
      if (item != null)
        return ItemManager.CreateItem(fieldName, base.InnerItem, item.ID);
      return null;
    }

    public IFieldItem GetField(ID fieldID)
    {
      var innerItem = base.InnerItem.Database.GetItem(fieldID);
      if (!(innerItem.ParentID == base.InnerItem.ID) && !(innerItem.Parent.ParentID == base.InnerItem.ID))
        return null;
      return new FieldItem(innerItem);
    }

    public IFieldItem[] GetFields(Item section)
    {
      Assert.ArgumentNotNull(section, "section");
      var list = new List<IFieldItem>();
      foreach (Item item in section.Children)
        if (item.TemplateID == IDs.FieldTemplateID)
          list.Add(new FieldItem(item));
      return list.ToArray();
    }

    public Item GetSection(string id) =>
      base.InnerItem.Database.GetItem(ID.Parse(id), base.InnerItem.Language);

    void IFormItem.BeginEdit()
    {
      base.BeginEdit();
    }

    void IFormItem.EndEdit()
    {
      base.EndEdit();
    }

    public IListDefinition ActionsDefinition
    {
      get
      {
        var list = new List<IGroupDefinition>();
        list.AddRange(ListDefinition.Parse(SaveActions).Groups);
        list.AddRange(ListDefinition.Parse(CheckActions).Groups);
        return new ListDefinition {Groups = list};
      }
    }

    public string CheckActions
    {
      get
      {
        return
          base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value;
      }
      set
      {
        base.InnerItem.Editing.BeginEdit();
        base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value = value;
        base.InnerItem.Editing.EndEdit();
      }
    }

    public string CustomCss =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.FormCustomCssClass].Value;

    public IFieldItem[] Fields
    {
      get
      {
        var list = new List<IFieldItem>();
        list.AddRange(GetFields(base.InnerItem));
        foreach (var item in Sections)
          if (base.InnerItem.ID != item.ID)
            list.AddRange(GetFields(item));
        return list.ToArray();
      }
    }

    public string Footer =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormFooterID].Value;

    public string FooterFieldName =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormFooterID].Name;

    public string FormAlignment
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.FormAlignment].Value;
        if (string.IsNullOrEmpty(str))
        {
          if (!string.IsNullOrEmpty(ThemesManager.GetThemeName(base.InnerItem, FormIDs.MvcFormAlignmentID)))
            return string.Empty;
          var item1 = base.Database.GetItem(str);
          if (item1 == null)
            return null;
          return item1["Value"];
        }
        var item = base.Database.GetItem(str);
        if (item == null)
          return null;
        return item["Value"];
      }
    }

    public string FormName
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID].Value;
        if (string.IsNullOrEmpty(str))
          return base.InnerItem.Name;
        return str;
      }
    }

    public FormType FormType
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.FormType].Value;
        if (string.IsNullOrEmpty(str))
        {
          var themeName = ThemesManager.GetThemeName(base.InnerItem, FormIDs.MvcFormTypeID);
          if (string.IsNullOrEmpty(themeName))
            return FormType.Basic;
          return (FormType) Enum.Parse(typeof(FormType), themeName);
        }
        var item = base.Database.GetItem(str);
        if (item == null)
          return FormType.Basic;
        var name = item.Name;
        return (FormType) Enum.Parse(typeof(FormType), name);
      }
    }

    public string FormTypeClass
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.FormType].Value;
        if (string.IsNullOrEmpty(str))
        {
          if (!string.IsNullOrEmpty(ThemesManager.GetThemeName(base.InnerItem, FormIDs.MvcFormTypeID)))
            return string.Empty;
          var item1 = base.Database.GetItem(str);
          if (item1 == null)
            return null;
          return item1["Value"];
        }
        var item = base.Database.GetItem(str);
        if (item == null)
          return null;
        return item["Value"];
      }
    }

    public bool HasSections
    {
      get
      {
        foreach (Item item in base.InnerItem.Children)
          if (item.TemplateID == IDs.SectionTemplateID)
            return true;
        return false;
      }
    }

    public string Introduction =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormIntroductionID].Value;

    public string IntroductionFieldName =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormIntroductionID].Name;

    public bool IsAjaxMvcForm =>
      MainUtil.GetBool(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.IsAjaxMvcForm].Value, false);

    [Required("IsXdbTrackerEnabled", true)]
    public bool IsAnalyticsEnabled =>
      DependenciesManager.RequirementsChecker.CheckRequirements(MethodBase.GetCurrentMethod().GetType()) &&
      !Tracking.Ignore;

    public bool IsDropoutTrackingEnabled =>
      IsAnalyticsEnabled && Tracking.IsDropoutTrackingEnabled;

    public bool IsSaveFormDataToStorage =>
      MainUtil.GetBool(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveFormDataToStorage].Value,
        false);

    public IFieldItem[] this[string section]
    {
      get
      {
        if (!string.IsNullOrEmpty(section) && (section != ID.Null.ToString()))
          return GetFields(GetSection(section));
        return GetFields(base.InnerItem);
      }
    }

    public Language Language =>
      base.InnerItem.Language;

    public string LeftColumnStyle =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.LeftColumnStyle].Value;

    public string Parameters =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.Parameters].Value;

    public string ProfileItem
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ProfileItemId].Value;
        if (string.IsNullOrEmpty(str))
          return "{AE4C4969-5B7E-4B4E-9042-B2D8701CE214}";
        return str;
      }
    }

    public string RightColumnStyle =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.RightColumnStyle].Value;

    public string SaveActions
    {
      get
      {
        return
          base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value;
      }
      set
      {
        base.InnerItem.Editing.BeginEdit();
        base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value = value;
        base.InnerItem.Editing.EndEdit();
      }
    }

    public Item[] SectionItems
    {
      get
      {
        var list = new List<Item>();
        var flag = true;
        foreach (Item item in base.InnerItem.Children)
          if (item.TemplateID == IDs.SectionTemplateID)
          {
            list.Add(item);
          }
          else if (flag && (item.TemplateID == IDs.FieldTemplateID))
          {
            list.Add(base.InnerItem);
            flag = false;
          }
        return list.ToArray();
      }
    }

    public Item[] Sections
    {
      get
      {
        var list = new List<Item>();
        var flag = true;
        foreach (Item item in base.InnerItem.Children)
          if (item.TemplateID == IDs.SectionTemplateID)
          {
            list.Add(item);
          }
          else if (flag && (item.TemplateID == IDs.FieldTemplateID))
          {
            list.Add(base.InnerItem);
            flag = false;
          }
        return list.ToArray();
      }
    }

    public bool ShowFooter =>
      MainUtil.GetBool(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormFooterID].Value, false);

    public bool ShowIntroduction =>
      MainUtil.GetBool(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormIntroID].Value, false);

    public bool ShowTitle =>
      MainUtil.GetBool(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value, false);

    Database IFormItem.Database =>
      base.Database;

    ID IFormItem.ID =>
      base.ID;

    Item IFormItem.InnerItem =>
      base.InnerItem;

    string IFormItem.Name =>
      base.Name;

    public string SubmitButtonPosition
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.SubmitButtonPosition].Value;
        if (string.IsNullOrEmpty(str))
          return string.Empty;
        var item = base.Database.GetItem(str);
        if (item == null)
          return null;
        return item["Value"];
      }
    }

    public string SubmitButtonSize
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.SubmitButtonSize].Value;
        if (string.IsNullOrEmpty(str))
          return string.Empty;
        var item = base.Database.GetItem(str);
        if (item == null)
          return null;
        return item["Value"];
      }
    }

    public string SubmitButtonType
    {
      get
      {
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.Mvc.SubmitButtonType].Value;
        if (string.IsNullOrEmpty(str))
          return string.Empty;
        var item = base.Database.GetItem(str);
        if (item == null)
          return null;
        return item["Value"];
      }
    }

    public string SubmitName
    {
      get
      {
        return
          base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormSubmitID].Value;
      }
      set
      {
        base.InnerItem.Editing.BeginEdit();
        base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormSubmitID].Value = value;
        base.InnerItem.Editing.EndEdit();
      }
    }

    public string SuccessMessage =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessMessageID].Value;

    public LinkField SuccessPage =>
      new LinkField(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID],
        base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID].Value);

    public ID SuccessPageID =>
      new LinkField(base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID]).TargetID;

    public bool SuccessRedirect =>
      base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessModeID].Value ==
      "{F4D50806-6B89-4F2D-89FE-F77FC0A07D48}";

    public HtmlTextWriterTag TitleTag
    {
      get
      {
        HtmlTextWriterTag tag;
        var str = base.InnerItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleTagID].Value;
        if (!string.IsNullOrEmpty(str) && Enum.TryParse(str, out tag))
          return tag;
        return HtmlTextWriterTag.H1;
      }
    }

    public ITracking Tracking =>
      traking ?? (traking = new Tracking(base.InnerItem["__Tracking"], base.InnerItem.Database));

    public ItemUri Uri =>
      base.InnerItem.Uri;

    public Version Version =>
      base.InnerItem.Version;

    public static FormItem GetForm(ID itemID)
    {
      Assert.ArgumentNotNull(itemID, "itemID");
      var innerItem = StaticSettings.ContextDatabase.GetItem(itemID);
      if (innerItem != null)
        return new FormItem(innerItem);
      return null;
    }

    public static FormItem GetForm(string itemID)
    {
      if (!string.IsNullOrEmpty(itemID))
        return new FormItem(StaticSettings.ContextDatabase.GetItem(itemID));
      return null;
    }

    public static bool IsForm(Item item)
    {
      if (item?.TemplateID == IDs.FormTemplateID) return true;
      var list = item.Template.BaseTemplates.ToList();
      var first = list.FirstOrDefault(t => t.ID == IDs.FormTemplateID);
      return first != null;
    }

    public static implicit operator FormItem(Item item)
    {
      if (item != null)
        return new FormItem(item);
      return null;
    }

    public static void UpdateFormItem(Database database, Language language, FormDefinition definition)
    {
      Assert.ArgumentNotNull(definition, "definition");
      Assert.ArgumentNotNull(database, "database");
      Assert.ArgumentNotNull(language, "language");
      new FormItemSynchronizer(database, language, definition).Synchronize();
    }
  }
}