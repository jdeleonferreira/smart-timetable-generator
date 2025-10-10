namespace STG.Api.DTOs;

public sealed record CurriculumLineRequest(int Year, string Grade, string Subject, int WeeklyBlocks);
public sealed record CurriculumLineResponse(int Year, string Grade, string Subject, int WeeklyBlocks);
