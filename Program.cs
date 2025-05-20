using Nest; // NEST k�t�phanesini kullanmak i�in
using System; // Uri s�n�f�n� kullanmak i�in

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(); // Razor Pages servisini ekle

// === Elasticsearch �stemcisini Yap�land�r ve Ekle ===
// appsettings.json'dan Elasticsearch ayarlar�n� oku
var esUri = builder.Configuration["ElasticsearchSettings:Uri"];
var esDefaultIndex = builder.Configuration["ElasticsearchSettings:DefaultIndex"];

// NEST ba�lant� ayarlar�n� olu�tur
var settings = new ConnectionSettings(new Uri(esUri))
    .DefaultIndex(esDefaultIndex)
    // Gerekirse di�er NEST ayarlar� buraya eklenebilir
    // �rne�in, temel kimlik do�rulama i�in:
    // .BasicAuthentication("kullanici_adi", "sifre")
    // Bizim Elasticsearch'imiz �ifresiz oldu�u i�in bu gerekli de�il.
    .PrettyJson() // Elasticsearch'e g�nderilen ve al�nan JSON'� okunabilir formatlar (geli�tirme i�in faydal�)
    .DisableDirectStreaming(); // �stek ve yan�tlar� loglamak veya debug etmek i�in (geli�tirme i�in faydal�)

var esClient = new ElasticClient(settings);

// Elasticsearch istemcisini singleton olarak servislere ekle
// IElasticClient aray�z� �zerinden kullan�lacak
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