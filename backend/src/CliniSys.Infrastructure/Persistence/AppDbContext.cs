using CliniSys.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        });

        builder.Entity<ClinicSettings>(e => e.HasKey(s => s.Id));
    }
}
