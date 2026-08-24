using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Identity;
using Plus5.Domain.Teaching;

namespace Plus5.Infrastructure.Persistence;

internal sealed class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    public void Configure(EntityTypeBuilder<Program> builder)
    {
        builder.ToTable("Programs");
        builder.HasKey(program => program.Id);
        builder.Property(program => program.Name)
            .HasMaxLength(Program.NameMaxLength)
            .IsRequired();
        builder.Property(program => program.NormalizedName)
            .HasMaxLength(Program.NameMaxLength)
            .IsRequired();
        builder.Property(program => program.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(program => program.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(program => new { program.TeacherAccountId, program.NormalizedName })
            .IsUnique()
            .HasDatabaseName("UX_Programs_Teacher_NormalizedName");
    }
}

internal sealed class SchoolGradeConfiguration : IEntityTypeConfiguration<SchoolGrade>
{
    public void Configure(EntityTypeBuilder<SchoolGrade> builder)
    {
        builder.ToTable("SchoolGrades", table =>
            table.HasCheckConstraint("CK_SchoolGrades_SortOrder", "[SortOrder] >= 0"));
        builder.HasKey(grade => grade.Id);
        builder.Property(grade => grade.Code)
            .HasMaxLength(SchoolGrade.CodeMaxLength)
            .IsRequired();
        builder.Property(grade => grade.Name)
            .HasMaxLength(SchoolGrade.NameMaxLength)
            .IsRequired();
        builder.Property(grade => grade.SortOrder).IsRequired();
        builder.HasIndex(grade => grade.Code)
            .IsUnique()
            .HasDatabaseName("UX_SchoolGrades_Code");
        builder.HasIndex(grade => grade.SortOrder)
            .HasDatabaseName("IX_SchoolGrades_SortOrder");
    }
}

internal sealed class ProficiencyLevelConfiguration
    : IEntityTypeConfiguration<ProficiencyLevel>
{
    public void Configure(EntityTypeBuilder<ProficiencyLevel> builder)
    {
        builder.ToTable("ProficiencyLevels", table =>
            table.HasCheckConstraint("CK_ProficiencyLevels_SortOrder", "[SortOrder] >= 0"));
        builder.HasKey(level => level.Id);
        builder.Property(level => level.FrameworkCode)
            .HasMaxLength(ProficiencyLevel.FrameworkCodeMaxLength)
            .IsRequired();
        builder.Property(level => level.Code)
            .HasMaxLength(ProficiencyLevel.CodeMaxLength)
            .IsRequired();
        builder.Property(level => level.Name)
            .HasMaxLength(ProficiencyLevel.NameMaxLength)
            .IsRequired();
        builder.Property(level => level.SortOrder).IsRequired();
        builder.HasIndex(level => new { level.FrameworkCode, level.Code })
            .IsUnique()
            .HasDatabaseName("UX_ProficiencyLevels_Framework_Code");
        builder.HasIndex(level => new { level.FrameworkCode, level.SortOrder })
            .HasDatabaseName("IX_ProficiencyLevels_Framework_SortOrder");
    }
}

internal sealed class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curricula");
        builder.HasKey(curriculum => curriculum.Id);
        builder.Property(curriculum => curriculum.Code)
            .HasMaxLength(Curriculum.CodeMaxLength)
            .IsRequired();
        builder.Property(curriculum => curriculum.Name)
            .HasMaxLength(Curriculum.NameMaxLength)
            .IsRequired();
        builder.Property(curriculum => curriculum.Version)
            .HasMaxLength(Curriculum.VersionMaxLength)
            .IsRequired();
        builder.HasIndex(curriculum => new { curriculum.Code, curriculum.Version })
            .IsUnique()
            .HasDatabaseName("UX_Curricula_Code_Version");
    }
}
