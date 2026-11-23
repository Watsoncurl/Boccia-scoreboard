using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BocciaClient;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra el cliente de la API y la conexión SignalR (sección "BocciaServer").</summary>
    public static IServiceCollection AddBocciaClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BocciaServerOptions>(configuration.GetSection(BocciaServerOptions.SectionName));

        services.AddHttpClient<BocciaApiClient>((sp, http) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<BocciaServerOptions>>().Value.BaseUrl;
            http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(15);
        });

        // Scoped = una conexión por circuito de Blazor Server (por pestaña del navegador).
        services.AddScoped<ScoreboardConnection>();
        return services;
    }
}
