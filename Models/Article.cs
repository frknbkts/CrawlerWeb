using System.Text.Json.Serialization;

namespace ScraperWeb.Models
{
    public class Article
    {
        [JsonPropertyName("title")] // Elasticsearch'teki alan adıyla eşleşmesi için
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("scraped_date_utc")]
        public DateTime ScrapedDateUtc { get; set; } // Tarih tipinde alabiliriz

        [JsonPropertyName("indexed_at_utc")]
        public DateTime IndexedAtUtc { get; set; } // Tarih tipinde alabiliriz
    }
}
