namespace Plus5.Domain.Scheduling;

public enum ScheduleMaterializationIssueType
{
    TeacherConflict = 1,
    LocationConflict = 2,
    InvalidLocalTime = 3,
    AmbiguousLocalTime = 4,
    ConcurrencyFailure = 5,
    MaterializationFailure = 6,
}
