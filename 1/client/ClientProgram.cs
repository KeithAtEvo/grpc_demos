using RpcGenerated;
using Grpc.Net.Client;
using System.Collections.Specialized;


using BundleOfReverseCalls =
(
    //Here we declare "listeners" for all our "reverse functions".
    Grpc.Core.AsyncDuplexStreamingCall<RpcGenerated.EchoOutput, RpcGenerated.EchoInput> echo,
    //etc...
    int dummyItemBecauseTuplesMustHaveMoreThanOneItem
);
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Reflection.Metadata;


class RpcFuncs : IDisposable
{
    public RpcGenerated.Straight.StraightClient ClientFuncs{ get; private init; }

    RpcClientListeners ReverseFuncListeners{ get; init; }

    public Task ListenTask{ get; private init;}
    
    private RpcFuncs(
        RpcGenerated.Straight.StraightClient clientFuncs,
        RpcClientListeners reverseFuncListeners,
        Task listenTask
    )
    {
        ClientFuncs = clientFuncs;
        ReverseFuncListeners = reverseFuncListeners;
        ListenTask = listenTask;
    }

    ~RpcFuncs()
    {
        Dispose(false);
    }

    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                if
                (
                    !ClientFuncs.DeregisterClient(
                        new DeregisterClientRequest() { BestowedId = ReverseFuncListeners.Id }
                    ).Ok
                )
                    System.Console.WriteLine("server complained when we deregistered"); ;

                // Dispose managed resources.
                ListenTask.Dispose();
            }

            // Clean up unmanaged resources.

            disposed = true;

        }
    }

    public static async Task<RpcFuncs> CreateAsync(string address)
    {
        var channel = GrpcChannel.ForAddress(address);
        RpcGenerated.Straight.StraightClient clientFuncs = new (channel);
        var clientListeners =
            await RpcClientListeners.CreateAsync(
                channel,
                await DoHandshakeAsync(clientFuncs));
        Task listenTask = clientListeners.DoListenLoop();

        RpcFuncs obj =
         new (
            clientFuncs,
            clientListeners,
            listenTask);

        return obj;
    }

    private static async Task<string> DoHandshakeAsync(RpcGenerated.Straight.StraightClient clientFuncs)
    {
        System.Console.WriteLine("Beginning handshake...");

        string id = (await clientFuncs.RegisterClientAsync(new RegisterClientRequest() { Dummy = false })).BestowedId;

        System.Console.WriteLine($"...registered ourselves with server, received id '{id}'...");

        return id;
    }

}

class RpcClientListeners
{
    public string Id { get; init; }

    BundleOfReverseCalls _calls;

    private RpcClientListeners(
        string id,
        //Here we pass in all the pre-initialized calls;
        BundleOfReverseCalls calls
    )
    {
        Id = id;
        _calls = calls;
    }

    public static async Task<RpcClientListeners> CreateAsync(
        GrpcChannel channel,
        string id
    )
    {
        //Here we call all our two-way streaming functions, each of which will support
        // one of our "reverse" functions.

        RpcClientListeners obj =
            new (
                id,
                await DoHandshakeAsync(
                    id,
                    new RpcGenerated.Reverse.ReverseClient(channel))
            );

        return obj;
    }

    private static async Task<Grpc.Core.AsyncDuplexStreamingCall<EchoOutput, EchoInput>>
    DoEchoHandshakeAsync(
        string id,
        RpcGenerated.Reverse.ReverseClient reverseFuncClient
    )
    {
        var echoCall = reverseFuncClient.Echo();

        var inputStream = echoCall.ResponseStream;
        var outputStream = echoCall.RequestStream;

        //During this initial handshake, we repurpose the CallGuid field to hold our client id.
        EchoOutput clientHandshake = new() { CallGuid = id, TheEcho = "hello" };
        await outputStream.WriteAsync(clientHandshake);
        await inputStream.MoveNext(CancellationToken.None);
        var serverHandshake = inputStream.Current;

        if(serverHandshake.ToEcho == clientHandshake.TheEcho && serverHandshake.CallGuid == clientHandshake.CallGuid)
        {
            System.Console.WriteLine("Handshake successful.");
        }
        else
        {
            System.Console.WriteLine($"Handshake failed.");
        }

        return echoCall;
    }

    // etc ...

    private static async Task<BundleOfReverseCalls>
    DoHandshakeAsync(
        string id,
        RpcGenerated.Reverse.ReverseClient reverseFuncClient
    )
    {
        return
            new BundleOfReverseCalls{
                echo =
                    await DoEchoHandshakeAsync(
                        id,
                        reverseFuncClient
                    ),
            //etc...
            }
        ;
    }

    public async Task DoEchoListenLoop()
    {
        var echoInputStream = _calls.echo.ResponseStream;
        var echoOutputStream = _calls.echo.RequestStream;

        bool doAsync = false;

        var writeTasks = new HashSet<Task>();

        while (await echoInputStream.MoveNext(CancellationToken.None))
        {
            var funcInput = echoInputStream.Current;

            System.Console.WriteLine($"received reverse-func input: CallGuid: {funcInput.CallGuid}, arg: '{funcInput.ToEcho}'. Sending output...");

            var funcOutput = new EchoOutput {CallGuid = funcInput.CallGuid, TheEcho = funcInput.ToEcho};

            System.Console.WriteLine($"responding with reverse-func output: CallGuid: {funcInput.CallGuid}, arg: '{funcInput.ToEcho}'. Sending output...");

            var task = echoOutputStream.WriteAsync(funcOutput);
            if(doAsync)
                writeTasks.Add(task);
            else
                await task;
        }

        foreach (var task in writeTasks)
        {
            await task;
        }

        await echoOutputStream.CompleteAsync();
    }

    public async Task DoListenLoop()
    {
        List<Task> listenTasks = new();

        listenTasks.Add(DoEchoListenLoop());
        //etc...

        await Task.WhenAll(listenTasks);
    }

}

class ClientClientProgram
{
    static async Task Main(string[] args)
    {
        // The port number must match the port of the gRPC server.
        var client = await RpcFuncs.CreateAsync("http://localhost:5295");

        await client.ListenTask;

        System.Console.WriteLine("Returned from listen-loop. Press any key to exit.");
        System.Console.ReadKey();
    }
    
}
