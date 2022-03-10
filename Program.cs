namespace devanewbot
{
    using System.IO;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddCommandLine(args)
                    .Build()
                )
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    config
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile("appsettings.local.json", optional: true)
                        .AddEnvironmentVariables();
                })
                .UseStartup<Startup>();
    }
}
