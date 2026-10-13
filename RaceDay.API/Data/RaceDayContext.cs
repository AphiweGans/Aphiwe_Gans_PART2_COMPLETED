using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organiser>().HasIndex(o => o.Email).IsUnique();
        modelBuilder.Entity<Participant>().HasIndex(p => p.Email).IsUnique();

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganiserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Participant)
            .WithMany(p => p.Enrolments)
            .HasForeignKey(en => en.ParticipantID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(en => en.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(en => en.CategoryID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasIndex(en => new { en.ParticipantID, en.CategoryID })
            .IsUnique();

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(en => en.Result)
            .HasForeignKey<Result>(r => r.EnrolmentID)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();

        modelBuilder.Entity<Event>().Property(e => e.Distance).HasPrecision(6, 2);
        modelBuilder.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(5, 2);
        modelBuilder.Entity<Category>().Property(c => c.EntryFee).HasPrecision(8, 2);

        // Column sizes and types taken from the Part 1 SQL script.
        modelBuilder.Entity<Organiser>(o =>
        {
            o.Property(x => x.FullName).HasMaxLength(100);
            o.Property(x => x.Email).HasMaxLength(150);
            o.Property(x => x.PasswordHash).HasMaxLength(255);
            o.Property(x => x.Phone).HasMaxLength(20);
        });
        modelBuilder.Entity<Participant>(p =>
        {
            p.Property(x => x.FullName).HasMaxLength(100);
            p.Property(x => x.Email).HasMaxLength(150);
            p.Property(x => x.PasswordHash).HasMaxLength(255);
            p.Property(x => x.Gender).HasMaxLength(20);
            p.Property(x => x.DateOfBirth).HasColumnType("date");
        });
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.EventName).HasMaxLength(150);
            e.Property(x => x.Location).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.EventDate).HasColumnType("date");
        });
        modelBuilder.Entity<Category>().Property(c => c.CategoryName).HasMaxLength(50);
        modelBuilder.Entity<Enrolment>().Property(en => en.Status).HasMaxLength(20);
        modelBuilder.Entity<Result>().Property(r => r.Status).HasMaxLength(20);
    }
}
