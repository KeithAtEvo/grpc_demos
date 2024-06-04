using ReverseFuncs;
using ReverseFuncs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ReverseFuncService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

var serverTask = app.RunAsync();

while (ReverseFuncService.knownClients.Count == 0)
{
    System.Console.WriteLine("waiting for clients to connect...");
    System.Threading.Thread.Sleep(1000);
}

var allTasks = new HashSet<Task>();

foreach(var command in new List<string>{"eat", "sleep", "work", "repeat" })
{
    System.Console.WriteLine("sending 'function calls' to clients...");

    foreach (var client in ReverseFuncService.knownClients)
    {
        Guid callGuid = Guid.NewGuid();
        System.Console.WriteLine($"sending '{command}' with callGuid {callGuid} to client {client.GetHashCode()}");
        allTasks.Add(client.WriteAsync(new ReverseFuncInputPayload { CallGuid = callGuid.ToString(), ToEcho = command }));
    }
}

foreach (var task in allTasks)
{
    await task;
}

await serverTask;
