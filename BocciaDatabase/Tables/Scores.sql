﻿CREATE TABLE Scores (
    ScoreID INT IDENTITY(1, 1) PRIMARY KEY,
    MatchID INT NOT NULL,
    PlayerID INT NOT NULL,
    Points INT NOT NULL,
    FOREIGN KEY (MatchID) REFERENCES Matches(MatchID),
    FOREIGN KEY (PlayerID) REFERENCES Players(PlayerID)
);