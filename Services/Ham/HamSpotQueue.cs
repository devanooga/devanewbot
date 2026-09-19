namespace devanewbot.Services.Ham;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using devanewbot.Models;

public class HamSpotQueue
{
    private readonly Channel<ReceivedSpot> channel = Channel.CreateBounded<ReceivedSpot>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });

    public ValueTask Enqueue(ReceivedSpot spot, CancellationToken cancellationToken) =>
        channel.Writer.WriteAsync(spot, cancellationToken);

    public ChannelReader<ReceivedSpot> Reader => channel.Reader;
}
