using StackExchange.Redis;
using System.Security.Cryptography;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" }
        });
var db = redis.GetDatabase();
var key = "MultiClientParallel";
int serversNumber = 200;

Parallel.For(0, serversNumber, new ParallelOptions { MaxDegreeOfParallelism = serversNumber }, async (i) => await SimulateServer(i));

async Task SimulateServer(int number)
{
    RedisValue position = 0;
    while (true)
    {
        var entries = await db.StreamReadAsync(key, StreamPosition.Beginning);
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
    }
}


Console.WriteLine(".....Startup Completato....");
Console.Read();
