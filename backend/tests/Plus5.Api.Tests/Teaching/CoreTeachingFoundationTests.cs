using Microsoft.EntityFrameworkCore;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.Teaching;

public sealed class CoreTeachingFoundationTests
{
    [Fact]
    public void ProgramPreservesTeacherOwnershipAndNormalizesName()
    {
        var teacherAccountId = Guid.NewGuid();

        var program = new TeachingProgram(
            Guid.NewGuid(),
            teacherAccountId,
            "  Grammar Focus  ",
            new DateTimeOffset(2026, 8, 24, 21, 0, 0, TimeSpan.Zero));

        Assert.Equal(teacherAccountId, program.TeacherAccountId);
        Assert.Equal("Grammar Focus", program.Name);
        Assert.Equal("GRAMMAR FOCUS", program.NormalizedName);
    }

    [Fact]
    public void ProgramRejectsNonUtcCreationTimestamp()
    {
        Assert.Throws<ArgumentException>(() => new TeachingProgram(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grammar Focus",
            new DateTimeOffset(2026, 8, 24, 21, 0, 0, TimeSpan.FromHours(2))));
    }

    [Fact]
    public void SchoolGradeAndProficiencyLevelRemainSeparateDimensions()
    {
        var grade = new SchoolGrade(Guid.NewGuid(), " grade-8 ", "8. razred", 8);
        var level = new ProficiencyLevel(Guid.NewGuid(), " cefr ", " b1 ", "B1", 3);

        Assert.Equal("GRADE-8", grade.Code);
        Assert.Equal("CEFR", level.FrameworkCode);
        Assert.Equal("B1", level.Code);
    }

    [Fact]
    public void ReferenceEntitiesRejectNegativeSortOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SchoolGrade(Guid.NewGuid(), "GRADE-8", "8. razred", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProficiencyLevel(Guid.NewGuid(), "CEFR", "B1", "B1", -1));
    }

    [Fact]
    public void CurriculumNormalizesStableIdentityButPreservesDisplayName()
    {
        var curriculum = new Curriculum(
            Guid.NewGuid(),
            " hr-eng ",
            "Nacionalni kurikulum – Engleski jezik",
            " 2026-v1 ");

        Assert.Equal("HR-ENG", curriculum.Code);
        Assert.Equal("2026-V1", curriculum.Version);
        Assert.Equal("Nacionalni kurikulum – Engleski jezik", curriculum.Name);
    }

    [Fact]
    public void EfModelProtectsCoreTeachingNaturalKeysAndOwnership()
    {
        using var dbContext = CreateDbContext();
        var model = dbContext.Model;
        var program = model.FindEntityType(typeof(TeachingProgram))!;
        var grade = model.FindEntityType(typeof(SchoolGrade))!;
        var level = model.FindEntityType(typeof(ProficiencyLevel))!;
        var curriculum = model.FindEntityType(typeof(Curriculum))!;

        Assert.Contains(
            program.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([
                        nameof(TeachingProgram.TeacherAccountId),
                        nameof(TeachingProgram.NormalizedName),
                    ]));
        Assert.Contains(grade.GetIndexes(), index => index.IsUnique);
        Assert.Contains(level.GetIndexes(), index => index.IsUnique);
        Assert.Contains(curriculum.GetIndexes(), index => index.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, Assert.Single(program.GetForeignKeys()).DeleteBehavior);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Plus5DbContext(options);
    }
}
