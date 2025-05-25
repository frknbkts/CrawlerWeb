using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nest;
using ScraperWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScraperWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IElasticClient _elasticClient;

        public List<Article> Articles { get; set; } = new List<Article>();

        // Arama terimini URL'den almak i�in BindProperty attribute'u
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IElasticClient elasticClient)
        {
            _logger = logger;
            _elasticClient = elasticClient;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Ana sayfa y�kleniyor. Arama Terimi: {SearchTerm}", SearchTerm);

            var esDefaultIndex = _elasticClient.ConnectionSettings.DefaultIndex; // "sozcu_articles"

            ISearchResponse<Article> searchResponse;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                searchResponse = await _elasticClient.SearchAsync<Article>(s => s
                    .Index(esDefaultIndex)
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(SearchTerm)
                            .Fields(f => f // Hangi alanlarda aranaca��
                                .Field(ff => ff.Title, boost: 2) // baslik eslesmeleri daha oncelikli
                                .Field(ff => ff.Content)
                            )
                            .Fuzziness(Fuzziness.Auto) // yazim hatasi tolernas
                            .Operator(Operator.Or) // 2 kelimeli aramalarda bir kelime e�le�mesi yeterli
                        )
                    )
                    .Size(20) // aramalar _scorelere g�re siralanicak

                );
                _logger.LogInformation("'{SearchTerm}' i�in arama yap�ld�.", SearchTerm);
            }
            else
            {
                // Arama terimi yoksa, en son eklenen haberleri listele
                searchResponse = await _elasticClient.SearchAsync<Article>(s => s
                    .Index(esDefaultIndex)
                    .Query(q => q.MatchAll())
                    .Sort(ss => ss.Descending("scraped_date_utc"))
                    .Size(20)
                );
                _logger.LogInformation("Arama terimi yok, son haberler listeleniyor.");
            }

            if (searchResponse.IsValid)
            {
                Articles = searchResponse.Documents.ToList();
                _logger.LogInformation($"{Articles.Count} adet haber (arama/listeleme sonucu) ba�ar�yla �ekildi.");
            }
            else
            {
                _logger.LogError("Elasticsearch'ten arama/listeleme s�ras�nda bir hata olu�tu.");
                if (searchResponse.OriginalException != null)
                {
                    _logger.LogError($"Elasticsearch Hata Detay�: {searchResponse.OriginalException.Message}");
                }
                _logger.LogError($"Elasticsearch Debug Bilgisi: {searchResponse.DebugInformation}");
            }
        }
    }
}