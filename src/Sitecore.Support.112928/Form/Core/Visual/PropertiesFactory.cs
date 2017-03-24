using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Attributes;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Form.Core.Visual;
using Sitecore.Resources;
using Sitecore.StringExtensions;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Shared;
using Attribute = System.Attribute;
using ValidationAttribute = Sitecore.Form.Core.Attributes.ValidationAttribute;

namespace Sitecore.Support.Form.Core.Visual
{
  public class PropertiesFactory
  {
    [Obsolete("Use FieldSetEnd. Access modifier will be changed to private")] protected static string fieldSetEnd =
      "</fieldset>";

    [Obsolete("Use FieldSetStart. Access modifier will be changed to private")] protected static string fieldSetStart =
      "<fieldset class=\"sc-accordion-header\"><legend class=\"sc-accordion-header-left\"><span class=\"sc-accordion-header-center\">{0}<strong>{1}</strong><div class=\"sc-accordion-header-right\">&nbsp;</div></span></legend>";

    [Obsolete("Use Infos. Access modifier will be changed to private")] protected static Hashtable infos =
      new Hashtable();

    private readonly Item item;
    private readonly IItemRepository itemRepository;
    private readonly IResourceManager resourceManager;

    [Obsolete("Use another constructor")]
    public PropertiesFactory(Item item)
      : this(item, DependenciesManager.Resolve<IItemRepository>(), DependenciesManager.Resolve<IResourceManager>())
    {
    }

    public PropertiesFactory(Item item, IItemRepository itemRepository, IResourceManager resourceManager)
    {
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNull(itemRepository, "itemRepository");
      Assert.ArgumentNotNull(resourceManager, "resourceManager");
      this.item = item;
      this.itemRepository = itemRepository;
      this.resourceManager = resourceManager;
    }

    public static string FieldSetEnd
    {
      get
      {
        return
          fieldSetEnd;
      }
      set { fieldSetEnd = value; }
    }

    public static string FieldSetStart
    {
      get
      {
        return
          fieldSetStart;
      }
      set { fieldSetStart = value; }
    }

    protected static Hashtable Infos
    {
      get
      {
        return
          infos;
      }
      set { infos = value; }
    }

    internal static IEnumerable<string> CompareTypes(IEnumerable<Pair<string, string>> properties, Item newType,
      Item oldType, ID assemblyField, ID classField)
    {
      var source = properties as Pair<string, string>[] ?? properties.ToArray();
      if ((properties == null) || !source.Any())
        return new string[0];
      Func<Pair<string, string>, bool> predicate = null;
      var newTypeInfos =
        new PropertiesFactory(newType, DependenciesManager.Resolve<IItemRepository>(),
          DependenciesManager.Resolve<IResourceManager>()).GetProperties(assemblyField, classField);
      var oldTypeInfos =
        new PropertiesFactory(oldType, DependenciesManager.Resolve<IItemRepository>(),
          DependenciesManager.Resolve<IResourceManager>()).GetProperties(assemblyField, classField);
      IEnumerable<string> enumerable = new string[0];
      if (oldTypeInfos.Count > 0)
      {
        if (predicate == null)
          predicate =
            p =>
              (oldTypeInfos.FirstOrDefault(s => s.PropertyName.ToLower() == p.Part1.ToLower()) != null) &&
              (oldTypeInfos.FirstOrDefault(s => s.PropertyName.ToLower() == p.Part1.ToLower()).DefaultValue.ToLower() !=
               p.Part2.ToLower());
        enumerable = from p in source.Where(predicate) select p.Part1.ToLower();
      }
      return from f in enumerable
        where newTypeInfos.Find(s => s.PropertyName.ToLower() == f) == null
        select oldTypeInfos.Find(s => s.PropertyName.ToLower() == f).DisplayName.TrimEnd(' ', ':');
    }

    protected VisualPropertyInfo[] GetClassDefinedProperties(ICustomAttributeProvider type)
    {
      var list = new List<VisualPropertyInfo>();
      if (type != null)
      {
        var customAttributes = type.GetCustomAttributes(typeof(VisualPropertiesAttribute), true);
        if (customAttributes.Length != 0)
        {
          var attribute = customAttributes[0] as VisualPropertiesAttribute;
          if (attribute != null)
          {
            var properties = attribute.Properties;
            list.AddRange(properties.Select(VisualPropertyInfo.Parse));
          }
        }
      }
      return list.ToArray();
    }

    private string GetDisplayNameAttributeValue(MemberInfo t)
    {
      var customAttribute = t.GetCustomAttribute<DisplayNameAttribute>();
      if (customAttribute != null)
        return customAttribute.DisplayName;
      return t.Name;
    }

