using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RurouniJones.DCScribe.Core;
using RurouniJones.DCScribe.Grpc;
using RurouniJones.DCScribe.Postgres;
using RurouniJones.DCScribe.Shared.Interfaces;
using Serilog;

namespace RurouniJones.DCScribe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (OperatingSystem.IsWindows() && Environment.UserInteractive)
                ConsoleProperties.DisableQuickEdit();

            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                IConfigurationRoot configuration;
                try
                {
                    configuration = new ConfigurationBuilder()
                        .AddYamlFile("configuration.yaml", false, true)
                        .AddYamlFile("configuration.Development.yaml", true, true)
                        .Build();
                }
                catch (Exception ex)
                {
                    if (!Environment.UserInteractive) throw;
                    Console.WriteLine("Could not start DCScribe");
                    Console.WriteLine("Error reading \"configuration.yaml\" file");
                    Console.WriteLine(ex);
                    throw;
                }
                try
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(configuration)
                        .CreateLogger();

                    Log.Information("Starting DCScribe");
                    CreateHostBuilder(args, configuration).Build().Run();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Could not start DCScribe");
                    throw;
                }
                finally
                {
                    Log.Information("Stopping DCScribe");
                    Log.CloseAndFlush();
                }
            }
            catch
            {
                if (!Environment.UserInteractive) return;
                Console.ReadKey();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot config)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<ScribeFactory>();
                    services.AddTransient<Scribe>();
                    services.AddTransient<IRpcClient, RpcClient>();
                    services.AddTransient<IDatabaseClient, DatabaseClient>();
                    services.Configure<Configuration>(config);
                    services.AddOptions();
                })
                .UseSerilog()
                .UseWindowsService();
        }

        // https://stackoverflow.com/questions/13656846/how-to-programmatic-disable-c-sharp-console-applications-quick-edit-mode
        internal static class ConsoleProperties {

            // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
            private const int StdInputHandle = -10;

            private const uint QuickEdit = 0x0040;

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr GetStdHandle(int nStdHandle);

            [DllImport("kernel32.dll")]
            private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

            [DllImport("kernel32.dll")]
            private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

            internal static bool DisableQuickEdit() {

                var consoleHandle = GetStdHandle(StdInputHandle);

                GetConsoleMode(consoleHandle, out var consoleMode);

                consoleMode &= ~QuickEdit;

                return SetConsoleMode(consoleHandle, consoleMode);
            }
        }
    }
}
