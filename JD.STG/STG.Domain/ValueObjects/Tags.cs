using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STG.Domain.ValueObjects;

public sealed class Tags
{
    private readonly HashSet<string> _values = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Values => _values;

    public Tags() { }
    public Tags(IEnumerable<string> tags)
    {
        foreach (var t in tags ?? Array.Empty<string>())
            Add(t);
    }

    public bool Add(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return _values.Add(tag.Trim());
    }

    public bool Contains(string tag) => _values.Contains(tag);
}
