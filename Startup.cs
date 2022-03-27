namespace devanewbot;

using System.Threading.Tasks;
using devanewbot.Authorization;
using devanewbot.Services;
using Devanewbot.Discord;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlackDotNet;
using SlackDotNet.Options;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public Client Client { get; protected set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Slack>();
        services.AddSingleton<SlackSocket>();
        services.AddSingleton<SpongebobCommand>();
        services.AddSingleton<GifCommand>();
        services.AddSingleton<StallmanCommand>();
        services.AddSingleton<Client>();
        services.AddSingleton<ICommandService, CommandService>();
        services.Configure<DiscordOptions>(o => Configuration.GetSection("Discord").Bind(o));
        services.Configure<SlackSocketOptions>(o => Configuration.GetSection("SlackSocket").Bind(o));
        services.Configure<SlackOptions>(o => Configuration.GetSection("Slack").Bind(o));
        services.AddHangfire(config => config.UseRedisStorage(Configuration.GetConnectionString("Redis")));
    }

    [System.Obsolete]
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Client client, SlackSocket slackSocket)
    {
        // Start the Slack socket connection
        Task.Run(() => slackSocket.Connect());

        Client = client;
        Task.Run(() => client.Start());
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseHangfireServer();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DisplayStorageConnectionString = false
        });

        RecurringJob.AddOrUpdate<PropagationService>(p => p.PostMessage(), "0 9 * * *", System.TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
    }
}
