using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Plus5.Application.Students;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Students;

public sealed class EfStudentCreationService(
    Plus5DbContext dbContext,
    TimeProvider timeProvider) : IStudentCreationService
{
    public async Task<StudentCreateOptions> GetOptionsAsync(
        Guid teacherAccountId,
        Guid? programId,
        CancellationToken cancellationToken)
    {
        var schoolGrades = await dbContext.SchoolGrades
            .AsNoTracking()
            .OrderBy(grade => grade.SortOrder)
            .ThenBy(grade => grade.Name)
            .Select(grade => new StudentCreateOption(grade.Id, grade.Name, grade.Code))
            .ToListAsync(cancellationToken);

        var programs = await dbContext.Programs
            .AsNoTracking()
            .Where(program => program.TeacherAccountId == teacherAccountId)
            .OrderBy(program => program.Name)
            .Select(program => new StudentCreateOption(program.Id, program.Name, null))
            .ToListAsync(cancellationToken);

        var groups = programId.HasValue
            ? await dbContext.Groups
                .AsNoTracking()
                .Where(group =>
                    group.TeacherAccountId == teacherAccountId &&
                    group.ProgramId == programId.Value &&
                    group.Status == GroupStatus.Active &&
                    group.ArchivedAtUtc == null)
                .OrderBy(group => group.Name)
                .Select(group => new StudentGroupCreateOption(
                    group.Id,
                    group.Name,
                    group.ProgramId,
                    dbContext.GroupMemberships.Count(membership =>
                        membership.GroupId == group.Id && membership.LeftAtUtc == null),
                    group.Capacity))
                .ToListAsync(cancellationToken)
            : [];

        return new StudentCreateOptions(schoolGrades, programs, groups);
    }

    public async Task<StudentCreateResult> CreateAsync(
        Guid teacherAccountId,
        StudentCreateCommand command,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.SchoolGrades.AnyAsync(
                grade => grade.Id == command.SchoolGradeId,
                cancellationToken))
        {
            return StudentCreateResult.Failed(StudentCreateFailure.SchoolGradeNotFound);
        }

        if (command.ProgramId.HasValue && !await dbContext.Programs.AnyAsync(
                program => program.Id == command.ProgramId.Value &&
                    program.TeacherAccountId == teacherAccountId,
                cancellationToken))
        {
            return StudentCreateResult.Failed(StudentCreateFailure.ProgramNotFound);
        }

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        await using (transaction)
        {
            var now = timeProvider.GetUtcNow();
            var studentId = Guid.NewGuid();
            Group? group = null;
            var activeMemberCount = 0;

            if (command.DeliveryMode == StudentCreationDeliveryMode.Group)
            {
                group = await dbContext.Groups.SingleOrDefaultAsync(
                    candidate => candidate.Id == command.GroupId &&
                        candidate.TeacherAccountId == teacherAccountId,
                    cancellationToken);

                if (group is null)
                {
                    return StudentCreateResult.Failed(StudentCreateFailure.GroupNotFound);
                }

                if (group.Status != GroupStatus.Active || group.ArchivedAtUtc.HasValue)
                {
                    return StudentCreateResult.Failed(StudentCreateFailure.GroupUnavailable);
                }

                if (group.ProgramId != command.ProgramId)
                {
                    return StudentCreateResult.Failed(StudentCreateFailure.GroupProgramMismatch);
                }

                activeMemberCount = await dbContext.GroupMemberships.CountAsync(
                    membership => membership.GroupId == group.Id && membership.LeftAtUtc == null,
                    cancellationToken);

                if (activeMemberCount >= group.Capacity)
                {
                    return StudentCreateResult.Failed(StudentCreateFailure.GroupCapacityReached);
                }
            }

            var student = new Student(
                studentId,
                teacherAccountId,
                command.SchoolGradeId,
                command.FirstName,
                command.LastName,
                MapStatus(command.Status),
                now,
                command.ProgramId,
                command.DeliveryMode.HasValue ? MapDeliveryMode(command.DeliveryMode.Value) : null,
                dateOfBirth: command.DateOfBirth,
                schoolName: command.SchoolName,
                gender: command.Gender,
                email: command.Email,
                phone: command.Phone);

            dbContext.Students.Add(student);

            if (command.Guardian is not null)
            {
                dbContext.Guardians.Add(new Guardian(
                    Guid.NewGuid(),
                    studentId,
                    command.Guardian.FirstName,
                    command.Guardian.LastName,
                    true,
                    now,
                    email: command.Guardian.Email,
                    phone: command.Guardian.Phone));
            }

            if (group is not null)
            {
                group.RecordMembershipChange(activeMemberCount + 1, now);
                dbContext.Entry(group).Property(candidate => candidate.UpdatedAtUtc).IsModified = true;
                dbContext.GroupMemberships.Add(new GroupMembership(
                    Guid.NewGuid(),
                    teacherAccountId,
                    group.Id,
                    studentId,
                    now));
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                return StudentCreateResult.Failed(StudentCreateFailure.ConcurrencyConflict);
            }

            return StudentCreateResult.Success(studentId);
        }
    }

    private static StudentStatus MapStatus(StudentCreationStatus status) => status switch
    {
        StudentCreationStatus.Active => StudentStatus.Active,
        StudentCreationStatus.OnHold => StudentStatus.OnHold,
        StudentCreationStatus.Inactive => StudentStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static DeliveryMode MapDeliveryMode(StudentCreationDeliveryMode mode) => mode switch
    {
        StudentCreationDeliveryMode.Individual => DeliveryMode.Individual,
        StudentCreationDeliveryMode.Group => DeliveryMode.Group,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
