namespace STG.Api.DTOs;

public sealed record CreateTeacherRequest(string Name, List<string> Subjects);
public sealed record CreateRoomRequest(string Name, int Capacity, List<string>? Tags);
public sealed record CreateGroupRequest(string Grade, string Label, int Size);

public sealed record TeacherDto(string Name, List<string> Subjects);
public sealed record RoomDto(string Name, int Capacity, List<string> Tags);
public sealed record GroupDto(string Grade, string Label, int Size, string Code);
