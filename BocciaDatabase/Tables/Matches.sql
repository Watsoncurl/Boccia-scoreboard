CREATE TABLE Matches (
    MatchID INT IDENTITY(1, 1) PRIMARY KEY,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NULL,
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('InProgress', 'Finished')),
    CurrentTurn INT NOT NULL, -- Referencia al PlayerID que tiene el turno actual
    BallsThrownTeam1 INT NOT NULL DEFAULT 0, -- Cantidad de bolas lanzadas por el equipo 1
    BallsThrownTeam2 INT NOT NULL DEFAULT 0, -- Cantidad de bolas lanzadas por el equipo 2
    FOREIGN KEY (CurrentTurn) REFERENCES Players(PlayerID)
);