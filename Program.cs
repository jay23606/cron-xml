using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace cron;
public class Program
{
    public static async Task Main(string[] args) { await CreateHostBuilder(args).RunConsoleAsync(); }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => { services.AddHostedService<CronService>(); });
}