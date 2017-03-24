using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.ContentEditor.Data;
using Sitecore.Form.Core.Data;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Forms.Shell.UI.Dialogs;
using Sitecore.Globalization;
using Sitecore.Shell.Controls.Splitters;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Support.Form.Core.Visual;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using Sitecore.Web.UI.XmlControls;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.Dependencies;
using Action = System.Action;
using Control = System.Web.UI.Control;
using HtmlUtil = Sitecore.Web.HtmlUtil;
using ItemUtil = Sitecore.Form.Core.Utility.ItemUtil;
using Settings = Sitecore.Configuration.Settings;
using Translate = Sitecore.Form.Core.Configuration.Translate;
using Version = Sitecore.Data.Version;
using WebUtil = Sitecore.Web.WebUtil;
using XmlControl = Sitecore.Web.UI.XmlControls.XmlControl;

namespace Sitecore.Support.Forms.Shell.UI
{
  public class FormDesigner : ApplicationForm
  {
    public static readonly string DefautSubmitCommand = "{745D9CF0-B189-4EAD-8D1B-8CAB68B5C972}";
    public static readonly string FormBuilderID = "FormBuilderID";

    public static readonly string RibbonPath =
      "/sitecore/content/Applications/Modules/Web Forms for Marketers/Form Designer/Ribbon";

    public static Action saveCallback;
    public static FormDesigner savedDesigner;
    private readonly IAnalyticsSettings analyticsSettings = DependenciesManager.Resolve<IAnalyticsSettings>();
    protected FormBuilder builder;
    protected GridPanel DesktopPanel;
    protected Literal FieldsLabel;
    protected XmlControl Footer;
    protected RichTextBorder FooterGrid;
    protected VSplitterXmlControl FormsSpliter;
    protected GenericControl FormSubmit;
    protected Border FormTablePanel;
    protected Literal FormTitle;
    protected XmlControl Intro;
    protected RichTextBorder IntroGrid;
    protected Border RibbonPanel;
    protected FormSettingsDesigner SettingsEditor;
    protected Border TitleBorder;

    public string BackUrl =>
      WebUtil.GetQueryString("backurl");

    public string CurrentDatabase =>
      WebUtil.GetQueryString("db");

    public string CurrentItemID
    {
      get
      {
        var queryString = WebUtil.GetQueryString("formid");
        if (string.IsNullOrEmpty(queryString))
          queryString = WebUtil.GetQueryString("webform");
        if (string.IsNullOrEmpty(queryString))
          queryString = WebUtil.GetQueryString("id");
        if (string.IsNullOrEmpty(queryString))
          queryString = Sitecore.Form.Core.Utility.Utils.GetDataSource(WebUtil.GetQueryString());
        return queryString;
      }
    }

    public Language CurrentLanguage =>
      Language.Parse(WebUtil.GetQueryString("la"));

    public Version CurrentVersion =>
      Version.Parse(WebUtil.GetQueryString("vs"));

    public bool IsWebEditForm =>
      !string.IsNullOrEmpty(WebUtil.GetQueryString("webform"));

    private void AddNewField()
    {
      builder.AddToSetNewField();
      SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
      SheerResponse.Eval("$j('#f1 input:first').trigger('focus'); $j('.v-splitter').trigger('change')");
    }

    private void AddNewField(string parent, string id, string index)
    {
      builder.AddToSetNewField(parent, id, int.Parse(index));
    }

    private void AddNewSection(string id, string index)
    {
      builder.AddToSetNewSection(id, int.Parse(index));
    }

