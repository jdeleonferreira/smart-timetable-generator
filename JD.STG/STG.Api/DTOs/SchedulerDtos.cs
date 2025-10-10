using System.ComponentModel.DataAnnotations;

namespace STG.Api.DTOs;

public sealed record WeekRequest(
    [Range(1, 12)] 
    int BlocksPerDay,
    [Range(30, 120)] 
    int BlockLengthMinutes
);

public sealed record AssignmentResponse(
    string GroupCode, string Subject, string Teacher, string Room, string Day, int Block
);

public sealed record TimetableResponse(
    Guid Id, int Year, List<AssignmentResponse> Assignments
);
