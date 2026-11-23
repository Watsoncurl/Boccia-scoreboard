-- Lanzamientos dentro de un parcial, en orden (Sequence).
-- Incluye el lanzamiento del jack (BallType = 'Jack', sin distancia).
-- DistanceToJackMm es la distancia de la bola al jack medida al cierre
-- del parcial; con ella el servidor calcula los puntos del parcial.
CREATE TABLE [dbo].[Throws] (
    [ThrowId]          INT           IDENTITY (1, 1) NOT NULL,
    [EndId]            INT           NOT NULL,
    [Sequence]         TINYINT       NOT NULL,
    [Side]             NVARCHAR (4)  NOT NULL,   -- 'Red' | 'Blue'
    [AthleteId]        INT           NULL,       -- Quién lanzó (opcional en individual)
    [BallType]         NVARCHAR (10) NOT NULL CONSTRAINT [DF_Throws_BallType] DEFAULT ('Colour'),
    [Result]           NVARCHAR (20) NOT NULL CONSTRAINT [DF_Throws_Result] DEFAULT ('InPlay'),
    [DistanceToJackMm] INT           NULL,
    [ThrownAt]         DATETIME2 (0) NOT NULL CONSTRAINT [DF_Throws_ThrownAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Throws] PRIMARY KEY CLUSTERED ([ThrowId] ASC),
    CONSTRAINT [FK_Throws_Ends] FOREIGN KEY ([EndId]) REFERENCES [dbo].[Ends] ([EndId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Throws_Athletes] FOREIGN KEY ([AthleteId]) REFERENCES [dbo].[Athletes] ([AthleteId]),
    CONSTRAINT [UQ_Throws_End_Sequence] UNIQUE ([EndId], [Sequence]),
    CONSTRAINT [CK_Throws_Side] CHECK ([Side] IN ('Red', 'Blue')),
    CONSTRAINT [CK_Throws_BallType] CHECK ([BallType] IN ('Jack', 'Colour', 'Penalty')),
    -- InPlay: dentro de la cancha | Dead: bola muerta (fuera o retirada) | Retracted: retirada por infracción
    CONSTRAINT [CK_Throws_Result] CHECK ([Result] IN ('InPlay', 'Dead', 'Retracted')),
    CONSTRAINT [CK_Throws_Distance] CHECK ([DistanceToJackMm] IS NULL OR [DistanceToJackMm] >= 0),
    CONSTRAINT [CK_Throws_Jack_NoDistance] CHECK ([BallType] <> 'Jack' OR [DistanceToJackMm] IS NULL)
);
