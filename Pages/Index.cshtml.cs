// Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nest;
using ScraperWeb.Models;
using System.Collections.Generic;
using System.Linq; // ToList() için
using System.Threading.Tasks;

namespace ScraperWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IElasticClient _elasticClient;

        public List<Article> Articles { get; set; } = new List<Article>();

        // Arama terimini URL'den almak için BindProperty attribute'u
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; } // Kullanýcýnýn girdiði arama terimi

        public IndexModel(ILogger<IndexModel> logger, IElasticClient elasticClient)
        {
            _logger = logger;
            _elasticClient = elasticClient;
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Ana sayfa yükleniyor. Arama Terimi: {SearchTerm}", SearchTerm);

            var esDefaultIndex = _elasticClient.ConnectionSettings.DefaultIndex; // "sozcu_articles"

            ISearchResponse<Article> searchResponse;

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                // Eðer arama terimi varsa, baþlýk (title) ve içerik (content) alanlarýnda ara
                // Python scriptinizde Türkçe analizör kullandýðýnýz için Elasticsearch bu alanlarda
                // Türkçe arama yaparken daha iyi sonuçlar verecektir.
                searchResponse = await _elasticClient.SearchAsync<Article>(s => s
                    .Index(esDefaultIndex)
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(SearchTerm) // Aranacak metin
                            .Fields(f => f // Hangi alanlarda aranacaðý
                                .Field(ff => ff.Title, boost: 2) // Baþlýk eþleþmelerine daha fazla aðýrlýk ver
                                .Field(ff => ff.Content)
                            )
                            .Fuzziness(Fuzziness.Auto) // Yazým hatalarýna karþý tolerans (örn: ekonom -> ekonomi)
                            .Operator(Operator.Or) // Kelimelerden herhangi biri eþleþirse (Or vs And)
                        )
                    )
                    .Size(20) // En fazla 20 sonuç getir
                              // Arama yapýldýðýnda varsayýlan olarak alaka düzeyine (_score) göre sýralanýr.
                              // Ýsterseniz ek sýralama kriterleri ekleyebilirsiniz:
                              // .Sort(ss => ss.Descending("_score").ThenBy(s => s.Descending("scraped_date_utc")))
                );
                _logger.LogInformation("'{SearchTerm}' için arama yapýldý.", SearchTerm);
            }
            else
            {
                // Arama terimi yoksa, en son eklenen haberleri listele (mevcut mantýk)
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
                _logger.LogInformation($"{Articles.Count} adet haber (arama/listeleme sonucu) baþarýyla çekildi.");
            }
            else
            {
                _logger.LogError("Elasticsearch'ten arama/listeleme sýrasýnda bir hata oluþtu.");
                if (searchResponse.OriginalException != null)
                {
                    _logger.LogError($"Elasticsearch Hata Detayý: {searchResponse.OriginalException.Message}");
                }
                _logger.LogError($"Elasticsearch Debug Bilgisi: {searchResponse.DebugInformation}");
            }
        }
    }
}