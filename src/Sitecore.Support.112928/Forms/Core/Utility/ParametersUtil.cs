using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using Sitecore.Collections;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Globalization;
using WebUtil = Sitecore.Web.WebUtil;

namespace Sitecore.Support.Forms.Core.Utility
{
  public class ParametersUtil
  {
    private static void AddNode(HtmlDocument doc, string tag, string value, bool applyUrlEncode)
    {
      var newChild = doc.CreateElement(tag);
      newChild.InnerHtml = applyUrlEncode ? HttpUtility.UrlEncode(value) : value;
      doc.DocumentNode.AppendChild(newChild);
    }

    public static string ConcatXml(string a, string b)
    {
      string[] xmls = {a, b};
      return PairArrayToXml(XmlToPairArray(xmls));
    }

    private static string Decode(string xml, bool applyUrlDecode)
    {
      var s = xml;
      s = HttpUtility.HtmlDecode(s);
      if (!applyUrlDecode || string.IsNullOrEmpty(s))
        return s;
      var document = new HtmlDocument();
      using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
      {
        document.LoadHtml(s);
      }
      foreach (var local1 in document.DocumentNode.ChildNodes)
        local1.InnerHtml = HttpUtility.UrlDecode(local1.InnerHtml);
      return document.DocumentNode.InnerHtml;
    }

    private static string Encode(HtmlDocument doc)
    {
      if ((doc.DocumentNode == null ? null : doc.DocumentNode.FirstChild) == null)
        return string.Empty;
      return Encode(doc.DocumentNode.InnerHtml);
    }

    private static string Encode(string xml) =>
      HttpUtility.HtmlEncode(xml);

    public static string EncodeNodesText(string xml)
    {
      if (string.IsNullOrEmpty(xml))
        return xml;
      return xml.Replace("[<&]", "[&lt;&amp;]");
    }

    public static string Escape(string value) =>
      HttpUtility.HtmlEncode(HttpUtility.UrlEncode(value));

    internal static string Expand(string parameters) =>
      Expand(parameters, false);

    internal static string Expand(string parameters, bool allowUrlDEcoding)
    {
      Assert.ArgumentNotNull(parameters, "parameters");
      var collection = XmlToNameValueCollection(parameters, allowUrlDEcoding);
      collection.ForEach(delegate(string k, string v)
      {
        if (SessionUtil.IsSessionKey(v))
          collection[k] = WebUtil.GetSessionString(v, v);
      });
      return NameValueCollectionToXml(collection);
    }

    public static string HtmlNodeCollectionToXml(IEnumerable<HtmlNode> nodes)
    {
      Assert.IsNotNull(nodes, "nodes");
      var values = new NameValueCollection();
      foreach (var node in nodes)
        if (node != null)
          values[node.Name] = node.InnerHtml;
      return NameValueCollectionToXml(values);
    }

    public static IDictionary<string, string> ItemsValuesXmlToDictionaryList(string xml)
    {
      var dictionary = new Dictionary<string, string>();
      var queries = HttpUtility.UrlDecode(xml);
      if (queries == null)
        return dictionary;
      if (!queries.StartsWith(StaticSettings.SourceMarker))
        return QueryManager.Select(QuerySettings.ParseRange(queries)).ToDictionary();
      return
        Sitecore.Form.Core.Utility.Utils.GetItemsName(queries.Substring(StaticSettings.SourceMarker.Length))
          .ToDictionary(i => i, i => i);
    }

    public static string NameValueCollectionToXml(NameValueCollection values) =>
      NameValueCollectionToXml(values, false);

    public static string NameValueCollectionToXml(NameValueCollection values, bool applyUrlEncode)
    {
      Assert.ArgumentNotNull(values, "values");
      var doc = new HtmlDocument();
      foreach (var str in values.AllKeys)
        AddNode(doc, str, values[str], applyUrlEncode);
      return Encode(doc);
    }

    public static string PairArrayToXml(IEnumerable<Pair<string, string>> values) =>
      PairArrayToXml(values, false, false, true);

    public static string PairArrayToXml(IEnumerable<Pair<string, string>> values, bool applyUrlEncode,
      bool encodeNodeText = false, bool encodeDoc = true)
    {
      Assert.ArgumentNotNull(values, "values");
      var doc = new HtmlDocument();
      foreach (var pair in values)
      {
        Assert.ArgumentNotNullOrEmpty(pair.Part1, "tagName");
        var xml = pair.Part2 ?? string.Empty;
        AddNode(doc, pair.Part1, encodeNodeText ? Encode(xml) : xml, applyUrlEncode);
      }
      if (!encodeDoc)
        return doc.DocumentNode.InnerHtml;
      return Encode(doc);
    }

