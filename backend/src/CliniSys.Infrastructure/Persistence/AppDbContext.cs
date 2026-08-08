using CliniSys.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CliniSys.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict();

        builder.Entity<Doctor>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasOne(d => d.User).WithOne()
             .HasForeignKey<Doctor>(d => d.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Patient>(e => e.HasKey(p => p.Id));

        builder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Patient).WithMany()
             .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Doctor).WithMany()
             .HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);

            // Npgsql rejects DateTime.Kind != Utc for "timestamp with time zone" columns; incoming
            // StartsAt values arrive with Kind=Unspecified (no offset in the request JSON), so stamp
            // Kind=Utc without shifting the wall-clock value before it reaches SaveChangesAsync.
            var utcKind = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            e.Property(a => a.StartsAt).HasConversion(utcKind);
            e.Property(a => a.CreatedAt).HasConversion(utcKind);
        });

        builder.Entity<ClinicSettings>(e => e.HasKey(s => s.Id));
    }
}
