using Microsoft.EntityFrameworkCore;
using StudyFlow.Api.Models;

namespace StudyFlow.Api.Data;

public sealed class StudyFlowDbContext(DbContextOptions<StudyFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<StudyTask> Tasks => Set<StudyTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<StudyTask>();

        task.ToTable("Tasks");
        task.HasKey(item => item.Id);
        task.Property(item => item.Title).HasMaxLength(120).IsRequired();
        task.Property(item => item.Description).HasMaxLength(500);
        task.Property(item => item.Priority).HasConversion<string>().HasMaxLength(20);
        task.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        task.HasIndex(item => item.Status);
        task.HasIndex(item => item.DueDate);
    }
}
