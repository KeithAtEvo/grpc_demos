using Grpc.Net.Client;
using System.Net;

class ClientClientProgram
{
    static async Task Main(string[] args)
    {
        // The port number must match the port of the gRPC server.
        var channel = GrpcChannel.ForAddress("http://localhost:5295");
        Rpc.Generated.Service.ServiceClient clientService = new(channel);

        Rpc.Generated.Req request = new() { T = new Rpc.Generated.InternalType() { Grass = "growing" }, Field = "empty" };

        System.Console.WriteLine($"sending: '{request.T.Grass}', '{request.Field}'");

        Rpc.Generated.Resp response = await clientService.FuncAsync(request);

        System.Console.WriteLine($"Server answered: '{response.T.Grass}', '{response.Field}'");
        System.Console.ReadKey();
    }
    
}
