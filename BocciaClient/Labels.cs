using BocciaShared.Domain;

namespace BocciaClient;

/// <summary>Textos en español para mostrar los valores del dominio.</summary>
public static class Labels
{
    public static string Of(Side side) => side == Side.Red ? "Rojo" : "Azul";

    public static string Of(MatchStatus status) => status switch
    {
        MatchStatus.Scheduled => "Programado",
        MatchStatus.InProgress => "En juego",
        MatchStatus.Finished => "Finalizado",
        MatchStatus.Cancelled => "Cancelado",
        _ => status.ToString(),
    };

    public static string Of(BallType type) => type switch
    {
        BallType.Jack => "Jack",
        BallType.Colour => "Bola",
        BallType.Penalty => "Penalización",
        _ => type.ToString(),
    };

    public static string Of(ThrowResult result) => result switch
    {
        ThrowResult.InPlay => "En juego",
        ThrowResult.Dead => "Muerta",
        ThrowResult.Retracted => "Retirada",
        _ => result.ToString(),
    };

    public static string Of(PenaltyType type) => type switch
    {
        PenaltyType.PenaltyBall => "Bola de penalización",
        PenaltyType.Retraction => "Retiro de bola",
        PenaltyType.YellowCard => "Tarjeta amarilla",
        PenaltyType.RedCard => "Tarjeta roja",
        PenaltyType.Forfeit => "Pérdida del partido",
        _ => type.ToString(),
    };

    public static string Of(MatchFormat format) => format switch
    {
        MatchFormat.Individual => "Individual",
        MatchFormat.Pairs => "Parejas",
        MatchFormat.Team => "Equipos",
        _ => format.ToString(),
    };

    /// <summary>Clase CSS del lado: "red" o "blue".</summary>
    public static string Css(Side side) => side == Side.Red ? "red" : "blue";

    public static string EndName(int endNumber, bool isTieBreak) =>
        isTieBreak ? "Desempate" : $"Parcial {endNumber}";
}
