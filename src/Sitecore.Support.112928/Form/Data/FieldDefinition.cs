using System.Diagnostics;
using System.Xml.Serialization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Globalization;
using Sitecore.WFFM.Abstractions.Data;
using Sitecore.Xml.Serialization;

namespace Sitecore.Support.Form.Data
{
  [DebuggerDisplay("Type = {Type}", Name = "{Name}")]
  [XmlRoot("field")]
  public class FieldDefinition : XmlSerializable, IFieldDefinition
  {
    private string clientControlID;

    public FieldDefinition()
    {
      ControlID = string.Empty;
      Deleted = "0";
      FieldID = string.Empty;
      Type = string.Empty;
      Name = string.Empty;
      IsValidate = "0";
      IsTag = "0";
      Properties = string.Empty;
      LocProperties = string.Empty;
      Sortorder = string.Empty;
    }

    public FieldDefinition(Sitecore.Form.Core.Data.FieldDefinition field)
    {
      ControlID = field.ClientControlID;
      Deleted = field.Deleted;
      FieldID = field.FieldID;
      Type = field.Type;
      Name = field.Name;
      IsValidate = field.IsValidate;
      IsTag = field.IsTag;
      Properties = field.Properties;
      LocProperties = field.LocProperties;
      Sortorder = field.Sortorder;
      Conditions = field.Conditions;
    }

    public Item CreateCorrespondingItem(Item parent, Language language)
    {
      Assert.ArgumentNotNull(parent, "parent");
      var database = parent.Database;
      var item = database.GetItem(FieldID, language);
      if (item == null)
      {
        item = database.GetItem(CreateItem(parent).ID, language);
        UpdateItemName(item);
      }
      if (item != null)
      {
        item.Editing.BeginEdit();
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTitleID].Value = Name;
        item.Fields[FieldIDs.Sortorder].Value = Sortorder;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldParametersID].Value = Properties;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLinkTypeID].Value = Type;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldRequiredID].Value = IsValidate;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTagID].Value = IsTag;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ConditionsFieldID].Value = Conditions;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLocalizeParametersID].Value = LocProperties;
        item.Editing.EndEdit();
      }
      UpdateSharedFields(parent, item, database);
      return item;
    }

    public void UpdateSharedFields(Item parent, Item field, Database database)
    {
      Item item;
      var item2 = field ?? parent;
      if (item2 != null)
        item = item2.Database.GetItem(FieldID);
      else
        item = database.GetItem(FieldID);
      if (item != null)
      {
        if ((parent != null) && (item.Parent.ID != parent.ID))
          ItemManager.MoveItem(item, parent);
        item.Editing.BeginEdit();
        item.Fields[FieldIDs.Sortorder].Value = Sortorder;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldParametersID].Value = Properties;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLinkTypeID].Value = Type;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldRequiredID].Value = IsValidate;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTagID].Value = IsTag;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ConditionsFieldID].Value = Conditions;
        item.Editing.EndEdit();
      }
    }

    public bool Active { get; set; }

    [XmlAttribute("cci")]
    public string ClientControlID
    {
      get
      {
        return
          clientControlID ?? ControlID;
      }
      set { clientControlID = value; }
    }

    [XmlAttribute("condition")]
    public string Conditions { get; set; }

    [XmlAttribute("controlid")]
    public string ControlID { get; set; }

    [XmlAttribute("deleted")]
    public string Deleted { get; set; }

    [XmlAttribute("emptyname")]
    public string EmptyName { get; set; }

    [XmlAttribute("id")]
    public string FieldID { get; set; }

    [XmlAttribute("tag")]
    public string IsTag { get; set; }

    [XmlAttribute("validate")]
    public string IsValidate { get; set; }

    [XmlAttribute("locproperties")]
    public string LocProperties { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("properties")]
    public string Properties { get; set; }

    public string Sortorder { get; set; }

    [XmlAttribute("type")]
    public string Type { get; set; }

    private Item CreateItem(Item parent)
    {
      string str;
      if (string.IsNullOrEmpty(Name))
        str = "unknown field";
      else
        str = ItemUtil.ProposeValidItemName(Name);
      if (string.IsNullOrEmpty(str))
        str = "unknown field";
      return ItemManager.CreateItem(str, parent, IDs.FieldTemplateID);
    }

    private void UpdateItemName(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      if (!string.IsNullOrEmpty(Name))
      {
        var str = ItemUtil.ProposeValidItemName(Name);
        if (item.Name != str)
          item.Name = str;
      }
    }
  }
}