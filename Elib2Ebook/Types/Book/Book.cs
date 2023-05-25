using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elib2Ebook.Configs;
using Elib2Ebook.Extensions;
using Elib2Ebook.Logic.Builders;

namespace Elib2Ebook.Types.Book; 

public class Book {
    /// <summary>
    /// Название книги
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Автор книги
    /// </summary>
    public Author Author { get; set; }
    
    /// <summary>
    /// Описание книги
    /// </summary>
    public string Annotation { get; set; }
        
    /// <summary>
    /// Обложка
    /// </summary>
    public Image Cover { get; set; }
    
    /// <summary>
    /// Серия
    /// </summary>
    public Seria Seria { get; set; }

    /// <summary>
    /// Части
    /// </summary>
    public IEnumerable<Chapter> Chapters { get; set; } = new List<Chapter>();

    /// <summary>
    /// Url расположения книги
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// Язык книги
    /// </summary>
    public string Lang { get; set; } = "ru";

    public Book(Uri url) {
        Url = url;
    }

    /// <summary>
    /// Сохранение книги
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <param name="resourcesPath">Путь к папке с ресурсами</param>
    public async Task Save(BuilderBase builder, Options options, string resourcesPath) {
        var title = $"{Author.Name} - {Title}";
        string book_name = Program.Options.BookName;
        if (book_name == "") {
            book_name = "{Author.Name} - {Title} ({Start}-{End}), [{StartTitle}-{EndTitle}]";
        }

        if (this.Chapters.Count() == 0)
        {
            Console.WriteLine("Получено 0 глав! Выходим с ошибкой!");
            // Выход из программы с кодом 129
            Environment.Exit(129);
        } else { 
            var StartTitle = this.Chapters.FirstOrDefault().Title.RemoveInvalidCharsProtocol().RemoveInvalidChars();
            var EndTitle = this.Chapters.LastOrDefault().Title.RemoveInvalidCharsProtocol().RemoveInvalidChars();

            // только нужно более корректное определение
            // создаем название книги по паттерну
            book_name = book_name.Replace("{Author.Name}", Author.Name);
            book_name = book_name.Replace("{Title}", Title);
            book_name = book_name.Replace("{Start}", Program.Options.BookChapterStart.ToString());
            book_name = book_name.Replace("{End}", Program.Options.BookChapterEnd.ToString());
            book_name = book_name.Replace("{Count}", Program.Options.BookChapterCount.ToString());
            book_name = book_name.Replace("{StartTitle}", StartTitle);
            book_name = book_name.Replace("{EndTitle}", EndTitle);

            await builder
                .AddAuthor(Author)
                .WithBookUrl(Url)
                .WithTitle(Title)
                .WithAnnotation(Annotation)
                .WithCover(Cover)
                .WithLang(Lang)
                .WithSeria(Seria)
                .WithFiles(resourcesPath, "*.ttf")
                .WithFiles(resourcesPath, "*.css")
                .WithChapters(Chapters)
                .Build(options.SavePath, book_name);

            if (options.Cover) {
                await builder.SaveCover(options.SavePath, Cover, book_name);
            }
        }
    }
}