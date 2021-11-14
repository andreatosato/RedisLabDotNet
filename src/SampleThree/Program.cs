using StackExchange.Redis;
using System.Text.Json;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            //EndPoints = { "redis:6379" }
            EndPoints = { "localhost:6379" }
        });

// Setup
var db = redis.GetDatabase();
var channelName = "FirstChannelToStream";
var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
var redisKeyA = "KeyA";
var redisGroupNameA = "ConsumerGroupA";

try
{
    var infos = await db.StreamConsumerInfoAsync(redisKeyA, redisGroupNameA);
    foreach (var iConsumer in infos)
    {
        Console.WriteLine(JsonSerializer.Serialize(iConsumer));
    }
}
catch (Exception)
{
    var createdSuccessfullyA = await db.StreamCreateConsumerGroupAsync(redisKeyA, redisGroupNameA, StreamPosition.Beginning, CommandFlags.PreferReplica);
}

CancellationTokenSource cts = new CancellationTokenSource();
CancellationToken tokenToDispose = cts.Token;
Task listernerOneGroupA = Task.Run(async () =>
{
    while (true)
    {
        var entries = await db.StreamReadGroupAsync(redisKeyA, redisGroupNameA, "ConsumerA", noAck: false, flags: CommandFlags.PreferReplica);
        foreach (var e in entries)
        {
            foreach (var v in e.Values)
            {
                Console.WriteLine($"{e.Id} - ConsumerA - {v.Name}:{v.Value}");
            }
        }
        var entriesInPending = await db.StreamPendingMessagesAsync(redisKeyA, redisGroupNameA, 10, "ConsumerA");
        foreach (var e in entriesInPending)
        {
            var entriesNoAck = await db.StreamReadGroupAsync(redisKeyA, redisGroupNameA, "ConsumerA", e.MessageId, 10, CommandFlags.None);
            foreach (var eNoAck in entriesNoAck)
            {
                foreach (var v in eNoAck.Values)
                {
                    Console.WriteLine($"{eNoAck.Id} - ConsumerA NoAck - {v.Name}:{v.Value}");
                    await db.StreamAcknowledgeAsync(redisKeyA, redisGroupNameA, eNoAck.Id, CommandFlags.PreferReplica);
                    await db.StreamDeleteAsync(redisKeyA, new[] { eNoAck.Id });
                }
            }
        }
    }
}, tokenToDispose);

Task listernerTwoGroupA = Task.Run(async () =>
{
    while (true)
    {
        var entries = await db.StreamReadGroupAsync(redisKeyA, redisGroupNameA, "ConsumerB", noAck: true, flags: CommandFlags.PreferReplica);
        foreach (var e in entries)
        {
            foreach (var v in e.Values)
            {
                Console.WriteLine($"{e.Id} - ConsumerB - {v.Name}:{v.Value}");
                await db.StreamAcknowledgeAsync(redisKeyA, redisGroupNameA, e.Id, CommandFlags.PreferReplica);
                await db.StreamDeleteAsync(redisKeyA, new[] { e.Id });
            }
        }
    }
}, tokenToDispose);

Task listernerTwoGroupC = Task.Run(async () =>
{
    while (true)
    {
        var entries = await db.StreamReadGroupAsync(redisKeyA, redisGroupNameA, "ConsumerC", noAck: false, flags: CommandFlags.PreferReplica);
        foreach (var e in entries)
        {
            foreach (var v in e.Values)
            {
                Console.WriteLine($"{e.Id} - ConsumerC - {v.Name}:{v.Value}");
                await db.StreamAcknowledgeAsync(redisKeyA, redisGroupNameA, e.Id, CommandFlags.PreferReplica);
            }
        }
    }
}, tokenToDispose);



// Publisher
//for (int i = 0; i < 10; i++)
//{
//    await db.StreamAddAsync(redisKeyA,
//        new NameValueEntry[] {
//            new NameValueEntry("message", $"I'm [{i}] message in group A"),
//            new NameValueEntry("SecondsInTime", DateTime.UtcNow.Second)
//        });
//}


Console.Read();
cts.Cancel();
await Task.Delay(1000);
