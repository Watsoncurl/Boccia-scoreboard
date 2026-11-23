-- Deportistas. No tienen equipo ni color fijo: el lado (rojo/azul)
-- se asigna en cada partido mediante MatchSides/MatchSideMembers.
CREATE TABLE [dbo].[Athletes] (
    [AthleteId]      INT            IDENTITY (1, 1) NOT NULL,
    [FirstName]      NVARCHAR (100) NOT NULL,
    [LastName]       NVARCHAR (100) NOT NULL,
    [Classification] NVARCHAR (3)   NULL,     -- 'BC1' | 'BC2' | 'BC3' | 'BC4'
    [Club]           NVARCHAR (100) NULL,     -- Club, delegación o país
    [IsActive]       BIT            NOT NULL CONSTRAINT [DF_Athletes_IsActive] DEFAULT (1),
    CONSTRAINT [PK_Athletes] PRIMARY KEY CLUSTERED ([AthleteId] ASC),
    CONSTRAINT [CK_Athletes_Classification] CHECK ([Classification] IS NULL OR [Classification] IN ('BC1', 'BC2', 'BC3', 'BC4'))
);
