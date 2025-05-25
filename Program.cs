using Nest;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var esUri = builder.Configuration["ElasticsearchSettings:Uri"];
var esDefaultIndex = builder.Configuration["ElasticsearchSettings:DefaultIndex"];

// NEST baðlantý ayarlarý
var settings = new ConnectionSettings(new Uri(esUri))
    .DefaultIndex(esDefaultIndex)
    .PrettyJson() // json prettier
    .DisableDirectStreaming(); // istek ve yanitlar icin log

var esClient = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(esClient);

var app = builder.Build();

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