-- Los dos lados de un partido: rojo y azul.
-- DisplayName es lo que muestra el marcador (nombre del deportista,
-- de la pareja o del equipo).
CREATE TABLE [dbo].[MatchSides] (
    [MatchSideId] INT            IDENTITY (1, 1) NOT NULL,
    [MatchId]     INT            NOT NULL,
    [Side]        NVARCHAR (4)   NOT NULL,   -- 'Red' | 'Blue'
    [DisplayName] NVARCHAR (100) NOT NULL,
    CONSTRAINT [PK_MatchSides] PRIMARY KEY CLUSTERED ([MatchSideId] ASC),
    CONSTRAINT [FK_MatchSides_Matches] FOREIGN KEY ([MatchId]) REFERENCES [dbo].[Matches] ([MatchId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_MatchSides_Match_Side] UNIQUE ([MatchId], [Side]),
    CONSTRAINT [CK_MatchSides_Side] CHECK ([Side] IN ('Red', 'Blue'))
);
