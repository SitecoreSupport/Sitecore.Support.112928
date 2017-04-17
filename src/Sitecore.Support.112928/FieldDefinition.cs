using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Data;
using Sitecore.Globalization;
using Sitecore.WFFM.Abstractions.Data;
using Sitecore.Xml.Serialization;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Sitecore.Support.Form.Data
{
  [DebuggerDisplay("Type = {Type}", Name = "{Name}"), XmlRoot("field")]
  public class FieldDefinition : XmlSerializable, IFieldDefinition
  {
    private string clientControlID;

    public FieldDefinition()
    {
      this.ControlID = string.Empty;
      this.Deleted = "0";
      this.FieldID = string.Empty;
      this.Type = string.Empty;
      this.Name = string.Empty;
      this.IsValidate = "0";
      this.IsTag = "0";
      this.Properties = string.Empty;
      this.LocProperties = string.Empty;
      this.Sortorder = string.Empty;
    }

    public FieldDefinition(Sitecore.Form.Core.Data.FieldDefinition field)
    {
      this.ControlID = field.ClientControlID;
      this.Deleted = field.Deleted;
      this.FieldID = field.FieldID;
      this.Type = field.Type;
      this.Name = field.Name;
      this.IsValidate = field.IsValidate;
      this.IsTag = field.IsTag;
      this.Properties = field.Properties;
      this.LocProperties = field.LocProperties;
      this.Sortorder = field.Sortorder;
      this.Conditions = field.Conditions;
    }

    public Item CreateCorrespondingItem(Item parent, Language language)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(parent, "parent");
      Database database = parent.Database;
      Item item = database.GetItem(this.FieldID, language);
      if (item == null)
      {
        item = database.GetItem(this.CreateItem(parent).ID, language);
        this.UpdateItemName(item);
      }
      if (item != null)
      {
        item.Editing.BeginEdit();
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTitleID].Value = this.Name;
        item.Fields[Sitecore.FieldIDs.Sortorder].Value = this.Sortorder;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldParametersID].Value = this.Properties;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLinkTypeID].Value = this.Type;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldRequiredID].Value = this.IsValidate;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTagID].Value = this.IsTag;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ConditionsFieldID].Value = this.Conditions;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLocalizeParametersID].Value = this.LocProperties;
        item.Editing.EndEdit();
      }
      this.UpdateSharedFields(parent, item, database);
      return item;
    }

    private Item CreateItem(Item parent)
    {
      string str;
      if (string.IsNullOrEmpty(this.Name))
      {
        str = "unknown field";
      }
      else
      {
        str = ItemUtil.ProposeValidItemName(this.Name);
      }
      if (string.IsNullOrEmpty(str))
      {
        str = "unknown field";
      }
      return ItemManager.CreateItem(str, parent, IDs.FieldTemplateID);
    }

    private void UpdateItemName(Item item)
    {
      Sitecore.Diagnostics.Assert.ArgumentNotNull(item, "item");
      if (!string.IsNullOrEmpty(this.Name))
      {
        string str = ItemUtil.ProposeValidItemName(this.Name);
        if (item.Name != str)
        {
          item.Name = str;
        }
      }
    }

    public void UpdateSharedFields(Item parent, Item field, Database database)
    {
      Item item;
      Item item2 = field ?? parent;
      if (item2 != null)
      {
        item = item2.Database.GetItem(this.FieldID);
      }
      else
      {
        item = database.GetItem(this.FieldID);
      }
      if (item != null)
      {
        if ((parent != null) && (item.Parent.ID != parent.ID))
        {
          ItemManager.MoveItem(item, parent);
        }
        item.Editing.BeginEdit();
        item.Fields[Sitecore.FieldIDs.Sortorder].Value = this.Sortorder;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldParametersID].Value = this.Properties;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldLinkTypeID].Value = this.Type;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldRequiredID].Value = this.IsValidate;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FieldTagID].Value = this.IsTag;
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ConditionsFieldID].Value = this.Conditions;
        item.Editing.EndEdit();
      }
    }

    public bool Active { get; set; }

    [XmlAttribute("cci")]
    public string ClientControlID
    {
      get
      {
        return (this.clientControlID ?? this.ControlID);
      }
      set
      {
        this.clientControlID = value;
      }
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
  }

}