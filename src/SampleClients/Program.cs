using StackExchange.Redis;
using System.Security.Cryptography;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" }
        });
var db = redis.GetDatabase();
var key = "MultiClient";
var fieldMessage = "Message";
var fieldRandomTemperature = "Message";
int clientsNumber = 100;
int serversNumber = 10;

for (int i = 0; i < serversNumber; i++)
    new Thread(async () => await SimulateServer()).Start();

for (int i = 0; i < clientsNumber; i++)
    new Thread(async () => await SimulateClient()).Start();

async Task SimulateClient()
{
    var dbThreadConnection = redis.GetDatabase();
    int i = 0;
    while(true)
    {   
        await dbThreadConnection.StreamAddAsync(key, new NameValueEntry[] {
            new NameValueEntry(fieldMessage, $"{i} - Thread: {Thread.CurrentThread.ManagedThreadId}"),
            new NameValueEntry(fieldRandomTemperature, RandomNumberGenerator.GetInt32(-10, 40))
        });
        i++;

    }
}


async Task SimulateServer()
{
    RedisValue position = 0;
    while (true)
    {
        var entries = await db.StreamReadAsync(key, position);
        for (int i = 0; i < entries.Length; i++)
        {
            foreach (var v in entries[i].Values)
            {
                Console.WriteLine($"Read [{entries[i].Id}]: {v.Name} - {v.Value}");
            }
        }
        var idS = entries.Select(t => t.Id);
        if (idS.Any())
        {
            position = idS.OrderBy(t => t).FirstOrDefault();
            await db.StreamDeleteAsync(key, entries.Select(t => t.Id).ToArray());
        }
        //await Task.Delay(50);
    }    
}


Console.WriteLine(".....Startup Completato....");
Console.Read();
