-- Partido. El marcador total no se guarda aquí: se obtiene sumando los
-- parciales (ver vista vw_MatchScores).
CREATE TABLE [dbo].[Matches] (
    [MatchId]     INT            IDENTITY (1, 1) NOT NULL,
    [DivisionId]  INT            NOT NULL,
    [Court]       NVARCHAR (20)  NULL,       -- Cancha
    [Referee]     NVARCHAR (100) NULL,       -- Árbitro
    [Status]      NVARCHAR (20)  NOT NULL CONSTRAINT [DF_Matches_Status] DEFAULT ('Scheduled'),
    [ScheduledAt] DATETIME2 (0)  NULL,
    [StartedAt]   DATETIME2 (0)  NULL,
    [FinishedAt]  DATETIME2 (0)  NULL,
    [WinnerSide]  NVARCHAR (4)   NULL,       -- Se completa al finalizar: 'Red' | 'Blue'
    CONSTRAINT [PK_Matches] PRIMARY KEY CLUSTERED ([MatchId] ASC),
    CONSTRAINT [FK_Matches_Divisions] FOREIGN KEY ([DivisionId]) REFERENCES [dbo].[Divisions] ([DivisionId]),
    CONSTRAINT [CK_Matches_Status] CHECK ([Status] IN ('Scheduled', 'InProgress', 'Finished', 'Cancelled')),
    CONSTRAINT [CK_Matches_WinnerSide] CHECK ([WinnerSide] IS NULL OR [WinnerSide] IN ('Red', 'Blue')),
    CONSTRAINT [CK_Matches_Winner_Finished] CHECK ([WinnerSide] IS NULL OR [Status] = 'Finished'),
    CONSTRAINT [CK_Matches_Dates] CHECK ([FinishedAt] IS NULL OR [StartedAt] IS NULL OR [FinishedAt] >= [StartedAt])
);
GO

CREATE NONCLUSTERED INDEX [IX_Matches_Status]
    ON [dbo].[Matches] ([Status]);
