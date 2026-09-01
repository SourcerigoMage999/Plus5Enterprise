using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Plus5.Application.Students;
using Plus5.Domain.Groups;
using Plus5.Domain.Students;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Students;

public sealed class EfStudentEditingService(
    Plus5DbContext dbContext,
    TimeProvider timeProvider) : IStudentEditingService
{
    public async Task<StudentEditModel?> GetAsync(
        Guid teacherAccountId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await dbContext.Students
            .AsNoTracking()
            .Where(candidate => candidate.Id == studentId
                && candidate.TeacherAccountId == teacherAccountId
                && candidate.ArchivedAtUtc == null)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.FirstName,
                candidate.LastName,
                candidate.Nickname,
                candidate.DateOfBirth,
                candidate.SchoolName,
                candidate.Gender,
                candidate.Email,
                candidate.Phone,
                candidate.SchoolGradeId,
                candidate.ProgramId,
                candidate.DeliveryMode,
                candidate.Status,
                candidate.UpdatedAtUtc,
                candidate.RowVersion,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            return null;
        }

        var groupId = await dbContext.GroupMemberships
            .AsNoTracking()
            .Where(membership => membership.TeacherAccountId == teacherAccountId
                && membership.StudentId == studentId
                && membership.LeftAtUtc == null)
            .Select(membership => (Guid?)membership.GroupId)
            .SingleOrDefaultAsync(cancellationToken);
        var guardians = await dbContext.Guardians
            .AsNoTracking()
            .Where(guardian => guardian.StudentId == studentId)
            .OrderByDescending(guardian => guardian.IsPrimary)
            .ThenBy(guardian => guardian.LastName)
            .ThenBy(guardian => guardian.FirstName)
            .Select(guardian => new StudentEditGuardian(
                guardian.Id,
                guardian.FirstName,
                guardian.LastName,
                guardian.Relationship,
                guardian.Email,
                guardian.Phone,
                guardian.IsPrimary))
            .ToListAsync(cancellationToken);

        return new StudentEditModel(
            student.Id,
            student.FirstName,
            student.LastName,
            student.Nickname,
            student.DateOfBirth,
            student.SchoolName,
            student.Gender,
            student.Email,
            student.Phone,
            student.SchoolGradeId,
            student.ProgramId,
            student.DeliveryMode.HasValue
                ? (StudentCreationDeliveryMode?)student.DeliveryMode.Value
                : null,
            groupId,
            (StudentCreationStatus)student.Status,
            student.UpdatedAtUtc,
            student.RowVersion,
            guardians);
    }

    public async Task<StudentEditResult> UpdateAsync(
        Guid teacherAccountId,
        Guid studentId,
        StudentEditCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Guardians.Count > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "At most ten guardians are supported.");
        }

        if (command.Guardians.Count(guardian => guardian.IsPrimary) > 1)
        {
            return StudentEditResult.Failed(StudentEditFailure.MultiplePrimaryGuardians);
        }

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        await using (transaction)
        {
            var student = await dbContext.Students.SingleOrDefaultAsync(
                candidate => candidate.Id == studentId
                    && candidate.TeacherAccountId == teacherAccountId
                    && candidate.ArchivedAtUtc == null,
                cancellationToken);
            if (student is null)
            {
                return StudentEditResult.Failed(StudentEditFailure.StudentNotFound);
            }

            if (!student.RowVersion.AsSpan().SequenceEqual(command.RowVersion))
            {
                return StudentEditResult.Failed(StudentEditFailure.ConcurrencyConflict);
            }

            dbContext.Entry(student).Property(candidate => candidate.RowVersion).OriginalValue = command.RowVersion;

            if (!await dbContext.SchoolGrades.AnyAsync(
                    grade => grade.Id == command.SchoolGradeId,
                    cancellationToken))
            {
                return StudentEditResult.Failed(StudentEditFailure.SchoolGradeNotFound);
            }

            if (command.ProgramId.HasValue && !await dbContext.Programs.AnyAsync(
                    program => program.Id == command.ProgramId
                        && program.TeacherAccountId == teacherAccountId,
                    cancellationToken))
            {
                return StudentEditResult.Failed(StudentEditFailure.ProgramNotFound);
            }

            var membership = await dbContext.GroupMemberships.SingleOrDefaultAsync(
                candidate => candidate.TeacherAccountId == teacherAccountId
                    && candidate.StudentId == studentId
                    && candidate.LeftAtUtc == null,
                cancellationToken);
            Group? currentGroup = null;
            if (membership is not null)
            {
                currentGroup = await dbContext.Groups.SingleAsync(
                    group => group.Id == membership.GroupId
                        && group.TeacherAccountId == teacherAccountId,
                    cancellationToken);
            }

            Group? targetGroup = null;
            if (command.DeliveryMode == StudentCreationDeliveryMode.Group)
            {
                targetGroup = currentGroup?.Id == command.GroupId
                    ? currentGroup
                    : await dbContext.Groups.SingleOrDefaultAsync(
                        group => group.Id == command.GroupId
                            && group.TeacherAccountId == teacherAccountId,
                        cancellationToken);

                if (targetGroup is null)
                {
                    return StudentEditResult.Failed(StudentEditFailure.GroupNotFound);
                }

                if (targetGroup.Status != GroupStatus.Active || targetGroup.ArchivedAtUtc.HasValue)
                {
                    return StudentEditResult.Failed(StudentEditFailure.GroupUnavailable);
                }

                if (targetGroup.ProgramId != command.ProgramId)
                {
                    return StudentEditResult.Failed(StudentEditFailure.GroupProgramMismatch);
                }

                if (targetGroup.Id != currentGroup?.Id)
                {
                    var targetCount = await ActiveMemberCountAsync(targetGroup.Id, cancellationToken);
                    if (targetCount >= targetGroup.Capacity)
                    {
                        return StudentEditResult.Failed(StudentEditFailure.GroupCapacityReached);
                    }
                }
            }

            var guardians = await dbContext.Guardians
                .Where(guardian => guardian.StudentId == studentId)
                .ToListAsync(cancellationToken);
            var suppliedIds = command.Guardians
                .Where(guardian => guardian.Id.HasValue)
                .Select(guardian => guardian.Id!.Value)
                .ToList();
            if (suppliedIds.Count != suppliedIds.Distinct().Count()
                || suppliedIds.Any(id => guardians.All(guardian => guardian.Id != id)))
            {
                return StudentEditResult.Failed(StudentEditFailure.GuardianNotFound);
            }

            if (guardians.Any(guardian => !suppliedIds.Contains(guardian.Id)))
            {
                return StudentEditResult.Failed(StudentEditFailure.GuardianSetMismatch);
            }

            if (guardians.Any(guardian => guardian.IsPrimary))
            {
                guardians.ForEach(guardian => guardian.ClearPrimary());
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var now = timeProvider.GetUtcNow();
            await ApplyMembershipChangeAsync(
                teacherAccountId,
                studentId,
                now,
                membership,
                currentGroup,
                targetGroup,
                cancellationToken);
            student.UpdateAdministrativeDetails(
                command.SchoolGradeId,
                command.FirstName,
                command.LastName,
                MapStatus(command.Status),
                now,
                command.ProgramId,
                command.DeliveryMode.HasValue ? MapDeliveryMode(command.DeliveryMode.Value) : null,
                command.Nickname,
                command.DateOfBirth,
                command.SchoolName,
                command.Gender,
                command.Email,
                command.Phone);

            foreach (var input in command.Guardians)
            {
                if (input.Id.HasValue)
                {
                    guardians.Single(guardian => guardian.Id == input.Id).Update(
                        input.FirstName,
                        input.LastName,
                        input.IsPrimary,
                        input.Relationship,
                        input.Email,
                        input.Phone);
                }
                else
                {
                    dbContext.Guardians.Add(new Guardian(
                        Guid.NewGuid(),
                        studentId,
                        input.FirstName,
                        input.LastName,
                        input.IsPrimary,
                        now,
                        input.Relationship,
                        input.Email,
                        input.Phone));
                }
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
                return StudentEditResult.Failed(StudentEditFailure.ConcurrencyConflict);
            }

            return StudentEditResult.Success(student.RowVersion);
        }
    }

    public async Task<StudentEditResult> ArchiveAsync(
        Guid teacherAccountId,
        Guid studentId,
        byte[] rowVersion,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        await using (transaction)
        {
            var student = await dbContext.Students.SingleOrDefaultAsync(
                candidate => candidate.Id == studentId
                    && candidate.TeacherAccountId == teacherAccountId
                    && candidate.ArchivedAtUtc == null,
                cancellationToken);
            if (student is null)
            {
                return StudentEditResult.Failed(StudentEditFailure.StudentNotFound);
            }

            if (!student.RowVersion.AsSpan().SequenceEqual(rowVersion))
            {
                return StudentEditResult.Failed(StudentEditFailure.ConcurrencyConflict);
            }

            dbContext.Entry(student).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
            var membership = await dbContext.GroupMemberships.SingleOrDefaultAsync(
                candidate => candidate.TeacherAccountId == teacherAccountId
                    && candidate.StudentId == studentId
                    && candidate.LeftAtUtc == null,
                cancellationToken);
            var now = timeProvider.GetUtcNow();
            if (membership is not null)
            {
                var group = await dbContext.Groups.SingleAsync(
                    candidate => candidate.Id == membership.GroupId
                        && candidate.TeacherAccountId == teacherAccountId,
                    cancellationToken);
                var count = await ActiveMemberCountAsync(group.Id, cancellationToken);
                membership.End(now);
                group.RecordMembershipChange(count - 1, now);
                dbContext.Entry(group).Property(candidate => candidate.UpdatedAtUtc).IsModified = true;
            }

            student.Archive(now);
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
                return StudentEditResult.Failed(StudentEditFailure.ConcurrencyConflict);
            }

            return StudentEditResult.Success(student.RowVersion);
        }
    }

    private async Task ApplyMembershipChangeAsync(
        Guid teacherAccountId,
        Guid studentId,
        DateTimeOffset now,
        GroupMembership? membership,
        Group? currentGroup,
        Group? targetGroup,
        CancellationToken cancellationToken)
    {
        if (currentGroup?.Id == targetGroup?.Id)
        {
            return;
        }

        if (membership is not null && currentGroup is not null)
        {
            var currentCount = await ActiveMemberCountAsync(currentGroup.Id, cancellationToken);
            membership.End(now);
            currentGroup.RecordMembershipChange(currentCount - 1, now);
            dbContext.Entry(currentGroup).Property(group => group.UpdatedAtUtc).IsModified = true;
        }

        if (targetGroup is not null)
        {
            var targetCount = await ActiveMemberCountAsync(targetGroup.Id, cancellationToken);
            targetGroup.RecordMembershipChange(targetCount + 1, now);
            dbContext.Entry(targetGroup).Property(group => group.UpdatedAtUtc).IsModified = true;
            dbContext.GroupMemberships.Add(new GroupMembership(
                Guid.NewGuid(),
                teacherAccountId,
                targetGroup.Id,
                studentId,
                now));
        }
    }

    private Task<int> ActiveMemberCountAsync(Guid groupId, CancellationToken cancellationToken) =>
        dbContext.GroupMemberships.CountAsync(
            membership => membership.GroupId == groupId && membership.LeftAtUtc == null,
            cancellationToken);

    private static StudentStatus MapStatus(StudentCreationStatus status) => (StudentStatus)status;

    private static DeliveryMode MapDeliveryMode(StudentCreationDeliveryMode mode) => (DeliveryMode)mode;
}
