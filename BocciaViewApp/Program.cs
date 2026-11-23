using BocciaClient;
using BocciaViewApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Cliente de la API y conexión SignalR con BocciaServer (sección "BocciaServer" de appsettings).
builder.Services.AddBocciaClient(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// Sin redirección a HTTPS: en la red local los dispositivos se conectan por HTTP.

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
