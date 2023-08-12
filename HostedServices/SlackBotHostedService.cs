namespace devanewbot.HostedServices;

using System.Threading;
using System.Threading.Tasks;
using Devanewbot.Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SlackBotHostedService : IHostedService
{
    protected Client Client { get; }

    protected ILogger<SlackBotHostedService> Logger { get; }

    public SlackBotHostedService(Client client, ILogger<SlackBotHostedService> logger)
    {
        Client = client;
        Logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Client.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}