namespace devanewbot.HostedServices;

using System.Threading;
using System.Threading.Tasks;
using devanewbot.Services;
using Hangfire;
using Microsoft.Extensions.Hosting;

public class HangfireHostedService(IRecurringJobManager recurringJobManager) : IHostedService
{
    private readonly RecurringJobOptions RecurringJobOptions = new RecurringJobOptions
    {
        TimeZone = System.TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobManager.AddOrUpdate<PropagationService>(
            "Ham Propagation",
            p => p.PostMessage(),
            "0 9 * * *",
            RecurringJobOptions);

        recurringJobManager.AddOrUpdate<IChannelBanService>(
            "Check Ban Expirations",
            cbs => cbs.CheckExpirations(),
            Cron.Hourly,
            RecurringJobOptions);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