    public static string StringArrayToXml(IEnumerable<string> values, string tagName) =>
      StringArrayToXml(values, tagName, false);

    public static string StringArrayToXml(IEnumerable<string> values, string tagName, bool applyUrlEncode)
    {
      Assert.ArgumentNotNull(values, "values");
      Assert.ArgumentNotNullOrEmpty(tagName, "tagName");
      var doc = new HtmlDocument();
      foreach (var str in values)
        AddNode(doc, tagName, str ?? string.Empty, applyUrlEncode);
      return Encode(doc);
    }

    public static string Unescape(string value) =>
      HttpUtility.HtmlDecode(HttpUtility.UrlDecode(value));

    public static Dictionary<string, string> XmlToDictionary(string xml, bool applyUrlDecode = false)
    {
      var dictionary = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(xml))
      {
        var document = new HtmlDocument();
        using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
        {
          document.LoadHtml(xml.StartsWith("<") ? xml : Decode(xml, applyUrlDecode));
        }
        foreach (var node in from x in document.DocumentNode.ChildNodes
          where x.Name != "#text"
          select x)
          dictionary.Add(node.Name.ToLower(), Decode(node.InnerHtml, applyUrlDecode));
      }
      return dictionary;
    }

    public static Dictionary<string, string> XmlToDictionaryWithOriginalNames(string xml, bool applyUrlDecode = false)
    {
      var dictionary = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(xml))
      {
        var document = new HtmlDocument();
        using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
        {
          document.LoadHtml(xml.StartsWith("<") ? xml : Decode(xml, applyUrlDecode));
        }
        foreach (var node in from x in document.DocumentNode.ChildNodes
          where x.Name != "#text"
          select x)
          dictionary.Add(node.OriginalName, node.InnerHtml);
      }
      return dictionary;
    }

    public static IEnumerable<HtmlNode> XmlToHtmlNodeCollection(string xml) =>
      XmlToHtmlNodeCollection(xml, false);

    public static IEnumerable<HtmlNode> XmlToHtmlNodeCollection(string xml, bool applyUrlDecode)
    {
      var list = new List<HtmlNode>();
      if (!string.IsNullOrEmpty(xml))
      {
        var document = new HtmlDocument();
        using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
        {
          document.LoadHtml(Decode(xml, applyUrlDecode));
          list.AddRange(document.DocumentNode.ChildNodes);
        }
      }
      return list;
    }

    public static NameValueCollection XmlToNameValueCollection(string xml) =>
      XmlToNameValueCollection(xml, false);

    public static NameValueCollection XmlToNameValueCollection(string xml, bool applyUrlDecode)
    {
      var values = new NameValueCollection();
      foreach (var pair in XmlToPairArray(xml, applyUrlDecode))
        values[pair.Part1] = pair.Part2;
      return values;
    }

    public static IEnumerable<Pair<string, string>> XmlToPairArray(string xml) =>
      XmlToPairArray(xml, false);

    public static IEnumerable<Pair<string, string>> XmlToPairArray(params string[] xmls)
    {
      var dictionary = new Dictionary<string, string>();
      var strArray = xmls;
      for (var i = 0; i < strArray.Length; i++)
        foreach (var pair in XmlToPairArray(strArray[i]))
          if (dictionary.ContainsKey(pair.Part1))
            dictionary[pair.Part1] = pair.Part2;
          else
            dictionary.Add(pair.Part1, pair.Part2);
      return from p in dictionary select new Pair<string, string>(p.Key, p.Value);
    }

    public static IEnumerable<Pair<string, string>> XmlToPairArray(string xml, bool applyUrlDecode)
    {
      Func<HtmlNode, Pair<string, string>> selector = null;
      var list = new List<Pair<string, string>>();
      if (!string.IsNullOrEmpty(xml))
      {
        var document = new HtmlDocument();
        using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
        {
          document.LoadHtml(xml.StartsWith("<") ? xml : Decode(xml, applyUrlDecode));
        }
        if (selector == null)
          selector = node => new Pair<string, string>(node.Name, Decode(node.InnerHtml, applyUrlDecode));
        list.AddRange(document.DocumentNode.ChildNodes.Select(selector));
      }
      return list;
    }

    public static IEnumerable<string> XmlToStringArray(string xml)
    {
      if (string.IsNullOrEmpty(xml))
        return new List<string>();
      return XmlToStringArray(xml, false);
    }

    public static IEnumerable<string> XmlToStringArray(string xml, bool applyUrlDecode)
    {
      var document = new HtmlDocument();
      using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
      {
        document.LoadHtml(Decode(xml, applyUrlDecode));
      }
      return (from node in document.DocumentNode.ChildNodes select node.InnerHtml).ToList();
    }
  }
}