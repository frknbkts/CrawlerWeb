// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nest;
using ScraperWeb.Models;
using System.Collections.Generic;
using System.Linq; // ToList() i�in
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
        public string? SearchTerm { get; set; } // Kullan�c�n�n girdi�i arama terimi

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
                // E�er arama terimi varsa, ba�l�k (title) ve i�erik (content) alanlar�nda ara
                // Python scriptinizde T�rk�e analiz�r kulland���n�z i�in Elasticsearch bu alanlarda
                // T�rk�e arama yaparken daha iyi sonu�lar verecektir.
                searchResponse = await _elasticClient.SearchAsync<Article>(s => s
                    .Index(esDefaultIndex)
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(SearchTerm) // Aranacak metin
                            .Fields(f => f // Hangi alanlarda aranaca��
                                .Field(ff => ff.Title, boost: 2) // Ba�l�k e�le�melerine daha fazla a��rl�k ver
                                .Field(ff => ff.Content)
                            )
                            .Fuzziness(Fuzziness.Auto) // Yaz�m hatalar�na kar�� tolerans (�rn: ekonom -> ekonomi)
                            .Operator(Operator.Or) // Kelimelerden herhangi biri e�le�irse (Or vs And)
                        )
                    )
                    .Size(20) // En fazla 20 sonu� getir
                              // Arama yap�ld���nda varsay�lan olarak alaka d�zeyine (_score) g�re s�ralan�r.
                              // �sterseniz ek s�ralama kriterleri ekleyebilirsiniz:
                              // .Sort(ss => ss.Descending("_score").ThenBy(s => s.Descending("scraped_date_utc")))
                );
                _logger.LogInformation("'{SearchTerm}' i�in arama yap�ld�.", SearchTerm);
            }
            else
            {
                // Arama terimi yoksa, en son eklenen haberleri listele (mevcut mant�k)
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