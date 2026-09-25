namespace Plus5.Domain.Readiness;

public enum ReadinessConfidence
{
    NoData = 1,
    VeryLow = 2,
    Low = 3,
    Medium = 4,
    High = 5,
}

public enum ReadinessStatus
{
    InsufficientData = 1,
    NeedsWork = 2,
    Developing = 3,
    Ready = 4,
    Strong = 5,
}
