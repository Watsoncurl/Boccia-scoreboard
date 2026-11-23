using BocciaShared.Domain;

namespace BocciaServer.Services;

/// <summary>
/// Cálculo del puntaje de un parcial según el reglamento de boccia:
/// <list type="bullet">
/// <item>Puntúa solo el lado con la bola más cercana al jack: 1 punto por cada
/// una de sus bolas que esté más cerca que la mejor bola rival.</item>
/// <item>Si un lado no tiene bolas en juego, el otro suma 1 punto por cada bola en juego.</item>
/// <item>Si las bolas más cercanas de ambos colores están a la misma distancia,
/// cada lado suma 1 punto por cada bola a esa distancia.</item>
/// </list>
/// Solo se consideran bolas válidas (en juego) con su distancia medida.
/// </summary>
public static class EndScoring
{
    public readonly record struct ScoredBall(Side Side, int DistanceToJackMm);

    public static (int Red, int Blue) Calculate(IEnumerable<ScoredBall> balls)
    {
        var red = balls.Where(b => b.Side == Side.Red).Select(b => b.DistanceToJackMm).ToList();
        var blue = balls.Where(b => b.Side == Side.Blue).Select(b => b.DistanceToJackMm).ToList();

        if (red.Count == 0 && blue.Count == 0) return (0, 0);
        if (blue.Count == 0) return (red.Count, 0);
        if (red.Count == 0) return (0, blue.Count);

        var closestRed = red.Min();
        var closestBlue = blue.Min();

        if (closestRed < closestBlue) return (red.Count(d => d < closestBlue), 0);
        if (closestBlue < closestRed) return (0, blue.Count(d => d < closestRed));

        // Empate: las bolas más cercanas de ambos colores están a la misma distancia.
        return (red.Count(d => d == closestRed), blue.Count(d => d == closestBlue));
    }
}
