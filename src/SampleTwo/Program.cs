using StackExchange.Redis;
using System.Management;
using System.Text.Json;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            //EndPoints = { "redis:6379" }
            EndPoints = { "localhost:6379" }
        });

// Setup
var db = redis.GetDatabase();
var channelName = "SecondChannelToStream";
var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
var redisKeyA = "Key3Stream";

// Listener
CancellationTokenSource cts = new CancellationTokenSource();
CancellationToken tokenToDispose = cts.Token;
Task listernerOne = Task.Run(async () =>
{
    while (true)
    {
        var entries = await db.StreamReadAsync(redisKeyA, 0, flags: CommandFlags.PreferReplica);
        foreach (var e in entries)
        {
            foreach (var v in e.Values)
            {
                Console.WriteLine($"{e.Id} - ConsumerOne - {v.Name}:{v.Value}");
                await db.StreamDeleteAsync(redisKeyA, new[] { e.Id });
            }            
        }
    }
}, tokenToDispose);

Task listernerTwo = Task.Run(async () =>
{
    while (true)
    {
        var entries = await db.StreamReadAsync(redisKeyA, 0, flags: CommandFlags.PreferReplica);
        foreach (var e in entries)
        {
            foreach (var v in e.Values)
            {
                Console.WriteLine($"{e.Id} - ConsumerTwo - {v.Name}:{v.Value}");
                await db.StreamDeleteAsync(redisKeyA, new[] { e.Id });
            }
        }
    }
}, tokenToDispose);




// Publisher
for (int i = 0; i < 5; i++)
{
    await db.StreamAddAsync(redisKeyA,
        new NameValueEntry[] {
            new NameValueEntry("message", $"I'm [{i}] message in group A"),
            new NameValueEntry("SecondsInTime", DateTime.UtcNow.Second)
        });
}

Console.Read();
cts.Cancel();
await Task.Delay(1000);
