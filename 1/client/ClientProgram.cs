using Rpc.Reverse.Client;

class ClientClientProgram
{
    static async Task Main(string[] args)
    {
        // The port number must match the port of the gRPC server.
        var client = await Rpc.Reverse.Client.Comms.CreateAsync("http://localhost:5295");

        await client.ListenTask;

        System.Console.WriteLine("Returned from listen-loop. Press any key to exit.");
        System.Console.ReadKey();
    }
    
}
