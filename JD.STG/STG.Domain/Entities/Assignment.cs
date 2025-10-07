using STG.Domain.ValueObjects;

namespace STG.Domain.Entities;

public sealed class Assignment
{
    public string GroupCode { get; }     // e.g., "6A"
    public string Subject { get; }       // e.g., "Matemáticas"
    public string Teacher { get; }       // e.g., "García"
    public string Room { get; }          // e.g., "A101"
    public TimeSlot Slot { get; }        // Día/Block
    public int Blocks { get; }           // 1 si simple; 2 si doble

    public Assignment(string groupCode, string subject, string teacher, string room, TimeSlot slot, int blocks = 1)
    {
        if (string.IsNullOrWhiteSpace(groupCode)) throw new ArgumentException("GroupCode required.");
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject required.");
        if (string.IsNullOrWhiteSpace(teacher)) throw new ArgumentException("Teacher required.");
        if (string.IsNullOrWhiteSpace(room)) throw new ArgumentException("Room required.");
        if (blocks <= 0) throw new ArgumentOutOfRangeException(nameof(blocks));

        GroupCode = groupCode.Trim();
        Subject = subject.Trim();
        Teacher = teacher.Trim();
        Room = room.Trim();
        Slot = slot;
        Blocks = blocks;
    }

    /// <summary>Devuelve los slots ocupados por este bloque (p.ej. doble cubre Block y Block+1).</summary>
    public IEnumerable<TimeSlot> CoveredSlots()
    {
        for (int i = 0; i < Blocks; i++)
            yield return new TimeSlot(Slot.Day, Slot.Block + i);
    }
}