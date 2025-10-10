namespace STG.Api.DTOs;

public sealed record SubjectDto(string Name, bool NeedsLab = false, bool NeedsComputerRoom = false);
