using BocciaShared.Domain;

namespace BocciaServer.Data;

// Entidades que reflejan las tablas de BocciaDatabase.
// El esquema lo define el proyecto SQL (dacpac); EF Core solo lo mapea.

public class Division
{
    public int DivisionId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public MatchFormat MatchType { get; set; }
    public byte PlayersPerSide { get; set; }
    public byte BallsPerPlayer { get; set; }
    public byte NumberOfEnds { get; set; }
    public int? TimePerEndSeconds { get; set; }

    public int BallsPerSide => PlayersPerSide * BallsPerPlayer;
}

public class Athlete
{
    public int AthleteId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Classification { get; set; }
    public string? Club { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}

public class Match
{
    public int MatchId { get; set; }
    public int DivisionId { get; set; }
    public Division Division { get; set; } = null!;
    public string? Court { get; set; }
    public string? Referee { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public Side? WinnerSide { get; set; }

    public List<MatchSide> Sides { get; set; } = [];
    public List<End> Ends { get; set; } = [];
}

public class MatchSide
{
    public int MatchSideId { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public Side Side { get; set; }
    public string DisplayName { get; set; } = "";

    public List<MatchSideMember> Members { get; set; } = [];
}

public class MatchSideMember
{
    public int MatchSideId { get; set; }
    public MatchSide MatchSide { get; set; } = null!;
    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;
    public byte PlayerNumber { get; set; }
    public bool IsSubstitute { get; set; }
}

public class End
{
    public int EndId { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public byte EndNumber { get; set; }
    public bool IsTieBreak { get; set; }
    public Side JackSide { get; set; }
    public EndStatus Status { get; set; } = EndStatus.InProgress;
    public byte RedPoints { get; set; }
    public byte BluePoints { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public List<Throw> Throws { get; set; } = [];
    public List<Penalty> Penalties { get; set; } = [];
}

public class Throw
{
    public int ThrowId { get; set; }
    public int EndId { get; set; }
    public End End { get; set; } = null!;
    public byte Sequence { get; set; }
    public Side Side { get; set; }
    public int? AthleteId { get; set; }
    public Athlete? Athlete { get; set; }
    public BallType BallType { get; set; } = BallType.Colour;
    public ThrowResult Result { get; set; } = ThrowResult.InPlay;
    public int? DistanceToJackMm { get; set; }
    public DateTime ThrownAt { get; set; }
}

public class Penalty
{
    public int PenaltyId { get; set; }
    public int EndId { get; set; }
    public End End { get; set; } = null!;
    public Side PenalizedSide { get; set; }
    public int? AthleteId { get; set; }
    public Athlete? Athlete { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
