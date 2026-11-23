-- Marcador total por partido: suma de los parciales regulares finalizados.
-- Los parciales de desempate solo definen al ganador y se informan aparte.
-- Es lo que consulta el servidor para mostrar en el marcador.
CREATE VIEW [dbo].[vw_MatchScores]
AS
SELECT
    m.[MatchId],
    m.[Status],
    d.[Code]                                   AS [DivisionCode],
    red.[DisplayName]                          AS [RedName],
    blue.[DisplayName]                         AS [BlueName],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' AND e.[IsTieBreak] = 0 THEN e.[RedPoints]  END), 0) AS [RedScore],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' AND e.[IsTieBreak] = 0 THEN e.[BluePoints] END), 0) AS [BlueScore],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' AND e.[IsTieBreak] = 1 THEN e.[RedPoints]  END), 0) AS [RedTieBreakScore],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' AND e.[IsTieBreak] = 1 THEN e.[BluePoints] END), 0) AS [BlueTieBreakScore],
    COUNT(CASE WHEN e.[Status] = 'Finished' AND e.[IsTieBreak] = 0 THEN 1 END)                       AS [EndsPlayed],
    d.[NumberOfEnds],
    m.[WinnerSide]
FROM [dbo].[Matches] AS m
INNER JOIN [dbo].[Divisions] AS d ON d.[DivisionId] = m.[DivisionId]
LEFT JOIN [dbo].[MatchSides] AS red  ON red.[MatchId]  = m.[MatchId] AND red.[Side]  = 'Red'
LEFT JOIN [dbo].[MatchSides] AS blue ON blue.[MatchId] = m.[MatchId] AND blue.[Side] = 'Blue'
LEFT JOIN [dbo].[Ends] AS e ON e.[MatchId] = m.[MatchId]
GROUP BY m.[MatchId], m.[Status], d.[Code], red.[DisplayName], blue.[DisplayName], d.[NumberOfEnds], m.[WinnerSide];
