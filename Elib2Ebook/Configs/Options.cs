using System.Collections.Generic;
using CommandLine;

namespace Elib2Ebook.Configs; 

public class Options {
    [Option('u', "url", Required = true, HelpText = "Ссылка на книгу", Separator = ',')]
    public IEnumerable<string> Url { get; set; }
    
    [Option('f', "format", Required = true, HelpText = "Формат для сохранения книги. Допустимые значения: epub, fb2, cbz, json", Separator = ',')]
    public IEnumerable<string> Format { get; set; }
    
    [Option('l', "login", Required = false, HelpText = "Логин от системы")]
    public string Login { get; set; }
        
    [Option('p', "password", Required = false, HelpText = "Пароль от системы")]
    public string Password { get; set; }
        
    [Option("proxy", Required = false, HelpText = "Прокси в формате <host>:<port>")]
    public string Proxy { get; set; }
        
    [Option('s', "save", Required = false, HelpText = "Директория для сохранения книги")]
    public string SavePath { get; set; }

    [Option('c', "cover", Required = false, HelpText = "Сохранить обложку книги в отдельный файл")]
    public bool Cover { get; set; }
    
    [Option('t', "timeout", Required = false, HelpText = "Timeout для запросов в секундах", Default = 5)]
    public int Timeout { get; set; }
    
    [Option("no-image", Required = false, HelpText = "Не загружать картинки")]
    public bool NoImage { get; set; }

    [Option("start", Required = false, HelpText = "Стартовый номер главы")]
    public int? Start { get; set; }
    
    [Option("end", Required = false, HelpText = "Конечный номер главы")]
    public int? End { get; set; }
    
    [Option("temp", Required = false, HelpText = "Папка для временного хранения картинок")]
    public string TempPath { get; set; }
    
    [Option("save-temp", Required = false, HelpText = "Сохранить ни временные файлы", Default = false)]
    public bool SaveTemp { get; set; }
    
    [Option("use-cache-dir", Required = false, HelpText = "Сохранить запросы на диск и использовать сохраненное как кэш", Default = "")]
    public string UseCacheDir { get; set; }

    [Option("book-name", Required = false, HelpText = "Установить имя файла для сохранения книги, иначе имя файла будет рассчитано автоматически", Default = "")]
    public string BookName { get; set; }

    [Option("sleep-between-requests", Required = false, HelpText = "Timeout в секундах между запросами в интернет", Default = 0)]
    public float SleepBetweenRequests { get; set; }

    [Option("debug-add-function-prefix", Required = false, HelpText = "Добавлять название функции в логи", Default = false)]
    public bool debug_add_function_prefix { get; set; }
}