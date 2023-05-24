using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Text.Json;

namespace Elib2Ebook.Extensions; 

public static class HttpClientExtensions {
    private const int MAX_TRY_COUNT = 5;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    private static TimeSpan GetTimeout(TimeSpan errorTimeout) {
        return errorTimeout == default ? DefaultTimeout : errorTimeout;
    }

    public static async Task SleepTimeout()
    {
        float sleepBetweenRequests = Program.Options.SleepBetweenRequests;
        if (sleepBetweenRequests > 0)
        {
            int sleepBetweenRequestsMs = (int)(sleepBetweenRequests * 1000);
            int progressBarWidth = 40; // Ширина прогресс-бара в символах
            int iterEveryMs = 100;

            for (int i = 0; i <= sleepBetweenRequestsMs / iterEveryMs; i++)
            {
                float progress = (float)i / (sleepBetweenRequestsMs / iterEveryMs);
                int completedWidth = (int)(progress * progressBarWidth);
                int remainingWidth = progressBarWidth - completedWidth;
                double secondsRemaining = (double)(sleepBetweenRequestsMs - (i * iterEveryMs)) / 1000;

                Console.Write($"    После запроса в интернет засыпаю ["); ;
                Console.Write(new string('#', completedWidth));
                Console.Write(new string('-', remainingWidth));
                Console.Write($"] {progress * 100:F0}% ({secondsRemaining:F1} сек)");

                if (i < sleepBetweenRequestsMs / iterEveryMs)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                await Task.Delay(iterEveryMs);
            }

            Console.WriteLine($"; Готово");
        }
    }

