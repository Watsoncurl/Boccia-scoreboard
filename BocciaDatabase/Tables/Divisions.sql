-- Modalidades/divisiones de competencia (BC1..BC4 individual, parejas, equipos).
-- Define la configuración de un partido: cuántos jugadores por lado,
-- cuántas bolas lanza cada jugador y cuántos parciales se juegan.
CREATE TABLE [dbo].[Divisions] (
    [DivisionId]        INT            IDENTITY (1, 1) NOT NULL,
    [Code]              NVARCHAR (20)  NOT NULL,  -- Ej.: 'BC1', 'PAIRS_BC3', 'TEAM_BC1BC2'
    [Name]              NVARCHAR (100) NOT NULL,
    [MatchType]         NVARCHAR (20)  NOT NULL,  -- 'Individual' | 'Pairs' | 'Team'
    [PlayersPerSide]    TINYINT        NOT NULL,
    [BallsPerPlayer]    TINYINT        NOT NULL,
    [NumberOfEnds]      TINYINT        NOT NULL,
    [TimePerEndSeconds] INT            NULL,      -- Tiempo por lado y por parcial (NULL = sin control de tiempo)
    CONSTRAINT [PK_Divisions] PRIMARY KEY CLUSTERED ([DivisionId] ASC),
    CONSTRAINT [UQ_Divisions_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_Divisions_MatchType] CHECK ([MatchType] IN ('Individual', 'Pairs', 'Team')),
    CONSTRAINT [CK_Divisions_PlayersPerSide] CHECK ([PlayersPerSide] BETWEEN 1 AND 3),
    CONSTRAINT [CK_Divisions_BallsPerPlayer] CHECK ([BallsPerPlayer] BETWEEN 1 AND 6),
    CONSTRAINT [CK_Divisions_NumberOfEnds] CHECK ([NumberOfEnds] >= 1),
    CONSTRAINT [CK_Divisions_TimePerEnd] CHECK ([TimePerEndSeconds] IS NULL OR [TimePerEndSeconds] > 0)
);
