using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.ContentEditor.Data;
using Sitecore.Form.Core.Data;
using Sitecore.Form.Core.Utility;
using Sitecore.Form.Core.Visual;
using Sitecore.Forms.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Forms.Shell.UI.Dialogs;
using Sitecore.Globalization;
using Sitecore.Shell.Controls.Splitters;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Sitecore.Web.UI.XmlControls;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.ContentEditor;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;

namespace Sitecore.Support.Forms.Shell.UI
{
    public class FormDesigner : ApplicationForm
    {
        // Fields
        private readonly IAnalyticsSettings analyticsSettings = DependenciesManager.Resolve<IAnalyticsSettings>();
        protected FormBuilder builder;
        public static readonly string DefautSubmitCommand = "{745D9CF0-B189-4EAD-8D1B-8CAB68B5C972}";
        protected GridPanel DesktopPanel;
        protected Literal FieldsLabel;
        protected Sitecore.Forms.Shell.UI.Controls.XmlControl Footer;
        protected RichTextBorder FooterGrid;
        public static readonly string FormBuilderID = "FormBuilderID";
        protected VSplitterXmlControl FormsSpliter;
        protected GenericControl FormSubmit;
        protected Border FormTablePanel;
        protected Literal FormTitle;
        protected Sitecore.Forms.Shell.UI.Controls.XmlControl Intro;
        protected RichTextBorder IntroGrid;
        protected Border RibbonPanel;
        public static readonly string RibbonPath = "/sitecore/content/Applications/Modules/Web Forms for Marketers/Form Designer/Ribbon";
        public static Sitecore.Forms.Shell.UI.FormDesigner.ClientDialogCallback.Action saveCallback;
        public static FormDesigner savedDesigner;
        protected FormSettingsDesigner SettingsEditor;
        protected Border TitleBorder;

        // Methods
        private void AddNewField()
        {
            this.builder.AddToSetNewField();
            SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
            SheerResponse.Eval("$j('#f1 input:first').trigger('focus'); $j('.v-splitter').trigger('change')");
        }

        private void AddNewField(string parent, string id, string index)
        {
            this.builder.AddToSetNewField(parent, id, int.Parse(index));
        }

        private void AddNewSection(string id, string index)
        {
            this.builder.AddToSetNewSection(id, int.Parse(index));
        }

        protected virtual void BuildUpClientDictionary()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['tagDescription'] = '{0}';", DependenciesManager.ResourceManager.Localize("TAG_PROPERTY_DESCRIPTION"));
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['tagLabel'] = '{0}';", DependenciesManager.ResourceManager.Localize("TAG_LABEL_COLON"));
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['analyticsLabel']= '{0}';", DependenciesManager.ResourceManager.Localize("ANALYTICS"));
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['editButton']= '{0}';", DependenciesManager.ResourceManager.Localize("EDIT"));
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['conditionRulesLiteral']= '{0}';", DependenciesManager.ResourceManager.Localize("RULES"));
            builder.AppendFormat("Sitecore.FormBuilder.dictionary['noConditions']= '{0}';", DependenciesManager.ResourceManager.Localize("THERE_IS_NO_RULES_FOR_THIS_ELEMENT"));
            Context.ClientPage.ClientScript.RegisterClientScriptBlock(base.GetType(), "sc-webform-dict", builder.ToString(), true);
        }

