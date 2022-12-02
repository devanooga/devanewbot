namespace devanewbot.HostedServices;

using System.Threading;
using System.Threading.Tasks;
using devanewbot.Services;
using Hangfire;
using Microsoft.Extensions.Hosting;

public class HangfireHostedService : IHostedService
{
    protected IRecurringJobManager RecurringJobManager { get; }


    public HangfireHostedService(IRecurringJobManager recurringJobManager)
    {
        RecurringJobManager = recurringJobManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RecurringJobManager.AddOrUpdate<PropagationService>("Ham Propagation", p => p.PostMessage(), "0 9 * * *", System.TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}