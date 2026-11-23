namespace BocciaShared.Domain;

/// <summary>Lado de la cancha. Rojo lanza el jack en el primer parcial.</summary>
public enum Side { Red, Blue }

public enum MatchFormat { Individual, Pairs, Team }

public enum MatchStatus { Scheduled, InProgress, Finished, Cancelled }

public enum EndStatus { InProgress, Finished, Annulled }

/// <summary>Jack (bola blanca), bola de color o bola de penalización.</summary>
public enum BallType { Jack, Colour, Penalty }

/// <summary>
/// InPlay: la bola está dentro de la cancha y cuenta para el puntaje.
/// Dead: bola muerta (fuera de la cancha). Retracted: retirada por infracción.
/// </summary>
public enum ThrowResult { InPlay, Dead, Retracted }

public enum PenaltyType { PenaltyBall, Retraction, YellowCard, RedCard, Forfeit }

public static class SideExtensions
{
    public static Side Opponent(this Side side) => side == Side.Red ? Side.Blue : Side.Red;

    public static string ToSpanish(this Side side) => side == Side.Red ? "rojo" : "azul";
}
