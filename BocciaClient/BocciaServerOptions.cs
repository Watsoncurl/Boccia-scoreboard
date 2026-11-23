namespace BocciaClient;

/// <summary>Sección "BocciaServer" de appsettings.json.</summary>
public class BocciaServerOptions
{
    public const string SectionName = "BocciaServer";

    /// <summary>URL base del servidor, por ejemplo http://192.168.0.10:5123.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5123";
}
