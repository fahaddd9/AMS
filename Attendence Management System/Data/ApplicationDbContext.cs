using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Batch>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();

            b.HasOne(x => x.Batch)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Course>(b =>
        {
            b.Property(x => x.UniqueCode).HasMaxLength(30).IsRequired();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.HasIndex(x => x.UniqueCode).IsUnique();

            b.HasOne(x => x.Batch)
                .WithMany(x => x.Courses)
                .HasForeignKey(x => x.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TimetableSlot>(b =>
        {
            b.Property(x => x.TimeRange).HasMaxLength(30);
            b.HasIndex(x => new { x.CourseId, x.DayOfWeek, x.TimeRange }).IsUnique();
        });

        builder.Entity<Enrollment>(b =>
        {
            b.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();

            b.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TeacherAssignment>(b =>
        {
            b.HasIndex(x => new { x.TeacherId, x.CourseId }).IsUnique();

            b.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attendance>(b =>
        {
            // each teacher must have its own record
            b.HasIndex(x => new { x.StudentId, x.CourseId, x.Date, x.MarkedByTeacherId }).IsUnique();

            b.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.MarkedByTeacher)
                .WithMany()
                .HasForeignKey(x => x.MarkedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.Status).HasConversion<int>();
            b.Property(x => x.Date).HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v));
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
