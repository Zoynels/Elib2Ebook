using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;
using Elib2Ebook.Types.RanobeHub;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace Elib2Ebook.Logic.Getters; 

public class RanobeHubGetter : GetterBase {
    public RanobeHubGetter(BookGetterConfig config) : base(config) { }
    protected override Uri SystemUrl => new("https://ranobehub.org");
    public override async Task<Book> Get(Uri url) {
        url = SystemUrl.MakeRelativeUri($"/ranobe/{GetId(url)}");
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url);

        var book = new Book(url) {
            Cover = await GetCover(doc, url),
            Chapters = await FillChapters(doc, url),
            Title = doc.GetTextBySelector("h1.header"),
            Author = new Author("RanobeHub")
        };
            
        return book;
    }

    private async Task<IEnumerable<Chapter>> FillChapters(HtmlDocument doc, Uri url) {
        var result = new List<Chapter>();

        foreach (var ranobeChapter in await GetToc(doc)) {
            Console.WriteLine($"Загружаю главу {ranobeChapter.Name}");
            var chapter = new Chapter();
            
            var chapterDoc = await GetChapter(ranobeChapter.Url);
            chapter.Images = await GetImages(chapterDoc, url);
            chapter.Content = chapterDoc.DocumentNode.InnerHtml;
            chapter.Title = ranobeChapter.Name;
            chapter.Url = ranobeChapter.Url;
            chapter.BookUrl = url.ToString();

            result.Add(chapter);
        }

        return result;
    }

    private async Task<HtmlDocument> GetChapter(string url) {
        var doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AsUri());
        while (doc.QuerySelector("div[data-callback=correctCaptcha]") != null) {
            Console.WriteLine($"Обнаружена капча. Перейдите по ссылке {url}, введите капчу и нажмите Enter...");
            Console.Read();
            doc = await Config.Client.GetHtmlDocWithTriesAsync(url.AsUri());
        }
        
        var result = doc.QuerySelector("div.container[data-container]").RemoveNodes("div.title-wrapper, div.ads-desktop, div.tablet").InnerHtml.AsHtmlDoc();
        
        foreach (var img in result.QuerySelectorAll("img")) {
            var id = img.Attributes["data-media-id"]?.Value;
            if (string.IsNullOrWhiteSpace(id)) {
                continue;
            }

            if (img.Attributes.Contains("src")) {
                img.Attributes["src"].Value = $"/api/media/{id}";
            } else {
                img.Attributes.Add("src", $"/api/media/{id}");
            }
        }
        
        return result;
    }

    private async Task<IEnumerable<RanobeHubChapter>> GetToc(HtmlDocument doc) {
        var internalId = doc.QuerySelector("html[data-id]").Attributes["data-id"].Value;
        var response = await Config.Client.GetFromJsonAsync<RanobeHubApiResponse>(SystemUrl.MakeRelativeUri($"/api/ranobe/{internalId}/contents"));
        return SliceToc(response?.Volumes.SelectMany(t => t.Chapters).ToList());
    }
    
    private Task<Image> GetCover(HtmlDocument doc, Uri bookUri) {
        var imagePath = doc.QuerySelector("div.poster-slider img")?.Attributes["data-src"]?.Value;
        return !string.IsNullOrWhiteSpace(imagePath) ? SaveImage(bookUri.MakeRelativeUri(imagePath)) : Task.FromResult(default(Image));
    }
}