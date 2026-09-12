using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        string json = File.ReadAllText(path, Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<BookDto> items;
        try
        {
            items = JsonSerializer.Deserialize<List<BookDto>>(json, options) ?? [];
        }
        catch (JsonException ex)
        {
            return new ImportResult<BookDto>([], [$"не вдалося розібрати JSON: {ex.Message}"]);
        }

        return new ImportResult<BookDto>(items, []);
    }
}