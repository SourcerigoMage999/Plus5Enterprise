using Microsoft.EntityFrameworkCore;
using Plus5.Application.LessonPlanning;
using Plus5.Domain.Identity;
using Plus5.Domain.LessonPlanning;
using Plus5.Domain.Scheduling;
using Plus5.Domain.Students;
using Plus5.Domain.Teaching;
using Plus5.Infrastructure.LessonPlanning;
using Plus5.Infrastructure.Persistence;
using TeachingProgram = Plus5.Domain.Teaching.Program;

namespace Plus5.Api.Tests.LessonPlanning;

public sealed class LessonGoalSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LessonPlanRequiresExactlyOneParticipantAndNormalizesGoal()
    {
        var teacherId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => new LessonPlan(Guid.NewGuid(), teacherId, null, null, null, Now));
        Assert.Throws<ArgumentException>(() => new LessonPlan(Guid.NewGuid(), teacherId, Guid.NewGuid(), Guid.NewGuid(), null, Now));

        var plan = new LessonPlan(Guid.NewGuid(), teacherId, Guid.NewGuid(), null, null, Now);
        var version = new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, "  Razlikovati vremena.  ", Now);

        Assert.Equal(LessonPlanLifecycleStatus.Active, plan.LifecycleStatus);
        Assert.Equal(LessonPlanVersionStatus.Draft, version.Status);
        Assert.Equal("Razlikovati vremena.", version.Goal);
        Assert.Throws<ArgumentException>(() => new LessonPlanVersion(Guid.NewGuid(), plan.Id, 1, " ", Now));
    }

    [Fact]
    public async Task QueryIsOwnerScopedAndReturnsOnlyPublishedActiveKnowledge()
    {
        var data = await SeedAsync();
        var query = new EfLessonGoalSelectionQuery(data.Db, new FixedTimeProvider());

        var participants = await query.GetParticipantsAsync(data.Teacher.Id,
            LessonParticipantKind.Student, 1, 50, null, CancellationToken.None);
        var sessions = await query.GetSessionsAsync(data.Teacher.Id,
            LessonParticipantKind.Student, data.Student.Id, CancellationToken.None);
        var components = await query.GetKnowledgeComponentsAsync(1, 50, null, CancellationToken.None);

        Assert.Single(participants.Items);
        Assert.Equal(data.Student.Id, participants.Items[0].Id);
        Assert.Single(sessions);
        Assert.Equal(data.Session.Id, sessions[0].Id);
        Assert.Single(components.Items);
        Assert.Equal(data.Component.Id, components.Items[0].Id);
    }

    [Fact]
    public async Task CreatePersistsDraftAtomicallyAndRejectsCrossContextSession()
    {
        var data = await SeedAsync();
        var service = new EfLessonGoalSelectionService(data.Db, new FixedTimeProvider());

        var mismatch = await service.CreateAsync(data.Teacher.Id,
            new(LessonParticipantKind.Group, data.GroupId, data.Session.Id, "Cilj", [data.Component.Id]),
            CancellationToken.None);
        Assert.Equal(CreateLessonGoalFailure.SessionMismatch, mismatch.Failure);

        var result = await service.CreateAsync(data.Teacher.Id,
            new(LessonParticipantKind.Student, data.Student.Id, data.Session.Id,
                "Razlikovati Present Perfect i Past Simple.", [data.Component.Id]),
            CancellationToken.None);

        Assert.Equal(CreateLessonGoalFailure.None, result.Failure);
        var plan = await data.Db.LessonPlans.SingleAsync();
        var version = await data.Db.LessonPlanVersions.SingleAsync();
        var mapping = await data.Db.LessonPlanVersionKnowledgeComponents.SingleAsync();
        Assert.Equal(data.Teacher.Id, plan.TeacherAccountId);
        Assert.Equal(data.Student.Id, plan.StudentId);
        Assert.Equal(data.Session.Id, plan.SessionId);
        Assert.Equal(plan.Id, version.LessonPlanId);
        Assert.Equal(45, version.TargetDurationMinutes);
        Assert.Equal(data.Component.Id, mapping.KnowledgeComponentId);
    }

    private static async Task<SeededData> SeedAsync()
    {
        var db = CreateDbContext();
        var teacher = new UserAccount(Guid.NewGuid(), "owner@plus5.local", "OWNER@PLUS5.LOCAL", Now.AddDays(-10));
        var foreign = new UserAccount(Guid.NewGuid(), "foreign@plus5.local", "FOREIGN@PLUS5.LOCAL", Now.AddDays(-10));
        var grade = new SchoolGrade(Guid.NewGuid(), "G8", "8. razred", 8);
        var program = new TeachingProgram(Guid.NewGuid(), teacher.Id, "Grammar Focus", Now.AddDays(-10));
        var foreignProgram = new TeachingProgram(Guid.NewGuid(), foreign.Id, "Foreign", Now.AddDays(-10));
        var student = new Student(Guid.NewGuid(), teacher.Id, grade.Id, "Petar", "Horvat",
            StudentStatus.Active, Now.AddDays(-10), program.Id, DeliveryMode.Individual);
        var foreignStudent = new Student(Guid.NewGuid(), foreign.Id, grade.Id, "Foreign", "Student",
            StudentStatus.Active, Now.AddDays(-10), foreignProgram.Id, DeliveryMode.Individual);
        var group = new Plus5.Domain.Groups.Group(Guid.NewGuid(), teacher.Id, program.Id, grade.Id,
            "Grammar 8A", 8, Plus5.Domain.Groups.GroupStatus.Active, Now.AddDays(-10));
        var session = new Session(Guid.NewGuid(), teacher.Id, DeliveryMode.Individual, student.Id,
            Now.AddDays(2), Now.AddDays(2).AddMinutes(45), "Europe/Zagreb", Now.AddDays(-2));
        var model = new KnowledgeModel(Guid.NewGuid(), "ENG-G8", "2026");
        var area = new KnowledgeArea(Guid.NewGuid(), model, "Grammar", 0);
        var component = new KnowledgeComponent(Guid.NewGuid(), model, area, "Present Perfect — use", 0);
        model.Publish();
        db.AddRange(teacher, foreign, grade, program, foreignProgram, student, foreignStudent, group,
            session, model, area, component);
        await db.SaveChangesAsync();
        return new(db, teacher, student, group.Id, session, component);
    }

    private static Plus5DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Plus5DbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new Plus5DbContext(options);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed record SeededData(Plus5DbContext Db, UserAccount Teacher, Student Student,
        Guid GroupId, Session Session, KnowledgeComponent Component);
}
