using StackExchange.Redis;

IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" }
        });
var db = redis.GetDatabase();
var key = "MultiClient";
var fieldMessage = "Message";
var fieldRandomTemperature = "Message";
var randomizer = new Random();
int clientsNumber = 2000;
int serversNumber = 100;
Thread[] clients = new Thread[clientsNumber];
Thread[] servers = new Thread[serversNumber];

for (int i = 0; i < serversNumber; i++)
    servers[i] = new Thread(async () => await SimulateServer());

for (int i = 0; i < clientsNumber; i++)
    clients[i] = new Thread(async () => await SimulateClient());


for (int j = 0; j < serversNumber; j++)
    servers[j].Start();
for (int k = 0; k < serversNumber; k++)
    servers[k].Join();

// Start all threads, passing to each thread its app domain.
for (int j = 0; j < clientsNumber; j++)
    clients[j].Start();
// Wait for the threads to finish.
for (int k = 0; k < clientsNumber; k++)
    clients[k].Join();



async Task SimulateClient()
{
    var dbThreadConnection = redis.GetDatabase();
    int i = 0;
    while(true)
    {   
        await dbThreadConnection.StreamAddAsync(key, new NameValueEntry[] {
            new NameValueEntry(fieldMessage, $"{i} - Thread: {Thread.CurrentThread.ManagedThreadId}"),
            new NameValueEntry(fieldRandomTemperature, randomizer.NextDouble())
        });
        i++;
        await Task.Delay(100);
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
