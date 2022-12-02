namespace devanewbot.HostedServices;

using System;
using System.Threading;
using System.Threading.Tasks;
using Devanewbot.Discord;
using Microsoft.Extensions.Hosting;
using SlackDotNet;

public class SlackBotHostedService : IHostedService
{
    protected Client Client { get; }

    protected SlackSocket SlackSocket { get; }

    public SlackBotHostedService(Client client, SlackSocket slackSocket)
    {
        Client = client;
        SlackSocket = slackSocket;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SlackSocket.Connect();
        await Client.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}