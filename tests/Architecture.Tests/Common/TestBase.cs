using SmartTimetableGenerator.Application.Common.Interfaces;
using SmartTimetableGenerator.Domain.Common.Base;
using SmartTimetableGenerator.Infrastructure.Persistence;
using SmartTimetableGenerator.WebApi;
using System.Reflection;

namespace SmartTimetableGenerator.Architecture.UnitTests.Common;

public abstract class TestBase
{
    protected static readonly Assembly DomainAssembly = typeof(AggregateRoot<>).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(IApplicationDbContext).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(IWebApiMarker).Assembly;
}