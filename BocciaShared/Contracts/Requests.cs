using System.ComponentModel.DataAnnotations;
using BocciaShared.Domain;

namespace BocciaShared.Contracts;

public class CreateMatchRequest
{
    /// <summary>Código de la división, por ejemplo "BC1" o "PAIRS_BC3".</summary>
    [Required, StringLength(20)]
    public string DivisionCode { get; set; } = "";

    [StringLength(20)]
    public string? Court { get; set; }

    [StringLength(100)]
    public string? Referee { get; set; }

    public DateTime? ScheduledAt { get; set; }

    [Required]
    public CreateSideRequest Red { get; set; } = new();

    [Required]
    public CreateSideRequest Blue { get; set; } = new();
}

public class CreateSideRequest
{
    /// <summary>Nombre que muestra el marcador (deportista, pareja o equipo).</summary>
    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = "";

    /// <summary>Titulares, en orden de número de jugador. Opcional: puede quedar vacío.</summary>
    public List<int> AthleteIds { get; set; } = [];

    public List<int> SubstituteIds { get; set; } = [];
}

public class RegisterThrowRequest
{
    [Required]
    public Side? Side { get; set; }

    public BallType BallType { get; set; } = BallType.Colour;

    public int? AthleteId { get; set; }

    public ThrowResult Result { get; set; } = ThrowResult.InPlay;

    [Range(0, 20_000)]
    public int? DistanceToJackMm { get; set; }
}

public class UpdateThrowRequest
{
    public ThrowResult Result { get; set; } = ThrowResult.InPlay;

    [Range(0, 20_000)]
    public int? DistanceToJackMm { get; set; }
}

public class FinishEndRequest
{
    /// <summary>Mediciones al cierre del parcial: resultado y distancia de cada bola.</summary>
    public List<ThrowMeasurement> Measurements { get; set; } = [];

    /// <summary>
    /// Puntaje manual decidido por el árbitro. Si se indica, deben venir ambos
    /// y reemplazan el cálculo automático.
    /// </summary>
    [Range(0, 20)]
    public int? RedPointsOverride { get; set; }

    [Range(0, 20)]
    public int? BluePointsOverride { get; set; }
}

public class ThrowMeasurement
{
    public int ThrowId { get; set; }

    public ThrowResult Result { get; set; } = ThrowResult.InPlay;

    [Range(0, 20_000)]
    public int? DistanceToJackMm { get; set; }
}

public class AddPenaltyRequest
{
    [Required]
    public Side? PenalizedSide { get; set; }

    [Required]
    public PenaltyType? PenaltyType { get; set; }

    public int? AthleteId { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }
}

public class StartTieBreakRequest
{
    /// <summary>Lado que lanza el jack en el desempate.</summary>
    [Required]
    public Side? JackSide { get; set; }
}

public class CreateAthleteRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [RegularExpression("^BC[1-4]$", ErrorMessage = "La clasificación debe ser BC1, BC2, BC3 o BC4.")]
    public string? Classification { get; set; }

    [StringLength(100)]
    public string? Club { get; set; }
}
