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

namespace Sitecore.Support.Form.Core.Visual
{
    public class VisualPropertyInfo : Control
    {
        private readonly IResourceManager resourceManager;

        private string html;

        public string Category
        {
            get;
            private set;
        }

        public int CategorySortOrder
        {
            get;
            private set;
        }

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

        public string DisplayName
        {
            get;
            private set;
        }

        public IVisualFieldType FieldType
        {
            get;
            private set;
        }

        public new string ID
        {
            get
            {
                return this.FieldType.ID;
            }
        }

        public string PropertyName
        {
            get;
            private set;
        }

        public ValidationType Validation
        {
            get
            {
                return this.FieldType.Validation;
            }
        }

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
            this.FieldType = (IVisualFieldType)fieldType.InvokeMember(null, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, null, parameters ?? new object[0]);
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

        public static VisualPropertyInfo Parse(PropertyInfo info)
        {
            if (info == null || !Attribute.IsDefined(info, typeof(VisualPropertyAttribute), true))
            {
                return null;
            }
            string name = info.Name;
            string displayName = string.Empty;
            ValidationType validation = ValidationType.None;
            string category = DependenciesManager.ResourceManager.Localize("APPEARANCE");
            int sortOrder = -1;
            bool localize = false;
            Type fieldType = typeof(EditField);
            string defaultValue = string.Empty;
            object[] parameters = null;
            object[] customAttributes = info.GetCustomAttributes(true);
            for (int i = 0; i < customAttributes.Length; i++)
            {
                object obj = customAttributes[i];
                if (obj is VisualPropertyAttribute)
                {
                    displayName = (obj as VisualPropertyAttribute).DisplayName;
                    sortOrder = (obj as VisualPropertyAttribute).Sortorder;
                }
                else if (obj is VisualCategoryAttribute)
                {
                    category = (obj as VisualCategoryAttribute).Category;
                }
                else if (obj is ValidationAttribute)
                {
                    validation = (obj as ValidationAttribute).Validation;
                }
                else if (obj is VisualFieldTypeAttribute)
                {
                    fieldType = (obj as VisualFieldTypeAttribute).FieldType;
                    parameters = (obj as VisualFieldTypeAttribute).Parameters;
                }
                else if (obj is DefaultValueAttribute)
                {
                    defaultValue = (obj as DefaultValueAttribute).Value.ToString();
                }
                else if (obj is LocalizeAttribute)
                {
                    localize = true;
                }
            }
            return new VisualPropertyInfo(name, category, displayName, defaultValue, sortOrder, validation, fieldType, parameters, localize);
        }

        public static VisualPropertyInfo Parse(string propertyName)
        {
            return new VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize("APPEARANCE"), propertyName, string.Empty, -1, ValidationType.None, typeof(EditField), new object[0], false);
        }

        internal static VisualPropertyInfo Parse(string propertyName, string displayName, string defaultValue, string category, bool storeInLocalizedParameters)
        {
            return new VisualPropertyInfo(propertyName, DependenciesManager.ResourceManager.Localize(category), DependenciesManager.ResourceManager.Localize(displayName), defaultValue, -1, ValidationType.None, typeof(EditField), new object[0], storeInLocalizedParameters);
        }

        public virtual string RenderField()
        {
            if (!string.IsNullOrEmpty(this.html) && this.FieldType.IsCacheable)
            {
                return this.html;
            }
            string result = this.FieldType.Render();
            if (this.FieldType.IsCacheable)
            {
                this.html = result;
            }
            return result;
        }
    }
}
