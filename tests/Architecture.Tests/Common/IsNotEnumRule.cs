using Mono.Cecil;

namespace SmartTimetableGenerator.Architecture.UnitTests.Common;

public class IsNotEnumRule : ICustomRule
{
    public bool MeetsRule(TypeDefinition type) => !type.IsEnum;
}