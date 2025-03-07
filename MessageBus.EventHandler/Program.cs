using MessageBus.EventHandler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

try
{
    await Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.ConfigureServices(configuration);
        })
        .Build()
        .RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred while running the worker.");
    Console.WriteLine(ex.Message);
}