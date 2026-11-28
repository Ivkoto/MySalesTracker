using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MySalesTracker.Infrastructure.Extensions;

namespace MySalesTracker.Tests;

internal static class TestInfrastructureServices
{
    public static ServiceProvider Create(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DatabaseConnection"] = connectionString
            })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddInfrastructureServices(configuration, new TestHostEnvironment())
            .BuildServiceProvider();
    }

    sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "MySalesTracker.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
