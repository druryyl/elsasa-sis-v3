namespace Elsasa.Sis.IntegrationTests.UnitTests.Endpoints;

using System.Reflection;
using Elsasa.Sis.Shared.Endpoints;
using Elsasa.Sis.Students;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

public sealed class EndpointDiscoveryTests
{
    [Fact]
    public void MapEndpoints_Should_ThrowArgumentNullException_When_AppIsNull()
    {
        IEndpointRouteBuilder nullApp = null!;

        Assert.Throws<ArgumentNullException>(() => nullApp.MapEndpoints());
    }

    [Fact]
    public void MapEndpoints_Should_ThrowArgumentNullException_When_AssembliesIsNull()
    {
        var app = new DummyEndpointRouteBuilder();
        Assembly[] nullAssemblies = null!;

        Assert.Throws<ArgumentNullException>(() => app.MapEndpoints(nullAssemblies));
    }

    [Fact]
    public void MapEndpoints_Should_DiscoverAndInvokeEndpoint_When_AssemblyProvided()
    {
        var app = new DummyEndpointRouteBuilder();
        SampleDiscoveryEndpoint.MapEndpointCallCount = 0;

        app.MapEndpoints(typeof(SampleDiscoveryEndpoint).Assembly);

        Assert.True(SampleDiscoveryEndpoint.MapEndpointCallCount > 0);
    }

    [Fact]
    public void MapEndpoints_Should_NotInstantiateAbstractClassesOrNonEndpoints()
    {
        var app = new DummyEndpointRouteBuilder();
        AbstractSampleEndpoint.WasMapped = false;
        NonEndpointClass.WasInstantiated = false;

        app.MapEndpoints(typeof(EndpointDiscoveryTests).Assembly);

        Assert.False(AbstractSampleEndpoint.WasMapped);
        Assert.False(NonEndpointClass.WasInstantiated);
    }

    [Fact]
    public void MapEndpoints_Should_HandleEmptyAssembliesArray_WithoutThrowing()
    {
        var app = new DummyEndpointRouteBuilder();

        var returnedApp = app.MapEndpoints();

        Assert.Same(app, returnedApp);
    }

    [Fact]
    public void MapEndpoints_Should_DiscoverStudentsEndpoints_IncludingPlacementAndStatusEndpoints()
    {
        var services = new ServiceCollection();
        services.AddStudentsDomain();
        var app = new DummyEndpointRouteBuilder(services.BuildServiceProvider());

        app.MapEndpoints(typeof(Elsasa.Sis.Students.StudentsAssemblyMarker).Assembly);

        var endpoints = app.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/api/students/{id:guid}/history");
        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/api/students/{id:guid}/placements");
        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "/api/students/{id:guid}/status");
    }

    private sealed class DummyEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public DummyEndpointRouteBuilder(IServiceProvider? serviceProvider = null)
        {
            ServiceProvider = serviceProvider ?? new ServiceCollection().BuildServiceProvider();
        }

        public IServiceProvider ServiceProvider { get; }
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
        public IApplicationBuilder CreateApplicationBuilder() => throw new NotImplementedException();
    }
}

public sealed class SampleDiscoveryEndpoint : IEndpoint
{
    public static int MapEndpointCallCount { get; set; }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        MapEndpointCallCount++;
    }
}

public abstract class AbstractSampleEndpoint : IEndpoint
{
    public static bool WasMapped { get; set; }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        WasMapped = true;
    }
}

public class NonEndpointClass
{
    public static bool WasInstantiated { get; set; }

    public NonEndpointClass()
    {
        WasInstantiated = true;
    }
}
