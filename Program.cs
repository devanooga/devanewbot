using System;
using System.IO;
using System.Linq;
using AspNetCore.ReCaptcha;
using devanewbot.Authorization;
using devanewbot.Entities;
using devanewbot.HostedServices;
using devanewbot.Services;
using devanewbot.SlackDotNet.Options;
using Devanewbot.Discord;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RollbarDotNet.Configuration;
using RollbarDotNet.Core;
using RollbarDotNet.Logger;
using SlackDotNet;
using SlackNet.AspNetCore;
using SlackNet.Blocks;
using SlackNet.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var configuration = builder.Configuration;

var suffix = configuration.GetSection("SlackSocket").GetValue<string>("CommandSuffix");

builder.Services
    .AddDbContext<DevanewbotContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DevanewbotContext")))
    .AddHttpClient()
    .AddSingleton<Slack>()
    .AddSingleton<Client>()
    .AddTransient<InviteService>()
    .AddTransient<IChannelBanService, ChannelBanService>()
    .AddTransient<IWelcomeService, WelcomeService>()
    .AddSlackNet(c =>
    {
        c
            .UseSigningSecret(configuration.GetSection("Slack").GetValue<string>("VerificationToken")!)
            .UseApiToken(configuration.GetSection("Slack").GetValue<string>("OauthToken"))
            .UseAppLevelToken(configuration.GetSection("SlackSocket").GetValue<string>("AppToken"));

        c
            .RegisterSlashCommandHandler<SpongebobCommand>("/spongebob" + suffix)
            .RegisterSlashCommandHandler<GifCommand>("/jif")
            .RegisterBlockActionHandler<ButtonAction, GifCommand>("post")
            .RegisterBlockActionHandler<ButtonAction, GifCommand>("random")
            .RegisterBlockActionHandler<ButtonAction, GifCommand>("cancel")
            .RegisterBlockActionHandler<ButtonAction, InviteService>("approve_invite")
            .RegisterBlockActionHandler<ButtonAction, InviteService>("decline_invite")
            .RegisterSlashCommandHandler<StallmanCommand>("/stallman" + suffix)
            .RegisterSlashCommandHandler<ChannelBanCommand>("/channel-ban" + suffix)
            .RegisterSlashCommandHandler<RemoveBanCommand>("/remove-channel-ban" + suffix)
            .RegisterViewSubmissionHandler<ChannelBanModalHandler>(ChannelBanModalHandler.ModalCallbackId)
            .RegisterViewSubmissionHandler<RemoveBanModalHandler>(RemoveBanModalHandler.ModalCallbackId)
            .RegisterEventHandler<MemberJoinedChannel, MemberJoinedChannelHandler>();

        // "No!" says the man in Github, "you should port the code"
        //  I choose the lazy solution, I choose... this.
        Directory.EnumerateFiles("qrmbot/lib")
            .Where(f => !f.EndsWith(".csv") && !f.EndsWith("pm"))
            .Select(f => f.Split("/").Last().Replace(".pl", string.Empty))
            .ToList()
            .ForEach(f => c.RegisterSlashCommandHandler<QrmBotCommand>($"/{f}"));
    })
    .Configure<RollbarOptions>(options => configuration.GetSection("Rollbar").Bind(options))
    .Configure<DiscordOptions>(o => configuration.GetSection("Discord").Bind(o))
    .Configure<SlackOptions>(o => configuration.GetSection("Slack").Bind(o))
    .Configure<ForwardedHeadersOptions>(options =>
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 3; // CDN + Load balancer
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

{
    // Database Migrations
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DevanewbotContext>();
    db.Database.Migrate();
}

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
loggerFactory.AddRollbarDotNetLogger(app.Services);
app
    .UseSlackNet(c =>
        c.UseSocketMode(true)
    )
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

// This is literally the worst thing I've done with this project, even worse than just running perl inline
//  YES THIS WILL FUCK WITH YOUR HOME DIRECTORY BLAME QRMBOT
//  That or just don't fill out QrmBot:Settings
foreach (var setting in configuration.GetSection("QrmBot:Settings").GetChildren())
{
    var configFile = $".{setting.Key.ToLower()}";
    var content = setting.GetChildren()
        .Select(c => $"${c.Key} = \"{c.Value}\";")
        .Aggregate((c1, c2) => $"{c1}\n{c2}");
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    await File.WriteAllTextAsync($"{homeDir}/{configFile}", content);
}

await app.RunAsync();
