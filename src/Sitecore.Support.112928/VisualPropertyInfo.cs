using Sitecore.Diagnostics;
using Sitecore.Form.Core.Attributes;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Visual;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Shared;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sitecore.Support.Form.Core.Visual
{
  public class VisualPropertyInfo : Control
  {
    private string html;
    private readonly IResourceManager resourceManager;

    public VisualPropertyInfo() : this(DependenciesManager.ResourceManager)
    {
    }

    public VisualPropertyInfo(IResourceManager resourceManager)
    {
      Assert.IsNotNull(resourceManager, "Dependency resourceManager is null");
      this.resourceManager = resourceManager;
    }

    private VisualPropertyInfo(string propertyName, string category, string displayName, string defaultValue, int sortOrder, ValidationType validation, Type fieldType, object[] parameters, bool localize)
    {
      this.FieldType = (IVisualFieldType)fieldType.InvokeMember(null, BindingFlags.CreateInstance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, null, parameters ?? new object[0]);
      if (fieldType == null)
      {
        throw new NotSupportedException(string.Format(this.resourceManager.Localize("NOT_SUPPORT"), fieldType.Name, "IVisualFieldType"));
      }
      this.FieldType.ID = StaticSettings.PrefixId + (localize ? StaticSettings.PrefixLocalizeId : string.Empty) + propertyName;
      this.FieldType.DefaultValue = defaultValue;
      this.FieldType.EmptyValue = defaultValue;
      this.FieldType.Validation = validation;
      this.FieldType.Localize = localize;
      this.DisplayName = displayName;
      this.Category = category;
      this.CategorySortOrder = sortOrder;
      this.PropertyName = propertyName;
    }

    public static Sitecore.Support.Form.Core.Visual.VisualPropertyInfo Parse(PropertyInfo info)
    {
      if ((info == null) || !Attribute.IsDefined(info, typeof(VisualPropertyAttribute), true))
      {
        return null;
      }
      string name = info.Name;
      string displayName = string.Empty;
      ValidationType none = ValidationType.None;
      string category = DependenciesManager.ResourceManager.Localize("APPEARANCE");
      int sortOrder = -1;
      bool localize = false;
      Type fieldType = typeof(EditField);
      string defaultValue = string.Empty;
      object[] parameters = null;
      foreach (object obj2 in info.GetCustomAttributes(true))
      {
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
      }
      return new Sitecore.Support.Form.Core.Visual.VisualPropertyInfo(name, category, displayName, defaultValue, sortOrder, none, fieldType, parameters, localize);
    }

    public static Sitecore.Support.Form.Core.Visual.VisualPropertyInfo Parse(string propertyName) =>
        new Sitecore.Support.Form.Core.Visual.VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize("APPEARANCE"), propertyName, string.Empty, -1, ValidationType.None, typeof(EditField), new object[0], false);

    internal static Sitecore.Support.Form.Core.Visual.VisualPropertyInfo Parse(string propertyName, string displayName, string defaultValue, string category, bool storeInLocalizedParameters) =>
        new Sitecore.Support.Form.Core.Visual.VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize(category), DependenciesManager.ResourceManager.Localize(displayName), defaultValue, -1, ValidationType.None, typeof(EditField), new object[0], storeInLocalizedParameters);

    public virtual string RenderField()
    {
      if (!string.IsNullOrEmpty(this.html) && this.FieldType.IsCacheable)
      {
        return this.html;
      }
      string str = this.FieldType.Render();
      if (this.FieldType.IsCacheable)
      {
        this.html = str;
      }
      return str;
    }

    public string Category { get; private set; }

    public int CategorySortOrder { get; private set; }

    public string DefaultValue
    {
      get
      {
        return this.FieldType.DefaultValue;
      }
      set
      {
        this.FieldType.DefaultValue = value;
      }
    }

    [Obsolete("Use DefaultValue")]
    public string DefaulValue
    {
      get
      {
        return this.DefaultValue;
      }
      set
      {
        this.DefaultValue = value;
      }
    }

    public string DisplayName { get; private set; }

    public IVisualFieldType FieldType { get; private set; }

    public string ID =>
        this.FieldType.ID;

    public string PropertyName { get; private set; }

    public ValidationType Validation =>
        this.FieldType.Validation;
  }
}