        public static async Task<HttpResponseMessage> GetWithTriesAsync(this HttpClient client, Uri url, TimeSpan errorTimeout = default, bool use_cache = true) {
        // функция используется в запросах внутри этого класса
        // в таком случае только если данная функция используется напрямую можно использовать кэш
        // иначе кэш будет дублироваться несколько раз
        bool UseCache = (Program.Options.UseCacheDir != "") && (use_cache);
        string saveResponse = "";
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "GetWithTriesAsync:         ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            saveResponse = $"{directory}/{url.ToString().RemoveInvalidCharsPath()}.bin";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                var cachedContent = await File.ReadAllBytesAsync(saveResponse);
                var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK);
                cachedResponse.Content = new ByteArrayContent(cachedContent);
                return cachedResponse;
            }
        }

        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try {
                var response = await client.GetAsync(url);

                if (UseCache)
                {
                    Console.WriteLine($"{prefix}    Считываю из Интернета: {url.ToString()}");
                }
                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }

                if (UseCache)
                {
                    Console.WriteLine($"{prefix}   Сохраняю файл на диск: {saveResponse}");
                    saveResponse.Makedirs();
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(saveResponse, content);
                }
                await SleepTimeout();
                return response;
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                ProcessTimeout(client);
                await Task.Delay(GetTimeout(errorTimeout));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                await Task.Delay(GetTimeout(errorTimeout));
            }
            await SleepTimeout();
        }

        return default;
    }

    public static async Task<HttpResponseMessage> SendWithTriesAsync(this HttpClient client, Func<HttpRequestMessage> message, TimeSpan errorTimeout = default) {
        bool UseCache = Program.Options.UseCacheDir != "";
        string saveResponse = "";
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "SendWithTriesAsync:        ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            saveResponse = $"{directory}/{message().RequestUri.ToString().RemoveInvalidCharsPath()}.bin";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                var cachedContent = await File.ReadAllBytesAsync(saveResponse);
                var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK);
                cachedResponse.Content = new ByteArrayContent(cachedContent);
                return cachedResponse;
            }
        }

        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.SendAsync(message());

                if (UseCache)
                {
                    Console.WriteLine($"{prefix}    Считываю из Интернета: {message().RequestUri}");
                }
                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    continue;
                }
                if (UseCache)
                {
                    Console.WriteLine($"{prefix}    Сохраняю файл на диск: {saveResponse}");
                    saveResponse.Makedirs();
                    var content = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(saveResponse, content);
                }

                await SleepTimeout();
                return response;
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                ProcessTimeout(client);
                await Task.Delay(GetTimeout(errorTimeout));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                await Task.Delay(GetTimeout(errorTimeout));
            }
            await SleepTimeout();
        }

        return default;
    }
        
    public static async Task<HttpResponseMessage> PostWithTriesAsync(this HttpClient client, Uri url, HttpContent content, TimeSpan errorTimeout = default) {
        bool UseCache = (Program.Options.UseCacheDir != "");
        string saveResponse = "";
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "PostWithTriesAsync:        ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            saveResponse = $"{directory}/{url.ToString().RemoveInvalidCharsPath()}.bin";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                var cachedContent = await File.ReadAllBytesAsync(saveResponse);
                var cachedResponse = new HttpResponseMessage(HttpStatusCode.OK);
                cachedResponse.Content = new ByteArrayContent(cachedContent);
                return cachedResponse;
            }
        }

        for (var i = 0; i < MAX_TRY_COUNT; i++) {
            try { 
                var response = await client.PostAsync(url, content);

                if (UseCache)
                {
                    Console.WriteLine($"{prefix}    Считываю из Интернета: {url.ToString()}");
                }
                if (response.StatusCode != HttpStatusCode.OK) {
                    await Task.Delay(GetTimeout(errorTimeout));
                    if (i == MAX_TRY_COUNT - 1) {
                        return response;
                    }
                        
                    continue;
                }

                if (UseCache)
                {
                    Console.WriteLine($"{prefix}   Сохраняю файл на диск: {saveResponse}");
                    saveResponse.Makedirs();
                    var content_resp = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(saveResponse, content_resp);
                }
                await SleepTimeout();
                return response;
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                ProcessTimeout(client);
                await Task.Delay(GetTimeout(errorTimeout));
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                await Task.Delay(GetTimeout(errorTimeout));
            }
            await SleepTimeout();
        }

        return default;
    }

    private static void ProcessTimeout(HttpClient client) {
        Console.WriteLine($"Сервер не успевает ответить за {client.Timeout.Seconds} секунд. Попробуйте увеличить Timeout с помощью параметра -t");
    }
        
    public static async Task<HtmlDocument> GetHtmlDocWithTriesAsync(this HttpClient client, Uri url, Encoding encoding = null) {
        bool UseCache = (Program.Options.UseCacheDir != "");
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "GetHtmlDocWithTriesAsync:  ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var saveResponse = $"{directory}/{url.ToString().RemoveInvalidCharsPath()}.html";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                return await File.ReadAllTextAsync(saveResponse).ContinueWith(t => t.Result.AsHtmlDoc());
            }
            Console.WriteLine($"{prefix}    Считываю из Интернета: {url.ToString()}");
            using var response = await client.GetWithTriesAsync(url, use_cache: false);
            var res_htmp = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));

            Console.WriteLine($"{prefix}    Сохраняю файл на диск: {saveResponse}");
            saveResponse.Makedirs();
            await File.WriteAllTextAsync(saveResponse, res_htmp.DocumentNode.OuterHtml);
            return res_htmp;
        }
        else
        {
            using var response = await client.GetWithTriesAsync(url, use_cache: UseCache);
            var res_htmp = await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
            return res_htmp;
        }
    }
    
    public static async Task<T> GetFromJsonWithTriesAsync<T>(this HttpClient client, Uri url) {
        bool UseCache = (Program.Options.UseCacheDir != "");
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "GetFromJsonWithTriesAsync: ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var saveResponse = $"{directory}/{url.ToString().RemoveInvalidCharsPath()}.json";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                var js = await File.ReadAllTextAsync(saveResponse).ContinueWith(t => t.Result);
                return JsonSerializer.Deserialize<T>(js);
            }

            Console.WriteLine($"{prefix}    Считываю из Интернета: {url.ToString()}");
            using var response = await client.GetWithTriesAsync(url, use_cache: false);
            var res_js = await response.Content.ReadFromJsonAsync<T>();

            Console.WriteLine($"{prefix}    Сохраняю файл на диск: {saveResponse}");
            saveResponse.Makedirs();
            await File.WriteAllTextAsync(saveResponse, JsonSerializer.Serialize<T>(res_js));

            return res_js;
        }
        else
        {
            using var response = await client.GetWithTriesAsync(url, use_cache: UseCache);
            var res_js = await response.Content.ReadFromJsonAsync<T>();
            return res_js;
        } 
    }
    
    public static async Task<T> GetFromJsonAsync<T>(this HttpClient client, Uri url) {
        bool UseCache = (Program.Options.UseCacheDir != "");
        string prefix = "";
        if (Program.Options.debug_add_function_prefix)
        {
            prefix = "GetFromJsonAsync:          ";
        }

        if (UseCache)
        {
            string directory = Program.Options.UseCacheDir;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var saveResponse = $"{directory}/{url.ToString().RemoveInvalidCharsPath()}.json";
            if (File.Exists(saveResponse))
            {
                Console.WriteLine($"{prefix}    Считываю из CACHE:     {saveResponse}");
                var js = await File.ReadAllTextAsync(saveResponse).ContinueWith(t => t.Result);
                return JsonSerializer.Deserialize<T>(js);
            }
            Console.WriteLine($"{prefix}    Считываю из Интернета: {url.ToString()}");
            using var response = await client.GetWithTriesAsync(url, use_cache: false);
            var res_js = await response.Content.ReadFromJsonAsync<T>();

            Console.WriteLine($"{prefix}    Сохраняю файл на диск: {saveResponse}");
            saveResponse.Makedirs();
            await File.WriteAllTextAsync(saveResponse, JsonSerializer.Serialize<T>(res_js));

            return res_js;
        } 
        else
        {
            using var response = await client.GetWithTriesAsync(url, use_cache: UseCache);
            var res_js = await response.Content.ReadFromJsonAsync<T>();
            return res_js;
        }
    }

    public static async Task<HtmlDocument> PostHtmlDocWithTriesAsync(this HttpClient client, Uri url, HttpContent content, Encoding encoding = null) {
        using var response = await client.PostWithTriesAsync(url, content);
        return await response.Content.ReadAsStreamAsync().ContinueWith(t => t.Result.AsHtmlDoc(encoding));
    }
}