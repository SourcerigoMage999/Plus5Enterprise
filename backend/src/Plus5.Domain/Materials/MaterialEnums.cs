namespace Plus5.Domain.Materials;

public enum MaterialStatus
{
    Active = 1,
    Archived = 2,
}

public enum MaterialVisibility
{
    Private = 1,
    Shared = 2,
}

public enum MaterialVersionStatus
{
    Draft = 1,
    Active = 2,
    Superseded = 3,
}

public enum MaterialFileFormat
{
    Pdf = 1,
    Docx = 2,
    Pptx = 3,
    Mp4 = 4,
    Zip = 5,
}

public enum MaterialFileStatus
{
    PendingUpload = 1,
    Uploaded = 2,
    Scanning = 3,
    Clean = 4,
    Quarantined = 5,
    Rejected = 6,
    Failed = 7,
}

public enum MaterialShareAccess
{
    View = 1,
    Use = 2,
}
