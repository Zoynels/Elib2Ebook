using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elib2Ebook.Extensions;
using Elib2Ebook.Types.Book;

namespace Elib2Ebook.Logic.Builders;

public class ShortImageConverter : JsonConverter<Image> {
    public override Image Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize<Image>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, Image value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        writer.WriteString(nameof(value.Url), value.Url.ToString());
        writer.WriteString(nameof(value.Directory), value.Directory);
        writer.WriteString(nameof(value.Name), value.Name);
        writer.WriteString(nameof(value.FilePath), value.FilePath);
        writer.WriteEndObject();
    }
}

public class ShortChapterConverter : JsonConverter<Chapter> {
    public override Chapter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return JsonSerializer.Deserialize<Chapter>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, Chapter value, JsonSerializerOptions options) {
        writer.WriteStartObject();
        
        writer.WriteString(nameof(value.Title), value.Title);
        writer.WriteBoolean(nameof(value.IsValid), value.IsValid);
        
        writer.WriteStartArray(nameof(value.Images));
        foreach (var image in value.Images) {
            JsonSerializer.Serialize(writer, image, options);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}

public class JsonLiteBuilder : BuilderBase {
    private readonly Book _book = new(null);
    
    public static BuilderBase Create() {
        return new JsonLiteBuilder();
    }
    
    public override BuilderBase AddAuthor(Author author) {
        _book.Author = author;
        return this;
    }

    public override BuilderBase WithTitle(string title) {
        _book.Title = title;
        return this;
    }

    public override BuilderBase WithCover(Image cover) {
        _book.Cover = cover;
        return this;
    }

    public override BuilderBase WithBookUrl(Uri url) {
        _book.Url = url;
        return this;
    }

    public override BuilderBase WithAnnotation(string annotation) {
        _book.Annotation = annotation;
        return this;
    }

    public override BuilderBase WithFiles(string directory, string searchPattern) {
        return this;
    }

    public override BuilderBase WithChapters(IEnumerable<Chapter> chapters) {
        _book.Chapters = chapters;
        return this;
    }

    public override BuilderBase WithSeria(Seria seria) {
        _book.Seria = seria;
        return this;
    }

    public override BuilderBase WithLang(string lang) {
        _book.Lang = lang;
        return this;
    }

    protected override async Task BuildInternal(string name) {
        await using var file = File.Create(name);
        var jsonSerializerOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new ShortChapterConverter(), new ShortImageConverter() }
        };
        
        await JsonSerializer.SerializeAsync(file, _book, jsonSerializerOptions);
    }

    protected override string GetFileName(string name) {
        return $"{name}.json".RemoveInvalidChars();
    }
}