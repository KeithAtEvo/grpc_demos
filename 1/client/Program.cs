using ReverseFuncs;
using Grpc.Net.Client;

// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("http://localhost:5295");
var client = new ReverseFunc.ReverseFuncClient(channel);
using var call = client.ReverseFuncProcessor();

var openCommsMsg = new ReverseFuncOutputPayload() { TheEcho = "hello" };

var funcInputStream = call.ResponseStream;
var funcOutputStream = call.RequestStream;

await funcOutputStream.WriteAsync(openCommsMsg);

var writeTasks = new HashSet<Task>();

while (await funcInputStream.MoveNext(CancellationToken.None))
{
    var funcInput = funcInputStream.Current;

    System.Console.WriteLine($"received reverse-func input: CallGuid: {funcInput.CallGuid}, arg: '{funcInput.ToEcho}'. Sending output...");

    var funcOutput = new ReverseFuncOutputPayload {CallGuid = funcInput.CallGuid, TheEcho = funcInput.ToEcho};

    writeTasks.Add(funcOutputStream.WriteAsync(funcOutput));
}

foreach (var task in writeTasks)
{
    await task;
}

await funcOutputStream.CompleteAsync();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
 
