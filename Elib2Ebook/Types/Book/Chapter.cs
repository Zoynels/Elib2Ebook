using System.Collections.Generic;

namespace Elib2Ebook.Types.Book; 

public class Chapter {
    /// <summary>
    /// Адрес книги
    /// </summary>
    public string BookUrl { get; set; }

    /// <summary>
    /// Адрес части
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Название части
    /// </summary>
    public string Title { get; set; }
        
    /// <summary>
    /// Контент части
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Изображения из части
    /// </summary>
    public IEnumerable<Image> Images { get; set; } = new List<Image>();

    /// <summary>
    /// Валидна ли часть
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Content);
}