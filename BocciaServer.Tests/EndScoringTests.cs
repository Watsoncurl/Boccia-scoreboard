using BocciaShared.Domain;
using static BocciaShared.Domain.EndScoring;

namespace BocciaServer.Tests;

public class EndScoringTests
{
    private static ScoredBall R(int mm) => new(Side.Red, mm);
    private static ScoredBall B(int mm) => new(Side.Blue, mm);

    [Fact]
    public void Sin_bolas_en_juego_nadie_puntua() =>
        Assert.Equal((0, 0), Calculate([]));

    [Fact]
    public void Puntua_cada_bola_mas_cercana_que_la_mejor_rival() =>
        Assert.Equal((2, 0), Calculate([R(50), R(80), R(300), B(100), B(120)]));

    [Fact]
    public void Una_sola_bola_mas_cercana_suma_un_punto() =>
        Assert.Equal((0, 1), Calculate([R(200), R(90), B(40), B(150)]));

    [Fact]
    public void Si_el_rival_no_tiene_bolas_en_juego_puntuan_todas() =>
        Assert.Equal((0, 3), Calculate([B(500), B(10), B(900)]));

    [Fact]
    public void Empate_en_la_bola_mas_cercana_puntuan_ambos_lados() =>
        Assert.Equal((1, 1), Calculate([R(70), R(200), B(70), B(90)]));

    [Fact]
    public void Empate_con_varias_bolas_equidistantes_suma_una_por_bola() =>
        Assert.Equal((2, 1), Calculate([R(70), R(70), B(70), B(300)]));
}
