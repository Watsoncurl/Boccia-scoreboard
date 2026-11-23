-- Parciales (ends) de un partido. Los puntos se asignan al cerrar el parcial:
-- solo un lado puntúa (1 punto por cada bola más cercana al jack que la
-- mejor bola rival), salvo empate de distancias entre bolas de distinto color.
-- JackSide indica qué lado lanza el jack (rojo en el parcial 1, luego alterna).
CREATE TABLE [dbo].[Ends] (
    [EndId]       INT           IDENTITY (1, 1) NOT NULL,
    [MatchId]     INT           NOT NULL,
    [EndNumber]   TINYINT       NOT NULL,   -- 1..N; los desempates continúan la numeración
    [IsTieBreak]  BIT           NOT NULL CONSTRAINT [DF_Ends_IsTieBreak] DEFAULT (0),
    [JackSide]    NVARCHAR (4)  NOT NULL,
    [Status]      NVARCHAR (20) NOT NULL CONSTRAINT [DF_Ends_Status] DEFAULT ('InProgress'),
    [RedPoints]   TINYINT       NOT NULL CONSTRAINT [DF_Ends_RedPoints] DEFAULT (0),
    [BluePoints]  TINYINT       NOT NULL CONSTRAINT [DF_Ends_BluePoints] DEFAULT (0),
    [StartedAt]   DATETIME2 (0) NULL,
    [FinishedAt]  DATETIME2 (0) NULL,
    CONSTRAINT [PK_Ends] PRIMARY KEY CLUSTERED ([EndId] ASC),
    CONSTRAINT [FK_Ends_Matches] FOREIGN KEY ([MatchId]) REFERENCES [dbo].[Matches] ([MatchId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Ends_Match_EndNumber] UNIQUE ([MatchId], [EndNumber]),
    CONSTRAINT [CK_Ends_EndNumber] CHECK ([EndNumber] >= 1),
    CONSTRAINT [CK_Ends_JackSide] CHECK ([JackSide] IN ('Red', 'Blue')),
    CONSTRAINT [CK_Ends_Status] CHECK ([Status] IN ('InProgress', 'Finished', 'Annulled')),
    CONSTRAINT [CK_Ends_Points] CHECK ([RedPoints] <= 20 AND [BluePoints] <= 20)  -- 6 bolas + posibles bolas de penalización
);
