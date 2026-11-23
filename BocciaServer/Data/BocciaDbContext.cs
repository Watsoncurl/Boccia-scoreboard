using Microsoft.EntityFrameworkCore;

namespace BocciaServer.Data;

public class BocciaDbContext(DbContextOptions<BocciaDbContext> options) : DbContext(options)
{
    public DbSet<Division> Divisions => Set<Division>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchSide> MatchSides => Set<MatchSide>();
    public DbSet<MatchSideMember> MatchSideMembers => Set<MatchSideMember>();
    public DbSet<End> Ends => Set<End>();
    public DbSet<Throw> Throws => Set<Throw>();
    public DbSet<Penalty> Penalties => Set<Penalty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Los enums se guardan como texto, igual que los CHECK del esquema SQL.
        modelBuilder.Entity<Division>(e =>
        {
            e.ToTable("Divisions");
            e.Property(x => x.Code).HasMaxLength(20);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.MatchType).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Athlete>(e =>
        {
            e.ToTable("Athletes");
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.Classification).HasMaxLength(3);
            e.Property(x => x.Club).HasMaxLength(100);
        });

        modelBuilder.Entity<Match>(e =>
        {
            e.ToTable("Matches");
            e.Property(x => x.Court).HasMaxLength(20);
            e.Property(x => x.Referee).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.WinnerSide).HasConversion<string>().HasMaxLength(4);
            e.HasOne(x => x.Division).WithMany().HasForeignKey(x => x.DivisionId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Sides).WithOne(x => x.Match).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Ends).WithOne(x => x.Match).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<MatchSide>(e =>
        {
            e.ToTable("MatchSides");
            e.Property(x => x.Side).HasConversion<string>().HasMaxLength(4);
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.HasIndex(x => new { x.MatchId, x.Side }).IsUnique();
            e.HasMany(x => x.Members).WithOne(x => x.MatchSide).HasForeignKey(x => x.MatchSideId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchSideMember>(e =>
        {
            e.ToTable("MatchSideMembers");
            e.HasKey(x => new { x.MatchSideId, x.AthleteId });
            e.HasIndex(x => new { x.MatchSideId, x.PlayerNumber }).IsUnique();
            e.HasOne(x => x.Athlete).WithMany().HasForeignKey(x => x.AthleteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<End>(e =>
        {
            e.ToTable("Ends");
            e.Property(x => x.JackSide).HasConversion<string>().HasMaxLength(4);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.MatchId, x.EndNumber }).IsUnique();
            e.HasMany(x => x.Throws).WithOne(x => x.End).HasForeignKey(x => x.EndId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Penalties).WithOne(x => x.End).HasForeignKey(x => x.EndId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Throw>(e =>
        {
            e.ToTable("Throws");
            e.Property(x => x.Side).HasConversion<string>().HasMaxLength(4);
            e.Property(x => x.BallType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.EndId, x.Sequence }).IsUnique();
            e.HasOne(x => x.Athlete).WithMany().HasForeignKey(x => x.AthleteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Penalty>(e =>
        {
            e.ToTable("Penalties");
            e.Property(x => x.PenalizedSide).HasConversion<string>().HasMaxLength(4);
            e.Property(x => x.PenaltyType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(200);
            e.HasOne(x => x.Athlete).WithMany().HasForeignKey(x => x.AthleteId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
