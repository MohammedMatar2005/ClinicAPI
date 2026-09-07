using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class ClinicManagementSystemContext : DbContext
{
    public ClinicManagementSystemContext()
    {
    }

    public ClinicManagementSystemContext(DbContextOptions<ClinicManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<AppointmentStatus> AppointmentStatuses { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceStatus> InvoiceStatuses { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientVisit> PatientVisits { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentStatus> PaymentStatuses { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<VisitStatus> VisitStatuses { get; set; }

    public virtual DbSet<ClinicSettings> ClinicSettings { get; set; }

    public DbSet<SystemLog> SystemLogs { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("PK__Appointm__8ECDFCC2E4F0000A");

            entity.ToTable(tb => tb.HasTrigger("TR_Appointments_InsteadOfDelete"));

            entity.HasIndex(e => e.AppointmentDate, "IX_Appointments_AppointmentDate");

            entity.HasIndex(e => e.AppointmentDate, "IX_Appointments_Date");

            entity.HasIndex(e => e.DoctorId, "IX_Appointments_DoctorId");

            entity.HasIndex(e => e.PatientId, "IX_Appointments_PatientId");

            entity.HasIndex(e => new { e.DoctorId, e.AppointmentDate }, "UQ_Appointments_Doctor_Date").IsUnique();

            entity.Property(e => e.AppointmentDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusId).HasDefaultValue(1);
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Patients");

            entity.HasOne(d => d.Status).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Status");
        });

        modelBuilder.Entity<AppointmentStatus>(entity =>
        {
            entity.HasIndex(e => e.StatusName, "UQ_AppointmentStatuses_Title").IsUnique();

            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.DoctorId).HasName("PK__Doctors__2DC00EBFDA164D12");

            entity.HasIndex(e => e.Specialization, "IX_Doctors_Specialization");

            entity.HasIndex(e => e.UserId, "IX_Doctors_UserId");

            entity.HasIndex(e => e.LicenseNumber, "UQ_Doctors_LicenseNumber").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ__Doctors__1788CC4DD55F2260").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LicenseNumber).HasMaxLength(50);
            entity.Property(e => e.OfficeLocation).HasMaxLength(200);
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Specialization).HasMaxLength(100);

            entity.HasOne(d => d.User).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctors_Users");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__Invoices__D796AAB558BD8B5A");

            entity.ToTable(tb => tb.HasTrigger("TR_Invoices_InsteadOfDelete"));

            entity.HasIndex(e => e.StatusId, "IX_Invoices_StatusId");

            entity.HasIndex(e => e.VisitId, "IX_Invoices_VisitId");

            entity.HasIndex(e => e.InvoiceNumber, "UQ__Invoices__D776E981475CB61F").IsUnique();

            entity.Property(e => e.ConsultationFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountPercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.FinalAmount)
                .HasComputedColumnSql("((((([ConsultationFee]+[LabTestFee])+[ProcedureFee])+[OtherCharges])+isnull([TaxAmount],(0)))-isnull([DiscountAmount],(0)))", false)
                .HasColumnType("decimal(23, 2)");
            entity.Property(e => e.InvoiceDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.LabTestFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OtherCharges).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProcedureFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StatusId).HasDefaultValue((byte)1);
            entity.Property(e => e.SubTotal)
                .HasComputedColumnSql("((([ConsultationFee]+[LabTestFee])+[ProcedureFee])+[OtherCharges])", false)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.TaxAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxPercentage)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Status).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoices_InvoiceStatuses");

            entity.HasOne(d => d.Visit).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.VisitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invoices__VisitI__6754599E");
        });

        modelBuilder.Entity<InvoiceStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.Property(e => e.StatusId)
          .HasColumnType("tinyint") // توافقاً مع قاعدة البيانات
          .ValueGeneratedOnAdd();

            entity.HasIndex(e => e.StatusName, "UQ_InvoiceStatuses_StatusName").IsUnique();

            entity.Property(e => e.StatusId).ValueGeneratedOnAdd();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC3668E94E190");

            entity.ToTable(tb => tb.HasTrigger("TR_Patients_InsteadOfDelete"));

            entity.HasIndex(e => e.PersonId, "UQ__Patients__1788CC4D8FB723EF").IsUnique();

            entity.Property(e => e.BloodType).HasMaxLength(10);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmergencyContact).HasMaxLength(100);
            entity.Property(e => e.EmergencyPhone).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MedicalHistory).HasMaxLength(1000);

            entity.HasOne(d => d.Person).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patients_People");
        });

        modelBuilder.Entity<PatientVisit>(entity =>
        {
            entity.HasKey(e => e.VisitId).HasName("PK__PatientV__4D3AA1DECCC2AE73");

            entity.HasIndex(e => e.AppointmentId, "IX_PatientVisits_AppointmentId");

            entity.HasIndex(e => e.VisitDate, "IX_PatientVisits_VisitDate");

            entity.Property(e => e.BloodPressure).HasMaxLength(20);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Height).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Temperature).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.VisitDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VisitStatusId).HasDefaultValue((byte)1);
            entity.Property(e => e.Weight).HasColumnType("decimal(6, 2)");

            entity.HasOne(d => d.Appointment).WithMany(p => p.PatientVisits)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PatientVi__Appoi__5535A963");

            entity.HasOne(d => d.VisitStatus).WithMany(p => p.PatientVisits)
                .HasForeignKey(d => d.VisitStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientVisits_VisitStatuses");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A3857FF602B");

            entity.ToTable(tb => tb.HasTrigger("TR_Payments_InsteadOfDelete"));

            entity.HasIndex(e => e.InvoiceId, "IX_Payments_InvoiceId");

            entity.HasIndex(e => e.PaymentDate, "IX_Payments_PaymentDate");

            entity.HasIndex(e => e.PaymentStatusId, "IX_Payments_StatusId");

            entity.HasIndex(e => e.TransactionReference, "UQ_Payments_TransactionReference").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PaymentAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatusId).HasDefaultValue(2);
            entity.Property(e => e.TransactionReference).HasMaxLength(100);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Invoic__6E01572D");

            entity.HasOne(d => d.PaymentStatus).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PaymentStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Status");
        });

        modelBuilder.Entity<PaymentStatus>(entity =>
        {
            entity.HasKey(e => e.PaymentStatusId).HasName("PK__PaymentS__34F8AC3FB80C4DCC");

            entity.HasIndex(e => e.StatusName, "UQ__PaymentS__05E7698AE2D62FD0").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.PersonId).HasName("PK__People__AA2FFB85FF7BF05E");

            entity.HasIndex(e => e.Email, "UQ_People_Email").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.NationalNumber)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SecondName).HasMaxLength(50);
            entity.Property(e => e.ThirdName).HasMaxLength(50);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__401308325A963C0B");

            entity.HasIndex(e => e.MedicineName, "IX_Prescriptions_MedicineName");

            entity.HasIndex(e => e.StartDate, "IX_Prescriptions_StartDate");

            entity.HasIndex(e => e.VisitId, "IX_Prescriptions_VisitId");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Frequency).HasMaxLength(100);
            entity.Property(e => e.MedicineName).HasMaxLength(100);

            entity.HasOne(d => d.Visit).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.VisitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Prescript__Visit__5AEE82B9");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA69BD930");

            entity.HasIndex(e => e.LastLoginDate, "IX_Users_LastLoginDate");

            entity.HasIndex(e => e.RoleId, "IX_Users_RoleId");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E48B81D0A5").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.RefreshTokenRevokedAt).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Person).WithMany(p => p.Users)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_People");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3F466844");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__UserRole__8AFACE1A841D78B1");

            entity.HasIndex(e => e.RoleName, "UQ__UserRole__8A2B6160E000B3C1").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<VisitStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId);

            entity.Property(e => e.StatusId).ValueGeneratedOnAdd();
            entity.Property(e => e.StatusTitle).HasMaxLength(50);
        });

        modelBuilder.Entity<ClinicSettings>(entity =>
        {
            entity.ToTable("ClinicSettings", "dbo"); // تحديد اسم الجدول والمخطط صراحة

            entity.HasKey(e => e.ClinicId);

            entity.Property(e => e.ClinicId)
                  .ValueGeneratedNever(); // 💡 لأننا نضع القيمة 1 يدوياً وبسبب الـ Check Constraint

            entity.Property(e => e.ClinicName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ClinicNameEn).HasMaxLength(150);
            entity.Property(e => e.TaxNumber).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber1).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PhoneNumber2).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Website).HasMaxLength(150);
            entity.Property(e => e.WeekendDays).HasMaxLength(50);
            entity.Property(e => e.DefaultCurrency).HasMaxLength(10).IsRequired();

            // ضبط نوع الـ Decimal بدقة لمنع مشاكل التقريب
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5, 2)");

            entity.Property(e => e.DefaultInvoiceNotes).HasMaxLength(500);
        });

            
        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogID);

            entity.Property(e => e.EventName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.SeverityLevel)
                .IsRequired()
                .HasColumnType("tinyint"); 

            entity.Property(e => e.LogTimestamp)
                .HasDefaultValueSql("GETDATE()")
                .IsRequired();

            
            entity.HasOne(d => d.CreatorUser)
                .WithMany() 
                .HasForeignKey(d => d.CreatedByUserID)
                .OnDelete(DeleteBehavior.SetNull); 
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
