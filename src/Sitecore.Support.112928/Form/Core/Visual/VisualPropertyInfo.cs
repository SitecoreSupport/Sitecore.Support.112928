using System;
using System.ComponentModel;
using System.Reflection;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Attributes;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Visual;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Shared;

namespace Sitecore.Support.Form.Core.Visual
{
  public class VisualPropertyInfo : Control
  {
    private readonly IResourceManager resourceManager;
    private string html;

    public VisualPropertyInfo() : this(DependenciesManager.ResourceManager)
    {
    }

    public VisualPropertyInfo(IResourceManager resourceManager)
    {
      Assert.IsNotNull(resourceManager, "Dependency resourceManager is null");
      this.resourceManager = resourceManager;
    }

    private VisualPropertyInfo(string propertyName, string category, string displayName, string defaultValue,
      int sortOrder, ValidationType validation, Type fieldType, object[] parameters, bool localize)
    {
      FieldType =
        (IVisualFieldType)
        fieldType.InvokeMember(null,
          BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
          BindingFlags.DeclaredOnly, null, null, parameters ?? new object[0]);
      if (fieldType == null)
        throw new NotSupportedException(string.Format(resourceManager.Localize("NOT_SUPPORT"), fieldType.Name,
          "IVisualFieldType"));
      FieldType.ID = StaticSettings.PrefixId + (localize ? StaticSettings.PrefixLocalizeId : string.Empty) +
                     propertyName;
      FieldType.DefaultValue = defaultValue;
      FieldType.EmptyValue = defaultValue;
      FieldType.Validation = validation;
      FieldType.Localize = localize;
      DisplayName = displayName;
      Category = category;
      CategorySortOrder = sortOrder;
      PropertyName = propertyName;
    }

    public string Category { get; private set; }

    public int CategorySortOrder { get; private set; }

    public string DefaultValue
    {
      get
      {
        return
          FieldType.DefaultValue;
      }
      set { FieldType.DefaultValue = value; }
    }

    [Obsolete("Use DefaultValue")]
    public string DefaulValue
    {
      get
      {
        return
          DefaultValue;
      }
      set { DefaultValue = value; }
    }

    public string DisplayName { get; private set; }

    public IVisualFieldType FieldType { get; }

    public string ID =>
      FieldType.ID;

    public string PropertyName { get; private set; }

    public ValidationType Validation =>
      FieldType.Validation;

    public static VisualPropertyInfo Parse(PropertyInfo info)
    {
      if ((info == null) || !Attribute.IsDefined(info, typeof(VisualPropertyAttribute), true))
        return null;
      var name = info.Name;
      var displayName = string.Empty;
      var none = ValidationType.None;
      var category = DependenciesManager.ResourceManager.Localize("APPEARANCE");
      var sortOrder = -1;
      var localize = false;
      var fieldType = typeof(EditField);
      var defaultValue = string.Empty;
      object[] parameters = null;
      foreach (var obj2 in info.GetCustomAttributes(true))
        if (obj2 is VisualPropertyAttribute)
        {
          displayName = (obj2 as VisualPropertyAttribute).DisplayName;
          sortOrder = (obj2 as VisualPropertyAttribute).Sortorder;
        }
        else if (obj2 is VisualCategoryAttribute)
        {
          category = (obj2 as VisualCategoryAttribute).Category;
        }
        else if (obj2 is ValidationAttribute)
        {
          none = (obj2 as ValidationAttribute).Validation;
        }
        else if (obj2 is VisualFieldTypeAttribute)
        {
          fieldType = (obj2 as VisualFieldTypeAttribute).FieldType;
          parameters = (obj2 as VisualFieldTypeAttribute).Parameters;
        }
        else if (obj2 is DefaultValueAttribute)
        {
          defaultValue = (obj2 as DefaultValueAttribute).Value.ToString();
        }
        else if (obj2 is LocalizeAttribute)
        {
          localize = true;
        }
      return new VisualPropertyInfo(name, category, displayName, defaultValue, sortOrder, none, fieldType, parameters,
        localize);
    }

    public static VisualPropertyInfo Parse(string propertyName) =>
      new VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize("APPEARANCE"), propertyName,
        string.Empty, -1, ValidationType.None, typeof(EditField), new object[0], false);

    internal static VisualPropertyInfo Parse(string propertyName, string displayName, string defaultValue,
        string category, bool storeInLocalizedParameters) =>
      new VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize(category),
        DependenciesManager.ResourceManager.Localize(displayName), defaultValue, -1, ValidationType.None,
        typeof(EditField), new object[0], storeInLocalizedParameters);

    public virtual string RenderField()
    {
      if (!string.IsNullOrEmpty(html) && FieldType.IsCacheable)
        return html;
      var str = FieldType.Render();
      if (FieldType.IsCacheable)
        html = str;
      return str;
    }
  }
}