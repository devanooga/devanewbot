using System;
using System.IO;
using AspNetCore.ReCaptcha;
using devanewbot.Authorization;
using devanewbot.HostedServices;
using devanewbot.Services;
using Devanewbot.Discord;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RollbarDotNet.Configuration;
using RollbarDotNet.Core;
using RollbarDotNet.Logger;
using SlackDotNet;
using SlackDotNet.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var configuration = builder.Configuration;

builder.Services
    .AddSingleton<Slack>()
    .AddSingleton<SlackSocket>()
    .AddSingleton<SpongebobCommand>()
    .AddSingleton<GifCommand>()
    .AddSingleton<StallmanCommand>()
    .AddSingleton<Client>()
    .AddSingleton<ICommandService, CommandService>()
    .Configure<RollbarOptions>(options => configuration.GetSection("Rollbar").Bind(options))
    .Configure<DiscordOptions>(o => configuration.GetSection("Discord").Bind(o))
    .Configure<SlackSocketOptions>(o => configuration.GetSection("SlackSocket").Bind(o))
    .Configure<SlackOptions>(o => configuration.GetSection("Slack").Bind(o))
    .Configure<ForwardedHeadersOptions>(options =>
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    })
    .AddRollbarWeb()
    .AddHangfire(config => config.UseRedisStorage(configuration.GetConnectionString("Redis")))
    .AddHangfireServer()
    .AddAuthorization()
    .AddAuthentication()
    .Services
    .AddReCaptcha(configuration.GetSection("ReCaptcha"))
    .AddControllers()
    .Services
    .AddHostedService<SlackBotHostedService>()
    .AddHostedService<HangfireHostedService>();

var app = builder.Build();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
loggerFactory.AddRollbarDotNetLogger(app.Services);
app
    .UseForwardedHeaders()
    .UseHsts()
    .UseHttpsRedirection()
    .UseStaticFiles(new StaticFileOptions
    {
        HttpsCompression = Microsoft.AspNetCore.Http.Features.HttpsCompressionMode.DoNotCompress,
        OnPrepareResponse = (context) =>
        {
            if (context.File.Name.EndsWith(".json") ||
                context.File.Name.EndsWith(".html"))
            {
                context.Context.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(0)
                };
            }
        }
    })
    .UseRollbarExceptionHandler()
    .UseAuthentication()
    .UseAuthorization()
    .UseRouting()
    .UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DisplayStorageConnectionString = false
    })
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapFallbackToFile("index.html");
    });

await app.RunAsync();