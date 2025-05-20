using Nest; // NEST kütüphanesini kullanmak için
using System; // Uri sýnýfýný kullanmak için

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(); // Razor Pages servisini ekle

// === Elasticsearch Ýstemcisini Yapýlandýr ve Ekle ===
// appsettings.json'dan Elasticsearch ayarlarýný oku
var esUri = builder.Configuration["ElasticsearchSettings:Uri"];
var esDefaultIndex = builder.Configuration["ElasticsearchSettings:DefaultIndex"];

// NEST baðlantý ayarlarýný oluþtur
var settings = new ConnectionSettings(new Uri(esUri))
    .DefaultIndex(esDefaultIndex)
    // Gerekirse diðer NEST ayarlarý buraya eklenebilir
    // Örneðin, temel kimlik doðrulama için:
    // .BasicAuthentication("kullanici_adi", "sifre")
    // Bizim Elasticsearch'imiz þifresiz olduðu için bu gerekli deðil.
    .PrettyJson() // Elasticsearch'e gönderilen ve alýnan JSON'ý okunabilir formatlar (geliþtirme için faydalý)
    .DisableDirectStreaming(); // Ýstek ve yanýtlarý loglamak veya debug etmek için (geliþtirme için faydalý)

var esClient = new ElasticClient(settings);

// Elasticsearch istemcisini singleton olarak servislere ekle
// IElasticClient arayüzü üzerinden kullanýlacak
builder.Services.AddSingleton<IElasticClient>(esClient);
// ======================================================

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();