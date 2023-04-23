﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using HtmlAgilityPack;

namespace Elib2Ebook.Extensions; 

public static class StringExtensions {
    /// <summary>
    /// Конвертация строки в Html документ
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static HtmlDocument AsHtmlDoc(this string self) {
        var doc = new HtmlDocument();
        doc.LoadHtml(self.HtmlDecode());
        return doc;
    }

    /// <summary>
    /// Конвертация строки в Xhtml документ
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static HtmlDocument AsXHtmlDoc(this string self) {
        HtmlNode.ElementsFlags.Remove("style");
        HtmlNode.ElementsFlags.Remove("title");

        var doc = new HtmlDocument {
            OptionFixNestedTags = true,
            OptionAutoCloseOnEnd = true,
            OptionOutputAsXml = true,
        };

        doc.LoadHtml(self);
        return doc;
    }

    /// <summary>
    /// Удаление из строки запрещенных символов для пути
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string RemoveInvalidChars(this string self) {
        var sb = new StringBuilder(self);
        foreach (var invalidFileNameChar in Path.GetInvalidFileNameChars().Union(new[]{'"'})) {
            sb.Replace(invalidFileNameChar, ' ');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Обертывание строки кавычками
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string CoverQuotes(this string self) {
        return "\"" + self + "\"";
    }
        
    /// <summary>
    /// Обрезка строки
    /// </summary>
    /// <param name="self"></param>
    /// <param name="lenght"></param>
    /// <returns></returns>
    public static string Crop(this string self, int lenght) {
        return self.Length > lenght ? self[..lenght] : self;
    }

    /// <summary>
    /// HtmlDecode
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string HtmlDecode(this string self) {
        if (string.IsNullOrWhiteSpace(self)) {
            return self;
        }
        
        var temp = HttpUtility.HtmlDecode(self.Replace("&gt;", "").Replace("&lt;", ""));
        while (temp != self) {
            self = temp;
            temp = HttpUtility.HtmlDecode(self.Replace("&gt;", "").Replace("&lt;", ""));
        }
        
        return self.Trim();
    }

    public static string CoverTag(this string self, string tag) {
        return string.IsNullOrEmpty(tag) ? self : $"<{tag}>{self}</{tag}>";
    }

    public static string CleanInvalidXmlChars(this string self) {
        return string.IsNullOrWhiteSpace(self) ? self : Regex.Replace(self, "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]", string.Empty, RegexOptions.Compiled);
    }
    
    public static string ReplaceNewLine(this string self) {
        return string.IsNullOrWhiteSpace(self) ? self : Regex.Replace(self, "\t|\n", " ").CollapseWhitespace().Trim();
    }
        
    /// <summary>
    /// HtmlEncode
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static string HtmlEncode(this string self) {
        return HttpUtility.HtmlEncode(self.Trim());
    }

    public static string CollapseWhitespace(this string self) {
        return !string.IsNullOrEmpty(self) ? Regex.Replace(self, @"\s+", " ") : self;
    }

    public static T Deserialize<T>(this string self) {
        return JsonSerializer.Deserialize<T>(self);
    }

    public static Uri AsUri(this string self) {
        return new(self);
    }

    public static string PrettyHtml(this string self) {
        var document = new HtmlParser().ParseDocument(self);

        using var sw = new StringWriter();
        document.ToHtml(sw, new PrettyMarkupFormatter());

        var result = sw.ToString();
        return result;
    }

    public static string RemoveWithRegexRules(this string self, string filePath)
    {
        if (File.Exists(filePath))
        {
            var rules = File.ReadAllLines(filePath);

            var text = self;
            string rule = null;
            string replace = null;

            for (int i = 0; i < rules.Length; i++)
            {
                rule = null;
                replace = string.Empty;
                string currentRule = rules[i].Trim();

                if (currentRule.StartsWith("rule:"))
                {
                    rule = currentRule.Substring(5);
                    if (i < rules.Length - 1 && rules[i + 1].Trim().StartsWith("replace:"))
                    {
                        replace = rules[i + 1].Trim().Substring(8);
                        i++;
                    }
                }
                else
                {
                    continue;
                }
                text = Regex.Replace(text, rule, replace);
            }
            return text;
        } else
        {
            return self;
        }
    }
}