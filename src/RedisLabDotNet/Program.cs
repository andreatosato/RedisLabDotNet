using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { "redis:6379" }
        })
    );
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/sendmessage", async (IConnectionMultiplexer redis) =>
{
    var key = new RedisKey("FirstKeySample");
    var db = redis.GetDatabase();
    var firstInsertResult = await db.StringSetAsync(new[] { KeyValuePair.Create(key, new RedisValue($"MyFirstInsertedMessage-{DateTime.UtcNow:O}")) });
    Console.WriteLine($"Inserted first key in DB result:{firstInsertResult}");

    var stringRead = await db.StringGetAsync(key, CommandFlags.PreferMaster);
    Console.WriteLine($"Read first key in DB result:{stringRead}");

    var channelName = "FirstChannelToStream";
    var channel = new RedisChannel(channelName, RedisChannel.PatternMode.Auto);
    var streamKey = new RedisKey($"{channelName}:FirstStreamKey");
    var value = new RedisValue($"My first message at: {DateTime.UtcNow:O}");
    
    //var redisStreamAddValue = await db.StreamAddAsync(streamKey, new RedisValue("Descriptions"), new RedisValue("MyFirstStreamValue"), flags: CommandFlags.PreferReplica);
    var subscriber = redis.GetSubscriber();
    var channelQueue = await subscriber.SubscribeAsync(channel, CommandFlags.PreferMaster);
    await subscriber.SubscribeAsync(channel,
        (c,v) => {
            Console.WriteLine(v.ToString());
        }, CommandFlags.PreferMaster);
    channelQueue.OnMessage(t =>
    {
        Console.WriteLine(t.Message);
    });

    var resultPost = await subscriber.PublishAsync(channel, value, CommandFlags.PreferReplica);
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}