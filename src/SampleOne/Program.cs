using StackExchange.Redis;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            //EndPoints = { "redis:6379" }
            EndPoints = { "localhost:6379" }
        });

// Setup
var channelName = "FirstChannelToStream";
var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
var subscriber = redis.GetSubscriber();



// Parallel
await subscriber.SubscribeAsync(channel,
    (c, v) => {
        Console.WriteLine($"parallel - {v}");
    }, CommandFlags.PreferMaster);


// Sequential
var channelQueue = await subscriber.SubscribeAsync(channel, CommandFlags.PreferMaster);
channelQueue.OnMessage(t =>
{
    Console.WriteLine($"queue - {t.Message}");
});



// Publisher
for (int i = 0; i < 10; i++)
{
    var value = new RedisValue($"My [{i}] message at: {DateTime.UtcNow:O}");
    var resultPost = await subscriber.PublishAsync(channel, value, CommandFlags.PreferReplica);
}
