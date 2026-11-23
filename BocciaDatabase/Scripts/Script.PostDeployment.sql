/*
 Datos iniciales: divisiones oficiales de boccia.
 Se ejecuta en cada despliegue y es idempotente (MERGE por Code).
 Todas las modalidades lanzan 6 bolas por lado:
   - Individual: 1 jugador x 6 bolas, 4 parciales
   - Parejas:    2 jugadores x 3 bolas, 4 parciales
   - Equipos:    3 jugadores x 2 bolas, 6 parciales
 TimePerEndSeconds queda en NULL hasta confirmar los tiempos del reglamento vigente.
*/
MERGE INTO [dbo].[Divisions] AS target
USING (VALUES
    (N'BC1',         N'Individual BC1',     N'Individual', 1, 6, 4),
    (N'BC2',         N'Individual BC2',     N'Individual', 1, 6, 4),
    (N'BC3',         N'Individual BC3',     N'Individual', 1, 6, 4),
    (N'BC4',         N'Individual BC4',     N'Individual', 1, 6, 4),
    (N'PAIRS_BC3',   N'Parejas BC3',        N'Pairs',      2, 3, 4),
    (N'PAIRS_BC4',   N'Parejas BC4',        N'Pairs',      2, 3, 4),
    (N'TEAM_BC1BC2', N'Equipos BC1/BC2',    N'Team',       3, 2, 6)
) AS source ([Code], [Name], [MatchType], [PlayersPerSide], [BallsPerPlayer], [NumberOfEnds])
ON target.[Code] = source.[Code]
WHEN MATCHED THEN
    UPDATE SET [Name] = source.[Name],
               [MatchType] = source.[MatchType],
               [PlayersPerSide] = source.[PlayersPerSide],
               [BallsPerPlayer] = source.[BallsPerPlayer],
               [NumberOfEnds] = source.[NumberOfEnds]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([Code], [Name], [MatchType], [PlayersPerSide], [BallsPerPlayer], [NumberOfEnds])
    VALUES (source.[Code], source.[Name], source.[MatchType], source.[PlayersPerSide], source.[BallsPerPlayer], source.[NumberOfEnds]);
