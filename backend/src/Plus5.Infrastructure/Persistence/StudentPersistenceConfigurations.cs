using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Identity;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Infrastructure.Persistence;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students", table =>
        {
            table.HasCheckConstraint(
                "CK_Students_Status",
                "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_Students_DeliveryMode",
                "[DeliveryMode] IS NULL OR [DeliveryMode] IN (1, 2)");
            table.HasCheckConstraint(
                "CK_Students_Organization",
                "([ProgramId] IS NULL AND [DeliveryMode] IS NULL) " +
                "OR ([ProgramId] IS NOT NULL AND [DeliveryMode] IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_Students_ArchivedStatus",
                "[ArchivedAtUtc] IS NULL OR [Status] = 3");
        });

        builder.HasKey(student => student.Id);
        builder.Property(student => student.FirstName)
            .HasMaxLength(Student.FirstNameMaxLength)
            .IsRequired();
        builder.Property(student => student.LastName)
            .HasMaxLength(Student.LastNameMaxLength)
            .IsRequired();
        builder.Property(student => student.Nickname)
            .HasMaxLength(Student.NicknameMaxLength);
        builder.Property(student => student.DateOfBirth).HasColumnType("date");
        builder.Property(student => student.SchoolName)
            .HasMaxLength(Student.SchoolNameMaxLength);
        builder.Property(student => student.Gender)
            .HasMaxLength(Student.GenderMaxLength);
        builder.Property(student => student.Email)
            .HasMaxLength(Student.EmailMaxLength);
        builder.Property(student => student.Phone)
            .HasMaxLength(Student.PhoneMaxLength);
        builder.Property(student => student.DeliveryMode)
            .HasConversion<int?>();
        builder.Property(student => student.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(student => student.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(student => student.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(student => student.ArchivedAtUtc).HasPrecision(7);
        builder.Property(student => student.RowVersion).IsRowVersion();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(student => student.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolGrade>()
            .WithMany()
            .HasForeignKey(student => student.SchoolGradeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeachingProgram>()
            .WithMany()
            .HasForeignKey(student => new
            {
                student.TeacherAccountId,
                student.ProgramId,
            })
            .HasPrincipalKey(program => new
            {
                program.TeacherAccountId,
                program.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(student => new
        {
            student.TeacherAccountId,
            student.Id,
        })
            .HasName("AK_Students_Teacher_Id");

        builder.HasIndex(student => new
        {
            student.TeacherAccountId,
            student.ArchivedAtUtc,
            student.Status,
        })
            .HasDatabaseName("IX_Students_Teacher_Archived_Status");
        builder.HasIndex(student => student.SchoolGradeId)
            .HasDatabaseName("IX_Students_SchoolGradeId");
        builder.HasIndex(student => new
        {
            student.TeacherAccountId,
            student.ProgramId,
        })
            .HasDatabaseName("IX_Students_Teacher_ProgramId");
    }
}

internal sealed class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians");
        builder.HasKey(guardian => guardian.Id);
        builder.Property(guardian => guardian.FirstName)
            .HasMaxLength(Guardian.FirstNameMaxLength)
            .IsRequired();
        builder.Property(guardian => guardian.LastName)
            .HasMaxLength(Guardian.LastNameMaxLength)
            .IsRequired();
        builder.Property(guardian => guardian.Relationship)
            .HasMaxLength(Guardian.RelationshipMaxLength);
        builder.Property(guardian => guardian.Email)
            .HasMaxLength(Guardian.EmailMaxLength);
        builder.Property(guardian => guardian.Phone)
            .HasMaxLength(Guardian.PhoneMaxLength);
        builder.Property(guardian => guardian.IsPrimary).IsRequired();
        builder.Property(guardian => guardian.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(guardian => guardian.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(guardian => new
        {
            guardian.StudentId,
            guardian.IsPrimary,
        })
            .HasDatabaseName("IX_Guardians_Student_Primary");
        builder.HasIndex(guardian => guardian.StudentId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("UX_Guardians_Student_Primary");
    }
}
