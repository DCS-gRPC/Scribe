using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RurouniJones.DCScribe.Core;
using Serilog;

namespace RurouniJones.DCScribe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var configuration = new ConfigurationBuilder()
                .AddYamlFile("configuration.yaml", false, true)
                .AddYamlFile("configuration.Development.yaml", true, true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            try
            {
                Log.Information("Starting DCScribe");
                CreateHostBuilder(args, configuration).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Could not start DCScribe");
            }
            finally
            {
                Log.Information("Stopping DCScribe");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot config)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<ScribeFactory>();
                    services.AddTransient<Scribe>();
                    services.Configure<Configuration>(config);
                    services.AddOptions();
                })
                .UseSerilog()
                .UseWindowsService();
        }
    }
}
