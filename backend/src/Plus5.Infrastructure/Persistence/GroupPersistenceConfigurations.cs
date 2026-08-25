using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Plus5.Domain.Groups;
using Plus5.Domain.Identity;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Infrastructure.Persistence;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups", table =>
        {
            table.HasCheckConstraint(
                "CK_Groups_Capacity",
                "[Capacity] > 0");
            table.HasCheckConstraint(
                "CK_Groups_Status",
                "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint(
                "CK_Groups_ArchivedStatus",
                "[ArchivedAtUtc] IS NULL OR [Status] = 3");
        });

        builder.HasKey(group => group.Id);
        builder.Property(group => group.Name)
            .HasMaxLength(Group.NameMaxLength)
            .IsRequired();
        builder.Property(group => group.NormalizedName)
            .HasMaxLength(Group.NameMaxLength)
            .IsRequired();
        builder.Property(group => group.Description)
            .HasMaxLength(Group.DescriptionMaxLength);
        builder.Property(group => group.Capacity).IsRequired();
        builder.Property(group => group.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(group => group.CreatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(group => group.UpdatedAtUtc).HasPrecision(7).IsRequired();
        builder.Property(group => group.ArchivedAtUtc).HasPrecision(7);
        builder.Property(group => group.RowVersion).IsRowVersion();

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(group => group.TeacherAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TeachingProgram>()
            .WithMany()
            .HasForeignKey(group => new
            {
                group.TeacherAccountId,
                group.ProgramId,
            })
            .HasPrincipalKey(program => new
            {
                program.TeacherAccountId,
                program.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchoolGrade>()
            .WithMany()
            .HasForeignKey(group => group.SchoolGradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(group => new
        {
            group.TeacherAccountId,
            group.Id,
        })
            .HasName("AK_Groups_Teacher_Id");
        builder.HasIndex(group => new
        {
            group.TeacherAccountId,
            group.NormalizedName,
        })
            .IsUnique()
            .HasDatabaseName("UX_Groups_Teacher_NormalizedName");
        builder.HasIndex(group => new
        {
            group.TeacherAccountId,
            group.ArchivedAtUtc,
            group.Status,
        })
            .HasDatabaseName("IX_Groups_Teacher_Archived_Status");
        builder.HasIndex(group => new
        {
            group.TeacherAccountId,
            group.ProgramId,
        })
            .HasDatabaseName("IX_Groups_Teacher_ProgramId");
        builder.HasIndex(group => group.SchoolGradeId)
            .HasDatabaseName("IX_Groups_SchoolGradeId");
    }
}

internal sealed class GroupMembershipConfiguration
    : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.ToTable("GroupMemberships", table =>
            table.HasCheckConstraint(
                "CK_GroupMemberships_Validity",
                "[LeftAtUtc] IS NULL OR [LeftAtUtc] >= [JoinedAtUtc]"));
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.JoinedAtUtc)
            .HasPrecision(7)
            .IsRequired();
        builder.Property(membership => membership.LeftAtUtc).HasPrecision(7);
        builder.Ignore(membership => membership.IsActive);

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(membership => new
            {
                membership.TeacherAccountId,
                membership.GroupId,
            })
            .HasPrincipalKey(group => new
            {
                group.TeacherAccountId,
                group.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(membership => new
            {
                membership.TeacherAccountId,
                membership.StudentId,
            })
            .HasPrincipalKey(student => new
            {
                student.TeacherAccountId,
                student.Id,
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => new
        {
            membership.GroupId,
            membership.LeftAtUtc,
        })
            .HasDatabaseName("IX_GroupMemberships_Group_LeftAtUtc");
        builder.HasIndex(membership => membership.StudentId)
            .IsUnique()
            .HasFilter("[LeftAtUtc] IS NULL")
            .HasDatabaseName("UX_GroupMemberships_Student_Active");
        builder.HasIndex(membership => new
        {
            membership.GroupId,
            membership.StudentId,
            membership.JoinedAtUtc,
        })
            .IsUnique()
            .HasDatabaseName("UX_GroupMemberships_Group_Student_JoinedAtUtc");
    }
}
