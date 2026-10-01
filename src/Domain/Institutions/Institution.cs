namespace SmartTimetableGenerator.Domain.Institutions;

[ValueObject<Guid>]
public readonly partial struct InstitutionId;

/// <summary>
/// Institución educativa. La v1 maneja una sola institución.
/// </summary>
public class Institution : AggregateRoot<InstitutionId>
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 30;
    public const int AddressMaxLength = 250;

    public string Name { get; private set; } = null!;

    /// <summary>NIT de la institución.</summary>
    public string? Nit { get; private set; }

    /// <summary>Código DANE.</summary>
    public string? DaneCode { get; private set; }

    public string? Address { get; private set; }

    private Institution() { } // Needed for EF Core

    public static Institution Create(string name, string? nit = null, string? daneCode = null, string? address = null)
    {
        var institution = new Institution { Id = InstitutionId.From(Guid.CreateVersion7()) };
        institution.Update(name, nit, daneCode, address);
        return institution;
    }

    public void Update(string name, string? nit, string? daneCode, string? address)
    {
        Name = TextRules.Required(name, NameMaxLength, nameof(name));
        Nit = TextRules.Optional(nit, CodeMaxLength, nameof(nit));
        DaneCode = TextRules.Optional(daneCode, CodeMaxLength, nameof(daneCode));
        Address = TextRules.Optional(address, AddressMaxLength, nameof(address));
    }
}
