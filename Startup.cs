using devanewbot.Authorization;
using devanewbot.Services;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlackDotNet;

namespace devanewbot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            var slackTaskClient = new Slack(
                Configuration["Slack:OauthToken"],
                Configuration["Slack:SigningSecret"],
                Configuration["Slack:VerificationToken"]);

            services.AddSingleton<Slack>(slackTaskClient);
            services.AddSingleton<SpongebobCommand>();
            services.AddSingleton<GifCommand>();
            services.AddSingleton<StallmanCommand>();
            services.AddHangfire(config => config.UseRedisStorage(Configuration.GetConnectionString("Redis")));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            RecurringJob.AddOrUpdate<PropagationService>(p => p.PostMessage(), "0 9 * * *", System.TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        }
    }
}
