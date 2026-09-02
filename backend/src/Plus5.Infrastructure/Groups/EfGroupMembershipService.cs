using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Plus5.Application.Groups;
using Plus5.Domain.Groups;
using Plus5.Infrastructure.Persistence;

namespace Plus5.Infrastructure.Groups;

public sealed class EfGroupMembershipService(Plus5DbContext db, TimeProvider clock) : IGroupMembershipService
{
    public async Task<GroupMembershipResult> ChangeAsync(Guid owner, Guid groupId, Guid studentId,
        GroupMembershipCommand command, CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        await using (transaction)
        {
            var group = await db.Groups.SingleOrDefaultAsync(group => group.TeacherAccountId == owner && group.Id == groupId && group.ArchivedAtUtc == null, cancellationToken);
            var student = await db.Students.SingleOrDefaultAsync(student => student.TeacherAccountId == owner && student.Id == studentId && student.ArchivedAtUtc == null, cancellationToken);
            if (group is null || student is null) return GroupMembershipResult.NotFound;
            if (!group.RowVersion.AsSpan().SequenceEqual(command.GroupRowVersion) || !student.RowVersion.AsSpan().SequenceEqual(command.StudentRowVersion))
                return GroupMembershipResult.Conflict;
            var membership = await db.GroupMemberships.SingleOrDefaultAsync(member => member.TeacherAccountId == owner && member.StudentId == studentId && member.LeftAtUtc == null, cancellationToken);
            if (command.Join ? membership is not null : membership?.GroupId != groupId) return GroupMembershipResult.MembershipChanged;
            var count = await db.GroupMemberships.CountAsync(member => member.TeacherAccountId == owner && member.GroupId == groupId && member.LeftAtUtc == null, cancellationToken);
            var now = clock.GetUtcNow();
            if (command.Join)
            {
                if (group.Status != GroupStatus.Active) return GroupMembershipResult.Unavailable;
                if (count >= group.Capacity) return GroupMembershipResult.Full;
                student.AssignToGroupProgram(group.ProgramId, now);
                db.GroupMemberships.Add(new GroupMembership(Guid.NewGuid(), owner, groupId, studentId, now));
            }
            else
            {
                membership!.End(now);
                student.MoveToIndividual(now);
            }
            group.RecordMembershipChange(count + (command.Join ? 1 : -1), now);
            // Always advance both rowversions, including multiple changes within the same clock tick.
            db.Entry(group).Property(group => group.UpdatedAtUtc).IsModified = true;
            db.Entry(student).Property(student => student.UpdatedAtUtc).IsModified = true;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return GroupMembershipResult.Saved;
            }
            catch (DbUpdateConcurrencyException)
            {
                return GroupMembershipResult.Conflict;
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // The unique active-Student membership index also arbitrates concurrent joins to different groups.
                return GroupMembershipResult.Conflict;
            }
        }
    }
}
