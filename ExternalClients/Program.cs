using StackExchange.Redis;
using System.Security.Cryptography;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { "20.203.217.149:6379" }
        });

Console.WriteLine("Connected");
var db = redis.GetDatabase();
var key = "MultiClientParallel";
var fieldMessage = "Message";
var fieldRandomTemperature = "Temperature";

Console.WriteLine("Quanti clients?");
int clientsNumber = int.Parse(Console.ReadLine()!);

Parallel.For(0, clientsNumber, new ParallelOptions { MaxDegreeOfParallelism = clientsNumber }, async (i) => await SimulateClient(i));

async Task SimulateClient(int number)
{
    var dbThreadConnection = redis.GetDatabase(asyncState: number);
    int i = 0;
    while (true)
    {
        await dbThreadConnection.StreamAddAsync(key, new NameValueEntry[] {
            new NameValueEntry(fieldMessage, $"{i} - Thread: {Thread.CurrentThread.ManagedThreadId}"),
            new NameValueEntry(fieldRandomTemperature, RandomNumberGenerator.GetInt32(-10, 40))
        });
        Console.WriteLine($"Add {i} - Thread: {Thread.CurrentThread.ManagedThreadId}");
        i++;
        await Task.Delay(RandomNumberGenerator.GetInt32(0, 1000));
    }
}

Console.WriteLine(".....Startup Completato....");
Console.Read();
