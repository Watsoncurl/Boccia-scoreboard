-- Marcador total por partido: suma de los parciales finalizados.
-- Es lo que consulta el servidor para mostrar en el marcador.
CREATE VIEW [dbo].[vw_MatchScores]
AS
SELECT
    m.[MatchId],
    m.[Status],
    d.[Code]                                   AS [DivisionCode],
    red.[DisplayName]                          AS [RedName],
    blue.[DisplayName]                         AS [BlueName],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' THEN e.[RedPoints]  END), 0) AS [RedScore],
    ISNULL(SUM(CASE WHEN e.[Status] = 'Finished' THEN e.[BluePoints] END), 0) AS [BlueScore],
    COUNT(CASE WHEN e.[Status] = 'Finished' THEN 1 END)                       AS [EndsPlayed],
    d.[NumberOfEnds],
    m.[WinnerSide]
FROM [dbo].[Matches] AS m
INNER JOIN [dbo].[Divisions] AS d ON d.[DivisionId] = m.[DivisionId]
LEFT JOIN [dbo].[MatchSides] AS red  ON red.[MatchId]  = m.[MatchId] AND red.[Side]  = 'Red'
LEFT JOIN [dbo].[MatchSides] AS blue ON blue.[MatchId] = m.[MatchId] AND blue.[Side] = 'Blue'
LEFT JOIN [dbo].[Ends] AS e ON e.[MatchId] = m.[MatchId]
GROUP BY m.[MatchId], m.[Status], d.[Code], red.[DisplayName], blue.[DisplayName], d.[NumberOfEnds], m.[WinnerSide];
