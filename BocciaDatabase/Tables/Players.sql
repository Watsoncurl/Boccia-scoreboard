CREATE TABLE Players (
    PlayerID INT IDENTITY(1, 1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Team NVARCHAR(50) NOT NULL CHECK (Team IN ('Team1', 'Team2')) -- Equipo al que pertenece
);