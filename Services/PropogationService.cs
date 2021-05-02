namespace devanewbot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using SlackDotNet;
    using SlackDotNet.Payloads;

    public class PropogationService : IHostedService
    {
        public readonly Slack Slack;
        public readonly IConfiguration Configuration;

        public PropogationService(Slack slack, IConfiguration configuration)
        {
            Slack = slack;
            Configuration = configuration;
        }

        private const string Report = "http://www.hamqsl.com/solar101vhf.php";

        private const string ChannelId = "C3XHU6W9H";

        private Timer Timer;

        public void PostMessage(object state)
        {
            var message = new ChatMessage()
            {
                Channel = ChannelId,
                Text = "Propogation Report",
                Attachments = new List<ChatAttachment>
                {
                    new ChatAttachment
                    {
                        ImageUrl = Report
                    }
                }
            };

            Slack.PostMessage(message);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromHours(24);
            var nextRunTime = DateTime.Today.AddDays(0).AddHours(9);
            var curTime = DateTime.Now;
            var firstInterval = nextRunTime.Subtract(curTime);

            Action action = () =>
            {
                var t1 = Task.Delay(firstInterval);
                t1.Wait();
                Timer = new Timer(
                    PostMessage,
                    null,
                    TimeSpan.Zero,
                    interval
                );
            };

            Task.Run(action);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
    }
}