﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Logic;
using Elib2Ebook.Logic.Builders;
using Elib2Ebook.Logic.Getters;
using Elib2Ebook.Misc.TempFolder;

namespace Elib2Ebook; 

internal static class Program {
    public static Options Options { get; private set; }

    private static async Task Main(string[] args) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(async options => {
                Options = options;
                var cookieContainer = new CookieContainer();
                using var client = GetClient(options, cookieContainer);

                using var getterConfig = new BookGetterConfig(options, client, cookieContainer, TempFolderFactory.Create(options.TempPath, !options.SaveTemp)); 
                using var getter = GetGetter(getterConfig, options.Url.First().AsUri());
                await getter.Init();
                await getter.Authorize();

                foreach (var url in options.Url) {
                    Console.WriteLine($"Начинаю генерацию книги {url.CoverQuotes()}");
                    try {
                        var book = await getter.Get(url.AsUri());
                        foreach (var format in options.Format) {
                            await book.Save(GetBuilder(format), options, "Patterns");
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Генерация книги {url} завершилась с ошибкой. {ex.Message}");
                    }
                }
            });
    }

    private static HttpClient GetClient(Options options, CookieContainer container) {
        var handler = new HttpClientHandler {
            AutomaticDecompression = DecompressionMethods.GZip | 
                                     DecompressionMethods.Deflate |
                                     DecompressionMethods.Brotli,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            CookieContainer = container,
            Proxy = null,
            UseProxy = false
        };

        if (!string.IsNullOrEmpty(options.Proxy)) {
            handler.Proxy = new WebProxy(options.Proxy.AsUri());
            handler.UseProxy = true;
        }

        var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(options.Timeout);
        return client;
    }

    private static BuilderBase GetBuilder(string format) {
        return format.Trim().ToLower() switch {
            "fb2" => Fb2Builder.Create(),
            "epub" => EpubBuilder.Create(FileProvider.Instance.ReadAllText("Patterns/ChapterPattern.xhtml")),
            "json" => JsonBuilder.Create(),
            "cbz" => CbzBuilder.Create(),
            "txt" => TxtBuilder.Create(),
            "json_lite" => JsonLiteBuilder.Create(),
            "json_chapter" => JsonChapterBuilder.Create(),
            _ => throw new ArgumentException("Неизвестный формат", nameof(format))
        };
    }

    private static GetterBase GetGetter(BookGetterConfig config, Uri url) {
        return Assembly.GetAssembly(typeof(GetterBase))!.GetTypes()
                   .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GetterBase)))
                   .Select(type => (GetterBase) Activator.CreateInstance(type, config))
                   .FirstOrDefault(g => g!.IsSameUrl(url)) ??
               throw new ArgumentException("Данная система не поддерживается", nameof(url));
    }
}