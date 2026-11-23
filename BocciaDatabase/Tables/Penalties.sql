-- Sanciones aplicadas durante un parcial (bola de penalización,
-- retirada de bola, tarjetas). Quedan registradas para el acta del partido.
CREATE TABLE [dbo].[Penalties] (
    [PenaltyId]     INT            IDENTITY (1, 1) NOT NULL,
    [EndId]         INT            NOT NULL,
    [PenalizedSide] NVARCHAR (4)   NOT NULL,   -- Lado que comete la infracción
    [AthleteId]     INT            NULL,
    [PenaltyType]   NVARCHAR (20)  NOT NULL,
    [Reason]        NVARCHAR (200) NULL,
    [CreatedAt]     DATETIME2 (0)  NOT NULL CONSTRAINT [DF_Penalties_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Penalties] PRIMARY KEY CLUSTERED ([PenaltyId] ASC),
    CONSTRAINT [FK_Penalties_Ends] FOREIGN KEY ([EndId]) REFERENCES [dbo].[Ends] ([EndId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Penalties_Athletes] FOREIGN KEY ([AthleteId]) REFERENCES [dbo].[Athletes] ([AthleteId]),
    CONSTRAINT [CK_Penalties_Side] CHECK ([PenalizedSide] IN ('Red', 'Blue')),
    CONSTRAINT [CK_Penalties_Type] CHECK ([PenaltyType] IN ('PenaltyBall', 'Retraction', 'YellowCard', 'RedCard', 'Forfeit'))
);