        [HandleMessage("item:load", true)]
        private void ChangeLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!string.IsNullOrEmpty(args.Parameters["language"]) && this.CheckModified(true))
            {
                UrlString str = new UrlString(HttpUtility.UrlDecode(HttpContext.Current.Request.RawUrl.Replace("&amp;", "&")))
                {
                    ["la"] = args.Parameters["language"]
                };
                Context.ClientPage.ClientResponse.SetLocation(str.ToString());
            }
        }

        private bool CheckModified(bool checkIfActionsModified)
        {
            if (checkIfActionsModified && this.SettingsEditor.IsModifiedActions)
            {
                Context.ClientPage.Modified = true;
                this.SettingsEditor.IsModifiedActions = false;
            }
            return SheerResponse.CheckModified();
        }

        protected virtual void CloseFormWebEdit()
        {
            if (this.CheckModified(true))
            {
                object sessionValue = Web.WebUtil.GetSessionValue(StaticSettings.Mode);
                bool flag = (sessionValue == null) ? string.IsNullOrEmpty(Web.WebUtil.GetQueryString("formId")) : (string.Compare(sessionValue.ToString(), StaticSettings.DesignMode, true) == 0);
                bool isExperienceEditor = Context.PageMode.IsExperienceEditor;
                SheerResponse.SetDialogValue(Web.WebUtil.GetQueryString("hdl"));
                if (this.IsWebEditForm || !flag)
                {
                    if (!string.IsNullOrEmpty(this.BackUrl))
                    {
                        SheerResponse.Eval("window.top.location.href='" + MainUtil.DecodeName(this.BackUrl) + "'");
                    }
                    else
                    {
                        SheerResponse.Eval("if(window.parent!=null&&window.parent.parent!=null&&window.parent.parent.scManager!= null){window.parent.parent.scManager.closeWindow(window.parent);}else{}");
                        SheerResponse.CloseWindow();
                    }
                }
                else
                {
                    SheerResponse.CloseWindow();
                }
            }
        }

        public void CompareTypes(string id, string newTypeID, string oldTypeID, string propValue)
        {
            Item currentItem = this.GetCurrentItem();
            List<string> list = new List<string>(Sitecore.Support.Form.Core.Visual.PropertiesFactory.CompareTypes(ParametersUtil.XmlToPairArray(HttpUtility.UrlDecode(propValue)), currentItem.Database.GetItem(newTypeID), currentItem.Database.GetItem(oldTypeID), Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeAssemblyID, Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeClassID));
            if (list.Count > 0)
            {
                ClientDialogs.Confirmation(string.Format(DependenciesManager.ResourceManager.Localize("CHANGE_TYPE"), "\n\n", string.Join(",\n\t", list.ToArray()), "\t"), new BasePipelineMessage.ExecuteCallback(new ClientDialogCallback(id, oldTypeID, newTypeID).Execute));
            }
            else
            {
                SheerResponse.Eval(GetUpdateTypeScript("yes", id, oldTypeID, newTypeID));
            }
        }

        [HandleMessage("forms:configuregoal", true)]
        protected void ConfigureGoal(ClientPipelineArgs args)
        {
            Database database = Factory.GetDatabase(this.CurrentDatabase);
            Item goal = new Tracking(this.SettingsEditor.TrackingXml, database).Goal;
            if (goal != null)
            {
                Item[] items = new Item[] { goal };
                CommandContext context = new CommandContext(items);
                CommandManager.GetCommand("item:personalize").Execute(context);
            }
            else
            {
                SheerResponse.Alert(DependenciesManager.ResourceManager.Localize("CHOOSE_GOAL_AT_FIRST"), new string[0]);
            }
        }

        [HandleMessage("forms:analytics", true)]
        protected void CustomizeAnalytics(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    this.SettingsEditor.FormID = this.CurrentItemID;
                    this.SettingsEditor.TrackingXml = args.Result;
                    this.SettingsEditor.UpdateCommands(this.SettingsEditor.SaveActions, this.builder.FormStucture.ToXml(), true);
                    SheerResponse.Eval("Sitecore.PropertiesBuilder.editors = [];");
                    SheerResponse.Eval("Sitecore.PropertiesBuilder.setActiveProperties(Sitecore.FormBuilder, null)");
                    this.SaveFormAnalyticsText();
                }
            }
            else
            {
                UrlString urlString = new UrlString(UIUtil.GetUri("control:Forms.CustomizeAnalyticsWizard"));
                new UrlHandle { ["tracking"] = this.SettingsEditor.TrackingXml }.Add(urlString);
                Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                args.WaitForPostBack();
            }
        }

        [HandleMessage("forms:edititem", true)]
        protected void Edit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.CheckModified(false))
            {
                bool save = true;
                ListDefinition saveActions = this.SettingsEditor.SaveActions;
                if (saveActions.Groups.Any<IGroupDefinition>())
                {
                    IListItemDefinition listItem = saveActions.Groups.First<IGroupDefinition>().GetListItem(args.Parameters["unicid"]);
                    if (listItem == null)
                    {
                        save = false;
                        saveActions = this.SettingsEditor.CheckActions;
                        if (saveActions.Groups.Any<IGroupDefinition>())
                        {
                            listItem = saveActions.Groups.First<IGroupDefinition>().GetListItem(args.Parameters["unicid"]);
                        }
                    }
                    if (listItem != null)
                    {
                        if (args.IsPostBack)
                        {
                            UrlHandle handle = UrlHandle.Get(new UrlString(args.Parameters["url"]));
                            this.SettingsEditor.FormID = this.CurrentItemID;
                            this.SettingsEditor.TrackingXml = handle["tracking"];
                            if (args.HasResult)
                            {
                                listItem.Parameters = (args.Result == "-") ? string.Empty : Sitecore.Support.Forms.Core.Utility.ParametersUtil.Expand(args.Result);
                                this.SettingsEditor.UpdateCommands(saveActions, this.builder.FormStucture.ToXml(), save);
                            }
                        }
                        else
                        {
                            UrlString str;
                            string name = ID.NewID.ToString();
                            HttpContext.Current.Session.Add(name, listItem.Parameters);
                            ActionItem item = new ActionItem(StaticSettings.ContextDatabase.GetItem(listItem.ItemID));
                            if (item.Editor.Contains("~/xaml/"))
                            {
                                str = new UrlString(item.Editor);
                            }
                            else
                            {
                                str = new UrlString(UIUtil.GetUri(item.Editor));
                            }
                            str.Append("params", name);
                            str.Append("id", this.CurrentItemID);
                            str.Append("actionid", listItem.ItemID);
                            str.Append("la", this.CurrentLanguage.Name);
                            str.Append("uniqid", listItem.Unicid);
                            str.Append("db", this.CurrentDatabase);
                            new UrlHandle
                            {
                                ["tracking"] = this.SettingsEditor.TrackingXml,
                                ["actiondefinition"] = this.SettingsEditor.SaveActions.ToXml()
                            }.Add(str);
                            args.Parameters["url"] = str.ToString();
                            string queryString = item.QueryString;
                            ModalDialog.Show(str, queryString);
                            args.WaitForPostBack();
                        }
                    }
                }
            }
        }

        [HandleMessage("forms:editsuccess", true)]
        private void EditSuccess(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.CheckModified(false))
            {
                if (args.IsPostBack)
                {
                    if (args.HasResult)
                    {
                        NameValueCollection values = ParametersUtil.XmlToNameValueCollection(args.Result);
                        FormItem item1 = new FormItem(this.GetCurrentItem());
                        LinkField successPage = item1.SuccessPage;
                        Item item = item1.Database.GetItem(values["page"]);
                        if (!string.IsNullOrEmpty(values["page"]))
                        {
                            successPage.TargetID = MainUtil.GetID(values["page"], null);
                            if (item != null)
                            {
                                Language language;
                                if (!Language.TryParse(Web.WebUtil.GetQueryString("la"), out language))
                                {
                                    language = Context.Language;
                                }
                                successPage.Url = Sitecore.Form.Core.Utility.ItemUtil.GetItemUrl(item, Configuration.Settings.Rendering.SiteResolving, language);
                            }
                        }
                        this.SettingsEditor.UpdateSuccess(values["message"], values["page"], successPage.Url, values["choice"] == "1");
                    }
                }
                else
                {
                    UrlString urlString = new UrlString(UIUtil.GetUri("control:SuccessForm.Editor"));
                    UrlHandle handle = new UrlHandle
                    {
                        ["message"] = this.SettingsEditor.SubmitMessage
                    };
                    if (!string.IsNullOrEmpty(this.SettingsEditor.SubmitPageID))
                    {
                        handle["page"] = this.SettingsEditor.SubmitPageID;
                    }
                    handle["choice"] = this.SettingsEditor.SuccessRedirect ? "1" : "0";
                    handle.Add(urlString);
                    Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                    args.WaitForPostBack();
                }
            }
        }

        private void ExportToAscx()
        {
            Run.ExportToAscx(this, this.GetCurrentItem().Uri);
        }

        public Item GetCurrentItem() =>
            Database.GetItem(new ItemUri(this.CurrentItemID, this.CurrentLanguage, this.CurrentVersion, this.CurrentDatabase));

        private static string GetUpdateTypeScript(string res, string id, string oldTypeID, string newTypeID)
        {
            StringBuilder builder1 = new StringBuilder();
            builder1.Append("Sitecore.PropertiesBuilder.changeType('");
            builder1.Append(res);
            builder1.Append("','");
            builder1.Append(id);
            builder1.Append("','");
            builder1.Append(newTypeID);
            builder1.Append("','");
            builder1.Append(oldTypeID);
            builder1.Append("')");
            return builder1.ToString();
        }

        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            base.HandleMessage(message);
            string name = message.Name;
            switch (name)
            {
                case null:
                    break;

                case "forms:save":
                    this.SaveFormStructure(true, null);
                    return;

                default:
                    {
                        if (string.IsNullOrEmpty(message["id"]))
                        {
                            break;
                        }
                        ClientPipelineArgs args = new ClientPipelineArgs();
                        args.Parameters.Add("id", message["id"]);
                        if (name != "richtext:edit")
                        {
                            if (name != "richtext:edithtml")
                            {
                                if (name != "richtext:fix")
                                {
                                    return;
                                }
                                Context.ClientPage.Start(this.SettingsEditor, "Fix", args);
                                break;
                            }
                        }
                        else
                        {
                            Context.ClientPage.Start(this.SettingsEditor, "EditText", args);
                            return;
                        }
                        Context.ClientPage.Start(this.SettingsEditor, "EditHtml", args);
                        return;
                    }
            }
        }

        [HandleMessage("list:edit", true)]
        protected void ListEdit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                UrlString str = new UrlString("/sitecore/shell/~/xaml/Sitecore.Forms.Shell.UI.Dialogs.ListItemsEditor.aspx");
                string name = ID.NewID.ToString();
                string str3 = HttpUtility.UrlDecode(args.Parameters["value"]);
                if (str3.StartsWith(StaticSettings.SourceMarker))
                {
                    str3 = new QuerySettings("root", str3.Substring(StaticSettings.SourceMarker.Length)).ToString();
                }
                NameValueCollection values = new NameValueCollection
                {
                    ["queries"] = str3
                };
                HttpContext.Current.Session.Add(name, ParametersUtil.NameValueCollectionToXml(values, true));
                str.Append("params", name);
                str.Append("id", this.CurrentItemID);
                str.Append("db", this.CurrentDatabase);
                str.Append("la", this.CurrentLanguage.Name);
                str.Append("vs", this.CurrentVersion.Number.ToString());
                str.Append("target", args.Parameters["target"]);
                Context.ClientPage.ClientResponse.ShowModalDialog(str.ToString(), true);
                args.WaitForPostBack();
            }
            else if (args.HasResult)
            {
                if (args.Result == "-")
                {
                    args.Result = string.Empty;
                }
                NameValueCollection values2 = ParametersUtil.XmlToNameValueCollection(Sitecore.Support.Forms.Core.Utility.ParametersUtil.Expand(args.Result, true), true);
                SheerResponse.SetAttribute(args.Parameters["target"], "value", HttpUtility.UrlEncode(values2["queries"]));
                SheerResponse.Eval("Sitecore.FormBuilder.executeOnChange($('" + args.Parameters["target"] + "'));");
                if (HttpUtility.UrlDecode(args.Parameters["value"]) != values2["queries"])
                {
                    SheerResponse.SetModified(true);
                }
            }
        }

        private void LoadControls()
        {
            FormItem item = new FormItem(this.GetCurrentItem());
            this.builder = new FormBuilder();
            this.builder.ID = FormBuilderID;
            this.builder.UriItem = item.Uri.ToString();
            this.FormTablePanel.Controls.Add(this.builder);
            this.FormTitle.Text = item.FormName;
            if (string.IsNullOrEmpty(this.FormTitle.Text))
            {
                this.FormTitle.Text = DependenciesManager.ResourceManager.Localize("UNTITLED_FORM");
            }
            this.TitleBorder.Controls.Add(new Literal("<input ID=\"ShowTitle\" Type=\"hidden\"/>"));
            if (!item.ShowTitle)
            {
                this.TitleBorder.Style.Add("display", "none");
            }
            this.SettingsEditor.TitleName = this.FormTitle.Text;
            this.SettingsEditor.TitleTags = (from ch in StaticSettings.TitleTagsRoot.Children select ch.Name).ToArray<string>();
            this.SettingsEditor.SelectedTitleTag = item.TitleTag;
            this.Intro.Controls.Add(new Literal("<input ID=\"ShowIntro\" Type=\"hidden\"/>"));
            this.IntroGrid.Value = item.Introduction;
            if (string.IsNullOrEmpty(this.IntroGrid.Value))
            {
                this.IntroGrid.Value = DependenciesManager.ResourceManager.Localize("FORM_INTRO_EMPTY");
            }
            if (!item.ShowIntroduction)
            {
                this.Intro.Style.Add("display", "none");
            }
            this.IntroGrid.FieldName = item.IntroductionFieldName;
            this.SettingsEditor.FormID = this.CurrentItemID;
            this.SettingsEditor.Introduce = this.IntroGrid.Value;
            this.SettingsEditor.SaveActionsValue = item.SaveActions;
            this.SettingsEditor.CheckActionsValue = item.CheckActions;
            this.SettingsEditor.TrackingXml = item.Tracking.ToString();
            this.SettingsEditor.SuccessRedirect = item.SuccessRedirect;
            if (item.SuccessPage.TargetItem != null)
            {
                Language language;
                if (!Language.TryParse(Web.WebUtil.GetQueryString("la"), out language))
                {
                    language = Context.Language;
                }
                this.SettingsEditor.SubmitPage = Sitecore.Form.Core.Utility.ItemUtil.GetItemUrl(item.SuccessPage.TargetItem, Configuration.Settings.Rendering.SiteResolving, language);
            }
            else
            {
                this.SettingsEditor.SubmitPage = item.SuccessPage.Url;
            }
            if (!ID.IsNullOrEmpty(item.SuccessPageID))
            {
                this.SettingsEditor.SubmitPageID = item.SuccessPageID.ToString();
            }
            this.Footer.Controls.Add(new Literal("<input ID=\"ShowFooter\" Type=\"hidden\"/>"));
            this.FooterGrid.Value = item.Footer;
            if (string.IsNullOrEmpty(this.FooterGrid.Value))
            {
                this.FooterGrid.Value = DependenciesManager.ResourceManager.Localize("FORM_FOOTER_EMPTY");
            }
            if (!item.ShowFooter)
            {
                this.Footer.Style.Add("display", "none");
            }
            this.FooterGrid.FieldName = item.FooterFieldName;
            this.SettingsEditor.Footer = this.FooterGrid.Value;
            this.SettingsEditor.SubmitMessage = item.SuccessMessage;
            string str = string.IsNullOrEmpty(item.SubmitName) ? DependenciesManager.ResourceManager.Localize("NO_BUTTON_NAME") : Sitecore.Form.Core.Configuration.Translate.TextByItemLanguage(item.SubmitName, item.Language.GetDisplayName());
            this.FormSubmit.Attributes["value"] = str;
            this.SettingsEditor.SubmitName = str;
            this.UpdateRibbon();
        }

        private void LoadPropertyEditor(string typeID, string id)
        {
            Item currentItem = this.GetCurrentItem();
            Item item = currentItem.Database.GetItem(typeID);
            if (!string.IsNullOrEmpty(typeID))
            {
                try
                {
                    string str = PropertiesFactory.RenderPropertiesSection(item, Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeAssemblyID, Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeClassID);
                    Tracking tracking = new Tracking(this.SettingsEditor.TrackingXml, currentItem.Database);
                    if ((!this.analyticsSettings.IsAnalyticsAvailable || tracking.Ignore) || (item["Deny Tag"] == "1"))
                    {
                        str = str + "<input id='denytag' type='hidden'/>";
                    }
                    if (!string.IsNullOrEmpty(str))
                    {
                        this.SettingsEditor.PropertyEditor = str;
                    }
                }
                catch
                {
                }
            }
            else if (id == "Welcome")
            {
                this.SettingsEditor.ShowEmptyForm();
            }
        }

        private void Localize()
        {
            this.FormTitle.Text = DependenciesManager.ResourceManager.Localize("TITLE_CAPTION");
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!Context.ClientPage.IsEvent)
            {
                this.Localize();
                this.BuildUpClientDictionary();
                if (string.IsNullOrEmpty(Registry.GetString("/Current_User/VSplitters/FormsSpliter")))
                {
                    Registry.SetString("/Current_User/VSplitters/FormsSpliter", "412,");
                }
                this.LoadControls();
                if (this.builder.IsEmpty)
                {
                    this.SettingsEditor.ShowEmptyForm();
                }
            }
            else
            {
                this.builder = this.FormTablePanel.FindControl(FormBuilderID) as FormBuilder;
                this.builder.UriItem = this.GetCurrentItem().Uri.ToString();
            }
        }

        [HandleMessage("forms:addaction", true)]
        private void OpenSetSubmitActions(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.CheckModified(false))
            {
                if (args.IsPostBack)
                {
                    UrlHandle handle = UrlHandle.Get(new UrlString(args.Parameters["url"]));
                    this.SettingsEditor.TrackingXml = handle["tracking"];
                    this.SettingsEditor.FormID = this.CurrentItemID;
                    if (args.HasResult)
                    {
                        ListDefinition definition = ListDefinition.Parse((args.Result == "-") ? string.Empty : args.Result);
                        this.SettingsEditor.UpdateCommands(definition, this.builder.FormStucture.ToXml(), args.Parameters["mode"] == "save");
                    }
                }
                else
                {
                    string name = ID.NewID.ToString();
                    HttpContext.Current.Session.Add(name, (args.Parameters["mode"] == "save") ? this.SettingsEditor.SaveActions : this.SettingsEditor.CheckActions);
                    UrlString urlString = new UrlString(UIUtil.GetUri("control:SubmitCommands.Editor"));
                    urlString.Append("definition", name);
                    urlString.Append("db", this.GetCurrentItem().Database.Name);
                    urlString.Append("id", this.CurrentItemID);
                    urlString.Append("la", this.CurrentLanguage.Name);
                    urlString.Append("root", args.Parameters["root"]);
                    urlString.Append("system", args.Parameters["system"] ?? string.Empty);
                    args.Parameters.Add("params", name);
                    new UrlHandle
                    {
                        ["title"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_TITLE" : "SELECT_CHECK_TITLE"),
                        ["desc"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SELECT_SAVE_DESC" : "SELECT_CHECK_DESC"),
                        ["actions"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "SAVE_ACTIONS" : "CHECK_ACTIONS"),
                        ["addedactions"] = DependenciesManager.ResourceManager.Localize((args.Parameters["mode"] == "save") ? "ADDED_SAVE_ACTIONS" : "ADDED_CHECK_ACTIONS"),
                        ["tracking"] = this.SettingsEditor.TrackingXml,
                        ["structure"] = this.builder.FormStucture.ToXml()
                    }.Add(urlString);
                    args.Parameters["url"] = urlString.ToString();
                    Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
                    args.WaitForPostBack();
                }
            }
        }

        protected void Refresh(string url)
        {
            this.builder.ReloadForm();
        }

        private void Save(bool refresh)
        {
            FormItem.UpdateFormItem(this.GetCurrentItem().Database, this.CurrentLanguage, this.builder.FormStucture);
            this.SaveFormsText();
            Context.ClientPage.Modified = false;
            if (refresh)
            {
                this.Refresh(string.Empty);
            }
        }

        private void SaveFormAnalyticsText()
        {
            Item currentItem = this.GetCurrentItem();
            currentItem.Editing.BeginEdit();
            if (currentItem.Fields["__Tracking"] != null)
            {
                currentItem.Fields["__Tracking"].Value = this.SettingsEditor.TrackingXml;
            }
            currentItem.Editing.EndEdit();
        }

        private void SaveFormsText()
        {
            Item currentItem = this.GetCurrentItem();
            FormItem item = new FormItem(currentItem);
            currentItem.Editing.BeginEdit();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID].Value = this.SettingsEditor.TitleName;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleTagID].Value = this.SettingsEditor.SelectedTitleTag.ToString();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value = Context.ClientPage.ClientRequest.Form["ShowTitle"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormIntroductionID].Value = this.SettingsEditor.Introduce;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormIntroID].Value = Context.ClientPage.ClientRequest.Form["ShowIntro"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormFooterID].Value = this.SettingsEditor.Footer;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormFooterID].Value = Context.ClientPage.ClientRequest.Form["ShowFooter"];
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormSubmitID].Value = (this.SettingsEditor.SubmitName == string.Empty) ? DependenciesManager.ResourceManager.Localize("NO_BUTTON_NAME") : this.SettingsEditor.SubmitName;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value = this.SettingsEditor.SaveActions.ToXml();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value = this.SettingsEditor.CheckActions.ToXml();
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessMessageID].Value = this.SettingsEditor.SubmitMessage;
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessModeID].Value = this.SettingsEditor.SuccessRedirect ? "{F4D50806-6B89-4F2D-89FE-F77FC0A07D48}" : "{3B8369A0-CC1A-4E9A-A3DB-7B086379C53B}";
            LinkField successPage = item.SuccessPage;
            successPage.TargetID = MainUtil.GetID(this.SettingsEditor.SubmitPageID, ID.Null);
            if (successPage.TargetItem != null)
            {
                successPage.Url = successPage.TargetItem.Paths.Path;
            }
            currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID].Value = successPage.Xml.OuterXml;
            currentItem.Editing.EndEdit();
        }

        protected virtual void SaveFormStructure()
        {
            SheerResponse.Eval("Sitecore.FormBuilder.SaveData();");
        }

        protected virtual void SaveFormStructure(bool refresh, Sitecore.Forms.Shell.UI.FormDesigner.ClientDialogCallback.Action callback)
        {
            bool flag = false;
            foreach (SectionDefinition definition in this.builder.FormStucture.Sections)
            {
                if (((definition.Name == string.Empty) && (definition.Deleted != "1")) && definition.IsHasOnlyEmptyField)
                {
                    flag = true;
                    break;
                }
                foreach (FieldDefinition definition2 in definition.Fields)
                {
                    if (string.IsNullOrEmpty(definition2.Name) && (definition2.Deleted != "1"))
                    {
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    break;
                }
            }
            if (flag)
            {
                saveCallback = callback;
                savedDesigner = this;
                ClientDialogs.Confirmation(DependenciesManager.ResourceManager.Localize("EMPTY_FIELD_NAME"), new BasePipelineMessage.ExecuteCallback(new ClientDialogCallback().SaveConfirmation));
            }
            else
            {
                this.Save(refresh);
                if (callback != null)
                {
                    callback();
                }
            }
        }

        protected virtual void SaveFormStructureAndClose()
        {
            Context.ClientPage.Modified = false;
            this.SettingsEditor.IsModifiedActions = false;
            this.SaveFormStructure(false, new Sitecore.Forms.Shell.UI.FormDesigner.ClientDialogCallback.Action(this.CloseFormWebEdit));
        }

        [HandleMessage("item:save", true)]
        private void SaveMessage(ClientPipelineArgs args)
        {
            this.SaveFormStructure(true, null);
            SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
        }

        [HandleMessage("item:selectlanguage", true)]
        private void SelectLanguage(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Run.SetLanguage(this, this.GetCurrentItem().Uri);
        }

        private void UpdateRibbon()
        {
            Item currentItem = this.GetCurrentItem();
            Ribbon ctl = new Ribbon
            {
                ID = "FormDesigneRibbon",
                CommandContext = new CommandContext(currentItem)
            };
            Item item = Context.Database.GetItem(RibbonPath);
            Error.AssertItemFound(item, RibbonPath);
            bool flag = !string.IsNullOrEmpty(this.SettingsEditor.TitleName);
            ctl.CommandContext.Parameters.Add("title", flag.ToString());
            bool flag2 = !string.IsNullOrEmpty(this.SettingsEditor.Introduce);
            ctl.CommandContext.Parameters.Add("intro", flag2.ToString());
            bool flag3 = !string.IsNullOrEmpty(this.SettingsEditor.Footer);
            ctl.CommandContext.Parameters.Add("footer", flag3.ToString());
            ctl.CommandContext.Parameters.Add("id", currentItem.ID.ToString());
            ctl.CommandContext.Parameters.Add("la", currentItem.Language.Name);
            ctl.CommandContext.Parameters.Add("vs", currentItem.Version.Number.ToString());
            ctl.CommandContext.Parameters.Add("db", currentItem.Database.Name);
            ctl.CommandContext.RibbonSourceUri = item.Uri;
            ctl.ShowContextualTabs = false;
            this.RibbonPanel.InnerHtml = Sitecore.Web.HtmlUtil.RenderControl(ctl);
        }

        private void UpdateSubmit()
        {
            this.SettingsEditor.FormID = this.CurrentItemID;
            this.SettingsEditor.UpdateCommands(this.SettingsEditor.SaveActions, this.builder.FormStucture.ToXml(), true);
        }

        private void UpgradeToSection(string parent, string id)
        {
            this.builder.UpgradeToSection(id);
        }

        [HandleMessage("forms:validatetext", true)]
        private void ValidateText(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (!args.IsPostBack)
            {
                this.SettingsEditor.Validate(args.Parameters["ctrl"]);
            }
        }

        private void WarningEmptyForm()
        {
            this.builder.ShowEmptyForm();
            Control control = this.SettingsEditor.ShowEmptyForm();
            Context.ClientPage.ClientResponse.SetOuterHtml(control.ID, control);
        }

        // Properties
        public string BackUrl =>
            Web.WebUtil.GetQueryString("backurl");

        public string CurrentDatabase =>
            Web.WebUtil.GetQueryString("db");

        public string CurrentItemID
        {
            get
            {
                string queryString = Web.WebUtil.GetQueryString("formid");
                if (string.IsNullOrEmpty(queryString))
                {
                    queryString = Web.WebUtil.GetQueryString("webform");
                }
                if (string.IsNullOrEmpty(queryString))
                {
                    queryString = Web.WebUtil.GetQueryString("id");
                }
                if (string.IsNullOrEmpty(queryString))
                {
                    queryString = Sitecore.Form.Core.Utility.Utils.GetDataSource(Web.WebUtil.GetQueryString());
                }
                return queryString;
            }
        }

        public Language CurrentLanguage =>
            Language.Parse(Web.WebUtil.GetQueryString("la"));

        public Data.Version CurrentVersion =>
            Data.Version.Parse(Web.WebUtil.GetQueryString("vs"));

        public bool IsWebEditForm =>
            !string.IsNullOrEmpty(Web.WebUtil.GetQueryString("webform"));

      
    [Serializable]
    public class ClientDialogCallback
    {
        // Fields
        private string id;
        private string newTypeID;
        private string oldTypeID;

        // Methods
        public ClientDialogCallback()
        {
        }

        public ClientDialogCallback(string id, string oldTypeID, string newTypeID)
        {
            this.id = id;
            this.oldTypeID = oldTypeID;
            this.newTypeID = newTypeID;
        }

        public void Execute(string res)
        {
            SheerResponse.Eval(Sitecore.Support.Forms.Shell.UI.FormDesigner.GetUpdateTypeScript(res, this.id, this.oldTypeID, this.newTypeID));
        }

        public void SaveConfirmation(string result)
        {
            if (result == "yes")
            {
                Sitecore.Support.Forms.Shell.UI.FormDesigner.savedDesigner.Save(true);
                if (FormDesigner.saveCallback != null)
                {
                    FormDesigner.saveCallback();
                }
            }
        }

        // Nested Types
        public delegate void Action();
    }
}

 

}