    protected VisualPropertyInfo[] GetMvcCustomErrorMessageProperties(Type type)
    {
      var list = new List<VisualPropertyInfo>();
      var list2 = new List<Attribute>();
      foreach (var info in type.GetProperties())
        list2.AddRange(from a in Attribute.GetCustomAttributes(info)
          where
          (a.GetType().BaseType != typeof(ValidationAttribute)) && (a.GetType().BaseType != typeof(DataTypeAttribute)) &&
          a is ValidationAttribute
          select a);
      var mvcValidationMessages = itemRepository.CreateFieldItem(item).MvcValidationMessages;
      list.AddRange(from g in from x in list2 group x by x.ToString()
        select g.First()
        into a
        select
        VisualPropertyInfo.Parse(a.GetType().Name, GetDisplayNameAttributeValue(a.GetType()),
          mvcValidationMessages.FirstOrDefault(m => m.Key == a.GetType().Name).Value, "VALIDATION_ERROR_MESSAGES", true));
      return list.ToArray();
    }

    protected List<VisualPropertyInfo> GetProperties(ID assemblyField, ID classField)
    {
      Assert.ArgumentNotNull(assemblyField, "assemblyField");
      Assert.ArgumentNotNull(classField, "classField");
      var key = item[assemblyField] + item[classField] +
                item[Sitecore.Form.Core.Configuration.FieldIDs.FieldUserControlID];
      if (Infos.ContainsKey(key))
        return Infos[key] as List<VisualPropertyInfo>;
      var list = new List<VisualPropertyInfo>();
      var type = FieldReflectionUtil.GetFieldType(item[assemblyField], item[classField],
        item[Sitecore.Form.Core.Configuration.FieldIDs.FieldUserControlID]);
      if (type != null)
      {
        list.AddRange(GetClassDefinedProperties(type));
        list.AddRange(GetPropertyDefinedProperties(type));
      }
      var str2 = item[Sitecore.Form.Core.Configuration.FieldIDs.MvcFieldId];
      if (!string.IsNullOrEmpty(str2))
      {
        var type2 = Type.GetType(str2);
        if (type2 != null)
          list.AddRange(GetMvcCustomErrorMessageProperties(type2));
      }
      list.Sort(new CategoryComparer());
      Infos.Add(key, list);
      return list;
    }

    protected VisualPropertyInfo[] GetPropertyDefinedProperties(Type type)
    {
      var list = new List<VisualPropertyInfo>();
      if (type != null)
      {
        var properties = type.GetProperties();
        list.AddRange(from property in properties.Select(VisualPropertyInfo.Parse)
          where property != null
          select property);
      }
      return list.ToArray();
    }

    protected string RenderCategoryBegin(string name)
    {
      var builder = new ImageBuilder
      {
        Width = 0x10,
        Height = 0x10,
        Border = "0",
        Align = "middle",
        Class = "sc-accordion-icon",
        Src = Themes.MapTheme("Applications/16x16/document_new.png", string.Empty, false)
      };
      object[] parameters = {builder.ToString(), Translate.Text(name) ?? string.Empty};
      return FieldSetStart.FormatWith(parameters);
    }

    protected string RenderCategoryEnd() =>
      FieldSetEnd;

    protected string RenderPropertiesEditor(IEnumerable<VisualPropertyInfo> properties)
    {
      var builder = new StringBuilder();
      builder.Append("<div class=\"scFieldProperties\" id=\"FieldProperties\" vAling=\"top\">");
      var source = properties as VisualPropertyInfo[] ?? properties.ToArray();
      if (!source.Any())
      {
        builder.Append("<div class=\"scFbSettingSectionEmpty\">");
        builder.AppendFormat("<label class='scFbHasNoPropLabel'>{0}</label>",
          resourceManager.Localize("HAS_NO_PROPERTIES"));
        builder.Append("</div>");
      }
      var category = string.Empty;
      var flag = false;
      foreach (var info in source)
      {
        if (string.IsNullOrEmpty(category) || (category != info.Category))
        {
          if (flag)
          {
            builder.Append("</div>");
            builder.Append(RenderCategoryEnd());
          }
          category = info.Category;
          flag = true;
          builder.Append(RenderCategoryBegin(category));
          builder.Append("<div class='sc-accordion-field-body'>");
        }
        builder.Append(RenderProperty(info));
      }
      builder.Append("</div>");
      return builder.ToString();
    }

    protected string RenderPropertiesEditor(ID assemblyField, ID classField) =>
      RenderPropertiesEditor(GetProperties(assemblyField, classField));

    public static string RenderPropertiesSection(Item item, ID assemblyField, ID classField) =>
      new PropertiesFactory(item, DependenciesManager.Resolve<IItemRepository>(),
        DependenciesManager.Resolve<IResourceManager>()).RenderPropertiesEditor(assemblyField, classField);

    protected string RenderProperty(VisualPropertyInfo info)
    {
      var builder = new StringBuilder();
      builder.Append("<div class='scFbPeEntry'>");
      if (!string.IsNullOrEmpty(info.DisplayName))
      {
        var str = Translate.Text(info.DisplayName);
        var str2 = info.FieldType is EditField ? "scFbPeLabelFullWidth" : "scFbPeLabel";
        builder.AppendFormat("<label class='{0}' for='{1}'>{2}</label>", str2, info.ID, str);
      }
      builder.Append(info.RenderField());
      builder.Append("</div>");
      return builder.ToString();
    }
  }
}