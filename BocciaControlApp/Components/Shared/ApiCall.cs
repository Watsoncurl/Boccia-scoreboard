using BocciaClient;

namespace BocciaControlApp.Components.Shared;

/// <summary>Traduce los errores de llamadas a la API en mensajes para el operador.</summary>
public static class ApiCall
{
    public static string ErrorMessage(Exception ex) => ex switch
    {
        ApiException api => api.Message,
        HttpRequestException or TaskCanceledException => "No se pudo conectar con el servidor. Verificá que esté en funcionamiento.",
        _ => throw ex,
    };

    public static bool IsApiError(Exception ex) => ex is ApiException or HttpRequestException or TaskCanceledException;
}
