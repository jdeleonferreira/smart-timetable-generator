namespace STG.Api.DTOs;

public sealed record ExportRequest(
    int Year,
    List<string>? Grades,   // ej: ["6","7"]
    List<string>? Groups    // ej: ["6A","6B"]
);
