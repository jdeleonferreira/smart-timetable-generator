namespace SmartTimetableGenerator.Domain.DayTypes;

// Especificaciones del agregado: una clase con un método de fábrica por consulta (ver docs/adr)
public sealed class DayTypeSpec : SingleResultSpecification<DayType>
{
    public static DayTypeSpec ById(DayTypeId id)
    {
        var spec = new DayTypeSpec();
        spec.Query.Where(x => x.Id == id);
        return spec;
    }

    public static DayTypeSpec Default()
    {
        var spec = new DayTypeSpec();
        spec.Query.Where(x => x.IsDefault);
        return spec;
    }

    public static DayTypeSpec ByCode(string code)
    {
        var spec = new DayTypeSpec();
        var normalized = code.Trim().ToUpperInvariant();
        spec.Query.Where(x => x.Code == normalized);
        return spec;
    }
}
