-- Deportistas que integran cada lado en un partido.
-- PlayerNumber es la posición dentro del lado (1..3 en parejas/equipos);
-- los suplentes no lanzan salvo que entren por sustitución.
CREATE TABLE [dbo].[MatchSideMembers] (
    [MatchSideId]  INT     NOT NULL,
    [AthleteId]    INT     NOT NULL,
    [PlayerNumber] TINYINT NOT NULL,
    [IsSubstitute] BIT     NOT NULL CONSTRAINT [DF_MatchSideMembers_IsSubstitute] DEFAULT (0),
    CONSTRAINT [PK_MatchSideMembers] PRIMARY KEY CLUSTERED ([MatchSideId] ASC, [AthleteId] ASC),
    CONSTRAINT [FK_MatchSideMembers_MatchSides] FOREIGN KEY ([MatchSideId]) REFERENCES [dbo].[MatchSides] ([MatchSideId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatchSideMembers_Athletes] FOREIGN KEY ([AthleteId]) REFERENCES [dbo].[Athletes] ([AthleteId]),
    CONSTRAINT [UQ_MatchSideMembers_PlayerNumber] UNIQUE ([MatchSideId], [PlayerNumber]),
    CONSTRAINT [CK_MatchSideMembers_PlayerNumber] CHECK ([PlayerNumber] BETWEEN 1 AND 6)
);
GO

CREATE NONCLUSTERED INDEX [IX_MatchSideMembers_Athlete]
    ON [dbo].[MatchSideMembers] ([AthleteId]);
