public class ServerProgram
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddGrpc();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<Rpc.Straight.Service>();
        app.MapGrpcService<Rpc.Reverse.Service>();

        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        var serverTask = app.RunAsync();

        int nclients = 3;
        int ncalls = 4;

        while (Rpc.Reverse.Service.Clients.Count < nclients)
        {
            System.Console.WriteLine("waiting for clients to connect...");
            System.Threading.Thread.Sleep(1000);
        }

        bool doAsync = false;

        var allTasks = new HashSet<Task>();

        for (int i = 1; i <= ncalls; i++)
        {
            RpcServices.EchoFunc.Args echoArg = new($"arg{i}");
            System.Console.WriteLine("sending function calls to clients...");

            foreach ((var clientId, var comms) in Rpc.Reverse.Service.Clients)
            {
                var task = RpcServices.EchoFunc.Run(clientId, echoArg);
                if (doAsync)
                    allTasks.Add(task);
                else
                    await task;
            }
        }

        foreach (var task in allTasks)
        {
            await task;
        }

        await serverTask;
    }

}