    protected virtual void BuildUpClientDictionary()
    {
      var builder = new StringBuilder();
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['tagDescription'] = '{0}';",
        DependenciesManager.ResourceManager.Localize("TAG_PROPERTY_DESCRIPTION"));
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['tagLabel'] = '{0}';",
        DependenciesManager.ResourceManager.Localize("TAG_LABEL_COLON"));
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['analyticsLabel']= '{0}';",
        DependenciesManager.ResourceManager.Localize("ANALYTICS"));
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['editButton']= '{0}';",
        DependenciesManager.ResourceManager.Localize("EDIT"));
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['conditionRulesLiteral']= '{0}';",
        DependenciesManager.ResourceManager.Localize("RULES"));
      builder.AppendFormat("Sitecore.FormBuilder.dictionary['noConditions']= '{0}';",
        DependenciesManager.ResourceManager.Localize("THERE_IS_NO_RULES_FOR_THIS_ELEMENT"));
      Context.ClientPage.ClientScript.RegisterClientScriptBlock(GetType(), "sc-webform-dict", builder.ToString(), true);
    }

    [HandleMessage("item:load", true)]
    private void ChangeLanguage(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!string.IsNullOrEmpty(args.Parameters["language"]) && CheckModified(true))
      {
        var text1 = new UrlString(HttpUtility.UrlDecode(HttpContext.Current.Request.RawUrl.Replace("&amp;", "&")));
        text1["la"] = args.Parameters["language"];
        var str = text1;
        Context.ClientPage.ClientResponse.SetLocation(str.ToString());
      }
    }

    private bool CheckModified(bool checkIfActionsModified)
    {
      if (checkIfActionsModified && SettingsEditor.IsModifiedActions)
      {
        Context.ClientPage.Modified = true;
        SettingsEditor.IsModifiedActions = false;
      }
      return SheerResponse.CheckModified();
    }

    protected virtual void CloseFormWebEdit()
    {
      if (CheckModified(true))
      {
        var sessionValue = WebUtil.GetSessionValue(StaticSettings.Mode);
        var flag = sessionValue == null
          ? string.IsNullOrEmpty(WebUtil.GetQueryString("formId"))
          : string.Compare(sessionValue.ToString(), StaticSettings.DesignMode, true) == 0;
        var isExperienceEditor = Context.PageMode.IsExperienceEditor;
        SheerResponse.SetDialogValue(WebUtil.GetQueryString("hdl"));
        if (IsWebEditForm || !flag)
          if (!string.IsNullOrEmpty(BackUrl))
          {
            SheerResponse.Eval("window.top.location.href='" + MainUtil.DecodeName(BackUrl) + "'");
          }
          else
          {
            SheerResponse.Eval(
              "if(window.parent!=null&&window.parent.parent!=null&&window.parent.parent.scManager!= null){window.parent.parent.scManager.closeWindow(window.parent);}else{}");
            SheerResponse.CloseWindow();
          }
        else
          SheerResponse.CloseWindow();
      }
    }

    public void CompareTypes(string id, string newTypeID, string oldTypeID, string propValue)
    {
      var currentItem = GetCurrentItem();
      var list =
        new List<string>(PropertiesFactory.CompareTypes(
          ParametersUtil.XmlToPairArray(HttpUtility.UrlDecode(propValue)), currentItem.Database.GetItem(newTypeID),
          currentItem.Database.GetItem(oldTypeID), Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeAssemblyID,
          Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeClassID));
      if (list.Count > 0)
        ClientDialogs.Confirmation(
          string.Format(DependenciesManager.ResourceManager.Localize("CHANGE_TYPE"), "\n\n",
            string.Join(",\n\t", list.ToArray()), "\t"), new ClientDialogCallback(id, oldTypeID, newTypeID).Execute);
      else
        SheerResponse.Eval(GetUpdateTypeScript("yes", id, oldTypeID, newTypeID));
    }

    [HandleMessage("forms:configuregoal", true)]
    protected void ConfigureGoal(ClientPipelineArgs args)
    {
      var database = Factory.GetDatabase(CurrentDatabase);
      var goal = new Tracking(SettingsEditor.TrackingXml, database).Goal;
      if (goal != null)
      {
        Item[] items = {goal};
        var context = new CommandContext(items);
        CommandManager.GetCommand("item:personalize").Execute(context);
      }
      else
      {
        SheerResponse.Alert(DependenciesManager.ResourceManager.Localize("CHOOSE_GOAL_AT_FIRST"));
      }
    }

    [HandleMessage("forms:analytics", true)]
    protected void CustomizeAnalytics(ClientPipelineArgs args)
    {
      if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          SettingsEditor.FormID = CurrentItemID;
          SettingsEditor.TrackingXml = args.Result;
          SettingsEditor.UpdateCommands(SettingsEditor.SaveActions, builder.FormStucture.ToXml(), true);
          SheerResponse.Eval("Sitecore.PropertiesBuilder.editors = [];");
          SheerResponse.Eval("Sitecore.PropertiesBuilder.setActiveProperties(Sitecore.FormBuilder, null)");
          SaveFormAnalyticsText();
        }
      }
      else
      {
        var urlString = new UrlString(UIUtil.GetUri("control:Forms.CustomizeAnalyticsWizard"));
        var handle1 = new UrlHandle();
        handle1["tracking"] = SettingsEditor.TrackingXml;
        handle1.Add(urlString);
        Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
        args.WaitForPostBack();
      }
    }

    [HandleMessage("forms:edititem", true)]
    protected void Edit(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (CheckModified(false))
      {
        var save = true;
        var saveActions = SettingsEditor.SaveActions;
        if (saveActions.Groups.Any())
        {
          var listItem = saveActions.Groups.First().GetListItem(args.Parameters["unicid"]);
          if (listItem == null)
          {
            save = false;
            saveActions = SettingsEditor.CheckActions;
            if (saveActions.Groups.Any())
              listItem = saveActions.Groups.First().GetListItem(args.Parameters["unicid"]);
          }
          if (listItem != null)
            if (args.IsPostBack)
            {
              var handle = UrlHandle.Get(new UrlString(args.Parameters["url"]));
              SettingsEditor.FormID = CurrentItemID;
              SettingsEditor.TrackingXml = handle["tracking"];
              if (args.HasResult)
              {
                listItem.Parameters = args.Result == "-"
                  ? string.Empty
                  : Core.Utility.ParametersUtil.Expand(args.Result);
                SettingsEditor.UpdateCommands(saveActions, builder.FormStucture.ToXml(), save);
              }
            }
            else
            {
              UrlString str;
              var name = ID.NewID.ToString();
              HttpContext.Current.Session.Add(name, listItem.Parameters);
              var item = new ActionItem(StaticSettings.ContextDatabase.GetItem(listItem.ItemID));
              if (item.Editor.Contains("~/xaml/"))
                str = new UrlString(item.Editor);
              else
                str = new UrlString(UIUtil.GetUri(item.Editor));
              str.Append("params", name);
              str.Append("id", CurrentItemID);
              str.Append("actionid", listItem.ItemID);
              str.Append("la", CurrentLanguage.Name);
              str.Append("uniqid", listItem.Unicid);
              str.Append("db", CurrentDatabase);
              var handle1 = new UrlHandle();
              handle1["tracking"] = SettingsEditor.TrackingXml;
              handle1["actiondefinition"] = SettingsEditor.SaveActions.ToXml();
              handle1.Add(str);
              args.Parameters["url"] = str.ToString();
              var queryString = item.QueryString;
              ModalDialog.Show(str, queryString);
              args.WaitForPostBack();
            }
        }
      }
    }

    [HandleMessage("forms:editsuccess", true)]
    private void EditSuccess(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (CheckModified(false))
        if (args.IsPostBack)
        {
          if (args.HasResult)
          {
            var values = ParametersUtil.XmlToNameValueCollection(args.Result);
            var item1 = new FormItem(GetCurrentItem());
            var successPage = item1.SuccessPage;
            var item = item1.Database.GetItem(values["page"]);
            if (!string.IsNullOrEmpty(values["page"]))
            {
              successPage.TargetID = MainUtil.GetID(values["page"], null);
              if (item != null)
              {
                Language language;
                if (!Language.TryParse(WebUtil.GetQueryString("la"), out language))
                  language = Context.Language;
                successPage.Url = ItemUtil.GetItemUrl(item, Settings.Rendering.SiteResolving, language);
              }
            }
            SettingsEditor.UpdateSuccess(values["message"], values["page"], successPage.Url, values["choice"] == "1");
          }
        }
        else
        {
          var urlString = new UrlString(UIUtil.GetUri("control:SuccessForm.Editor"));
          var handle1 = new UrlHandle();
          handle1["message"] = SettingsEditor.SubmitMessage;
          var handle = handle1;
          if (!string.IsNullOrEmpty(SettingsEditor.SubmitPageID))
            handle["page"] = SettingsEditor.SubmitPageID;
          handle["choice"] = SettingsEditor.SuccessRedirect ? "1" : "0";
          handle.Add(urlString);
          Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
          args.WaitForPostBack();
        }
    }

    private void ExportToAscx()
    {
      Run.ExportToAscx(this, GetCurrentItem().Uri);
    }

    public Item GetCurrentItem() =>
      Database.GetItem(new ItemUri(CurrentItemID, CurrentLanguage, CurrentVersion, CurrentDatabase));

    private static string GetUpdateTypeScript(string res, string id, string oldTypeID, string newTypeID)
    {
      var builder1 = new StringBuilder();
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
      var name = message.Name;
      switch (name)
      {
        case null:
          break;

        case "forms:save":
          SaveFormStructure(true, null);
          return;

        default:
        {
          if (string.IsNullOrEmpty(message["id"]))
            break;
          var args = new ClientPipelineArgs();
          args.Parameters.Add("id", message["id"]);
          if (name != "richtext:edit")
          {
            if (name != "richtext:edithtml")
            {
              if (name != "richtext:fix")
                return;
              Context.ClientPage.Start(SettingsEditor, "Fix", args);
              break;
            }
          }
          else
          {
            Context.ClientPage.Start(SettingsEditor, "EditText", args);
            return;
          }
          Context.ClientPage.Start(SettingsEditor, "EditHtml", args);
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
        var str = new UrlString("/sitecore/shell/~/xaml/Sitecore.Forms.Shell.UI.Dialogs.ListItemsEditor.aspx");
        var name = ID.NewID.ToString();
        var str3 = HttpUtility.UrlDecode(args.Parameters["value"]);
        if (str3.StartsWith(StaticSettings.SourceMarker))
          str3 = new QuerySettings("root", str3.Substring(StaticSettings.SourceMarker.Length)).ToString();
        var collection1 = new NameValueCollection();
        collection1["queries"] = str3;
        var values = collection1;
        HttpContext.Current.Session.Add(name, ParametersUtil.NameValueCollectionToXml(values, true));
        str.Append("params", name);
        str.Append("id", CurrentItemID);
        str.Append("db", CurrentDatabase);
        str.Append("la", CurrentLanguage.Name);
        str.Append("vs", CurrentVersion.Number.ToString());
        str.Append("target", args.Parameters["target"]);
        Context.ClientPage.ClientResponse.ShowModalDialog(str.ToString(), true);
        args.WaitForPostBack();
      }
      else if (args.HasResult)
      {
        if (args.Result == "-")
          args.Result = string.Empty;
        var values2 = ParametersUtil.XmlToNameValueCollection(Core.Utility.ParametersUtil.Expand(args.Result, true),
          true);
        SheerResponse.SetAttribute(args.Parameters["target"], "value", HttpUtility.UrlEncode(values2["queries"]));
        SheerResponse.Eval("Sitecore.FormBuilder.executeOnChange($('" + args.Parameters["target"] + "'));");
        if (HttpUtility.UrlDecode(args.Parameters["value"]) != values2["queries"])
          SheerResponse.SetModified(true);
      }
    }

    private void LoadControls()
    {
      var item = new FormItem(GetCurrentItem());
      builder = new FormBuilder();
      builder.ID = FormBuilderID;
      builder.UriItem = item.Uri.ToString();
      FormTablePanel.Controls.Add(builder);
      FormTitle.Text = item.FormName;
      if (string.IsNullOrEmpty(FormTitle.Text))
        FormTitle.Text = DependenciesManager.ResourceManager.Localize("UNTITLED_FORM");
      TitleBorder.Controls.Add(new Literal("<input ID=\"ShowTitle\" Type=\"hidden\"/>"));
      if (!item.ShowTitle)
        TitleBorder.Style.Add("display", "none");
      SettingsEditor.TitleName = FormTitle.Text;
      SettingsEditor.TitleTags = (from ch in StaticSettings.TitleTagsRoot.Children select ch.Name).ToArray();
      SettingsEditor.SelectedTitleTag = item.TitleTag;
      Intro.Controls.Add(new Literal("<input ID=\"ShowIntro\" Type=\"hidden\"/>"));
      IntroGrid.Value = item.Introduction;
      if (string.IsNullOrEmpty(IntroGrid.Value))
        IntroGrid.Value = DependenciesManager.ResourceManager.Localize("FORM_INTRO_EMPTY");
      if (!item.ShowIntroduction)
        Intro.Style.Add("display", "none");
      IntroGrid.FieldName = item.IntroductionFieldName;
      SettingsEditor.FormID = CurrentItemID;
      SettingsEditor.Introduce = IntroGrid.Value;
      SettingsEditor.SaveActionsValue = item.SaveActions;
      SettingsEditor.CheckActionsValue = item.CheckActions;
      SettingsEditor.TrackingXml = item.Tracking.ToString();
      SettingsEditor.SuccessRedirect = item.SuccessRedirect;
      if (item.SuccessPage.TargetItem != null)
      {
        Language language;
        if (!Language.TryParse(WebUtil.GetQueryString("la"), out language))
          language = Context.Language;
        SettingsEditor.SubmitPage = ItemUtil.GetItemUrl(item.SuccessPage.TargetItem, Settings.Rendering.SiteResolving,
          language);
      }
      else
      {
        SettingsEditor.SubmitPage = item.SuccessPage.Url;
      }
      if (!ID.IsNullOrEmpty(item.SuccessPageID))
        SettingsEditor.SubmitPageID = item.SuccessPageID.ToString();
      Footer.Controls.Add(new Literal("<input ID=\"ShowFooter\" Type=\"hidden\"/>"));
      FooterGrid.Value = item.Footer;
      if (string.IsNullOrEmpty(FooterGrid.Value))
        FooterGrid.Value = DependenciesManager.ResourceManager.Localize("FORM_FOOTER_EMPTY");
      if (!item.ShowFooter)
        Footer.Style.Add("display", "none");
      FooterGrid.FieldName = item.FooterFieldName;
      SettingsEditor.Footer = FooterGrid.Value;
      SettingsEditor.SubmitMessage = item.SuccessMessage;
      var str = string.IsNullOrEmpty(item.SubmitName)
        ? DependenciesManager.ResourceManager.Localize("NO_BUTTON_NAME")
        : Translate.TextByItemLanguage(item.SubmitName, item.Language.GetDisplayName());
      FormSubmit.Attributes["value"] = str;
      SettingsEditor.SubmitName = str;
      UpdateRibbon();
    }

    private void LoadPropertyEditor(string typeID, string id)
    {
      var currentItem = GetCurrentItem();
      var item = currentItem.Database.GetItem(typeID);
      if (!string.IsNullOrEmpty(typeID))
        try
        {
          var str = Sitecore.Form.Core.Visual.PropertiesFactory.RenderPropertiesSection(item,
            Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeAssemblyID,
            Sitecore.Form.Core.Configuration.FieldIDs.FieldTypeClassID);
          var tracking = new Tracking(SettingsEditor.TrackingXml, currentItem.Database);
          if (!analyticsSettings.IsAnalyticsAvailable || tracking.Ignore || (item["Deny Tag"] == "1"))
            str = str + "<input id='denytag' type='hidden'/>";
          if (!string.IsNullOrEmpty(str))
            SettingsEditor.PropertyEditor = str;
        }
        catch
        {
        }
      else if (id == "Welcome")
        SettingsEditor.ShowEmptyForm();
    }

    private void Localize()
    {
      FormTitle.Text = DependenciesManager.ResourceManager.Localize("TITLE_CAPTION");
    }

    protected override void OnLoad(EventArgs e)
    {
      if (!Context.ClientPage.IsEvent)
      {
        Localize();
        BuildUpClientDictionary();
        if (string.IsNullOrEmpty(Registry.GetString("/Current_User/VSplitters/FormsSpliter")))
          Registry.SetString("/Current_User/VSplitters/FormsSpliter", "412,");
        LoadControls();
        if (builder.IsEmpty)
          SettingsEditor.ShowEmptyForm();
      }
      else
      {
        builder = FormTablePanel.FindControl(FormBuilderID) as FormBuilder;
        builder.UriItem = GetCurrentItem().Uri.ToString();
      }
    }

    [HandleMessage("forms:addaction", true)]
    private void OpenSetSubmitActions(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (CheckModified(false))
        if (args.IsPostBack)
        {
          var handle = UrlHandle.Get(new UrlString(args.Parameters["url"]));
          SettingsEditor.TrackingXml = handle["tracking"];
          SettingsEditor.FormID = CurrentItemID;
          if (args.HasResult)
          {
            var definition = ListDefinition.Parse(args.Result == "-" ? string.Empty : args.Result);
            SettingsEditor.UpdateCommands(definition, builder.FormStucture.ToXml(), args.Parameters["mode"] == "save");
          }
        }
        else
        {
          var name = ID.NewID.ToString();
          HttpContext.Current.Session.Add(name,
            args.Parameters["mode"] == "save" ? SettingsEditor.SaveActions : SettingsEditor.CheckActions);
          var urlString = new UrlString(UIUtil.GetUri("control:SubmitCommands.Editor"));
          urlString.Append("definition", name);
          urlString.Append("db", GetCurrentItem().Database.Name);
          urlString.Append("id", CurrentItemID);
          urlString.Append("la", CurrentLanguage.Name);
          urlString.Append("root", args.Parameters["root"]);
          urlString.Append("system", args.Parameters["system"] ?? string.Empty);
          args.Parameters.Add("params", name);
          var handle1 = new UrlHandle();
          handle1["title"] =
            DependenciesManager.ResourceManager.Localize(args.Parameters["mode"] == "save"
              ? "SELECT_SAVE_TITLE"
              : "SELECT_CHECK_TITLE");
          handle1["desc"] =
            DependenciesManager.ResourceManager.Localize(args.Parameters["mode"] == "save"
              ? "SELECT_SAVE_DESC"
              : "SELECT_CHECK_DESC");
          handle1["actions"] =
            DependenciesManager.ResourceManager.Localize(args.Parameters["mode"] == "save"
              ? "SAVE_ACTIONS"
              : "CHECK_ACTIONS");
          handle1["addedactions"] =
            DependenciesManager.ResourceManager.Localize(args.Parameters["mode"] == "save"
              ? "ADDED_SAVE_ACTIONS"
              : "ADDED_CHECK_ACTIONS");
          handle1["tracking"] = SettingsEditor.TrackingXml;
          handle1["structure"] = builder.FormStucture.ToXml();
          handle1.Add(urlString);
          args.Parameters["url"] = urlString.ToString();
          Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), true);
          args.WaitForPostBack();
        }
    }

    protected void Refresh(string url)
    {
      builder.ReloadForm();
    }

    private void Save(bool refresh)
    {
      Core.Data.FormItem.UpdateFormItem(GetCurrentItem().Database, CurrentLanguage, builder.FormStucture);
      SaveFormsText();
      Context.ClientPage.Modified = false;
      if (refresh)
        Refresh(string.Empty);
    }

    private void SaveFormAnalyticsText()
    {
      var currentItem = GetCurrentItem();
      currentItem.Editing.BeginEdit();
      if (currentItem.Fields["__Tracking"] != null)
        currentItem.Fields["__Tracking"].Value = SettingsEditor.TrackingXml;
      currentItem.Editing.EndEdit();
    }

    private void SaveFormsText()
    {
      var currentItem = GetCurrentItem();
      var item = new FormItem(currentItem);
      currentItem.Editing.BeginEdit();
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID].Value = SettingsEditor.TitleName;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleTagID].Value =
        SettingsEditor.SelectedTitleTag.ToString();
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value =
        Context.ClientPage.ClientRequest.Form["ShowTitle"];
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormIntroductionID].Value = SettingsEditor.Introduce;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormIntroID].Value =
        Context.ClientPage.ClientRequest.Form["ShowIntro"];
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormFooterID].Value = SettingsEditor.Footer;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormFooterID].Value =
        Context.ClientPage.ClientRequest.Form["ShowFooter"];
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.FormSubmitID].Value = SettingsEditor.SubmitName ==
                                                                                         string.Empty
        ? DependenciesManager.ResourceManager.Localize("NO_BUTTON_NAME")
        : SettingsEditor.SubmitName;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SaveActionsID].Value =
        SettingsEditor.SaveActions.ToXml();
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.CheckActionsID].Value =
        SettingsEditor.CheckActions.ToXml();
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessMessageID].Value =
        SettingsEditor.SubmitMessage;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessModeID].Value = SettingsEditor.SuccessRedirect
        ? "{F4D50806-6B89-4F2D-89FE-F77FC0A07D48}"
        : "{3B8369A0-CC1A-4E9A-A3DB-7B086379C53B}";
      var successPage = item.SuccessPage;
      successPage.TargetID = MainUtil.GetID(SettingsEditor.SubmitPageID, ID.Null);
      if (successPage.TargetItem != null)
        successPage.Url = successPage.TargetItem.Paths.Path;
      currentItem.Fields[Sitecore.Form.Core.Configuration.FieldIDs.SuccessPageID].Value = successPage.Xml.OuterXml;
      currentItem.Editing.EndEdit();
    }

    protected virtual void SaveFormStructure()
    {
      SheerResponse.Eval("Sitecore.FormBuilder.SaveData();");
    }

    protected virtual void SaveFormStructure(bool refresh, Action callback)
    {
      var flag = false;
      foreach (SectionDefinition definition in builder.FormStucture.Sections)
      {
        if ((definition.Name == string.Empty) && (definition.Deleted != "1") && definition.IsHasOnlyEmptyField)
        {
          flag = true;
          break;
        }
        foreach (FieldDefinition definition2 in definition.Fields)
          if (string.IsNullOrEmpty(definition2.Name) && (definition2.Deleted != "1"))
          {
            flag = true;
            break;
          }
        if (flag)
          break;
      }
      if (flag)
      {
        saveCallback = callback;
        savedDesigner = this;
        ClientDialogs.Confirmation(DependenciesManager.ResourceManager.Localize("EMPTY_FIELD_NAME"),
          new ClientDialogCallback().SaveConfirmation);
      }
      else
      {
        Save(refresh);
        if (callback != null)
          callback();
      }
    }

    protected virtual void SaveFormStructureAndClose()
    {
      Context.ClientPage.Modified = false;
      SettingsEditor.IsModifiedActions = false;
      SaveFormStructure(false, CloseFormWebEdit);
    }

    [HandleMessage("item:save", true)]
    private void SaveMessage(ClientPipelineArgs args)
    {
      SaveFormStructure(true, null);
      SheerResponse.Eval("Sitecore.FormBuilder.updateStructure(true);");
    }

    [HandleMessage("item:selectlanguage", true)]
    private void SelectLanguage(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Run.SetLanguage(this, GetCurrentItem().Uri);
    }

    private void UpdateRibbon()
    {
      var currentItem = GetCurrentItem();
      var ctl = new Ribbon
      {
        ID = "FormDesigneRibbon",
        CommandContext = new CommandContext(currentItem)
      };
      var item = Context.Database.GetItem(RibbonPath);
      Error.AssertItemFound(item, RibbonPath);
      var flag = !string.IsNullOrEmpty(SettingsEditor.TitleName);
      ctl.CommandContext.Parameters.Add("title", flag.ToString());
      var flag2 = !string.IsNullOrEmpty(SettingsEditor.Introduce);
      ctl.CommandContext.Parameters.Add("intro", flag2.ToString());
      var flag3 = !string.IsNullOrEmpty(SettingsEditor.Footer);
      ctl.CommandContext.Parameters.Add("footer", flag3.ToString());
      ctl.CommandContext.Parameters.Add("id", currentItem.ID.ToString());
      ctl.CommandContext.Parameters.Add("la", currentItem.Language.Name);
      ctl.CommandContext.Parameters.Add("vs", currentItem.Version.Number.ToString());
      ctl.CommandContext.Parameters.Add("db", currentItem.Database.Name);
      ctl.CommandContext.RibbonSourceUri = item.Uri;
      ctl.ShowContextualTabs = false;
      RibbonPanel.InnerHtml = HtmlUtil.RenderControl(ctl);
    }

    private void UpdateSubmit()
    {
      SettingsEditor.FormID = CurrentItemID;
      SettingsEditor.UpdateCommands(SettingsEditor.SaveActions, builder.FormStucture.ToXml(), true);
    }

    private void UpgradeToSection(string parent, string id)
    {
      builder.UpgradeToSection(id);
    }

    [HandleMessage("forms:validatetext", true)]
    private void ValidateText(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.IsPostBack)
        SettingsEditor.Validate(args.Parameters["ctrl"]);
    }

    private void WarningEmptyForm()
    {
      builder.ShowEmptyForm();
      Control control = SettingsEditor.ShowEmptyForm();
      Context.ClientPage.ClientResponse.SetOuterHtml(control.ID, control);
    }

    [Serializable]
    public class ClientDialogCallback
    {
      public delegate void Action();

      private readonly string id;
      private readonly string newTypeID;
      private readonly string oldTypeID;

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
        SheerResponse.Eval(GetUpdateTypeScript(res, id, oldTypeID, newTypeID));
      }

      public void SaveConfirmation(string result)
      {
        if (result == "yes")
        {
          savedDesigner.Save(true);
          if (saveCallback != null)
            saveCallback();
        }
      }
    }
  }
}