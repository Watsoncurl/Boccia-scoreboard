using System.Net.Http.Json;
using System.Text.Json;

namespace BocciaClient;

/// <summary>Error devuelto por el servidor (regla del juego, validación o recurso inexistente).</summary>
public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    internal static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

            // Errores de validación: { "errors": { "Campo": ["mensaje", ...] } }
            if (problem.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct();
                return new ApiException(status, string.Join(" ", messages));
            }

            if (problem.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } text)
                return new ApiException(status, text);
        }
        catch (JsonException)
        {
            // La respuesta no es ProblemDetails: se usa el mensaje genérico.
        }

        return new ApiException(status, $"El servidor respondió con un error ({status}).");
    }
}
