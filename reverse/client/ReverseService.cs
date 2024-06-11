using Grpc.Net.Client;


using BundleOfReverseCalls =
(
    //Here we declare "listeners" for all our "reverse functions".
    Grpc.Core.AsyncDuplexStreamingCall<Rpc.Reverse.Generated.EchoOutput, Rpc.Reverse.Generated.EchoInput> echo,
    //etc...
    int dummyItemBecauseTuplesMustHaveMoreThanOneItem
);

namespace Rpc.Reverse.Client;

class Comms : IDisposable
{
    public Straight.Generated.Service.ServiceClient ClientService { get; private init; }

    Listeners ReverseFuncListeners { get; init; }

    public Task ListenTask { get; private init; }

    private Comms(
        Rpc.Straight.Generated.Service.ServiceClient clientService,
        Listeners reverseFuncListeners,
        Task listenTask
    )
    {
        ClientService = clientService;
        ReverseFuncListeners = reverseFuncListeners;
        ListenTask = listenTask;
    }

    ~Comms()
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
                    !ClientService.DeregisterClient(
                        new Straight.Generated.DeregisterClientRequest() { BestowedId = ReverseFuncListeners.Id }
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

    public static async Task<Comms> CreateAsync(string address)
    {
        var channel = GrpcChannel.ForAddress(address);
        Rpc.Straight.Generated.Service.ServiceClient clientService = new(channel);
        var clientListeners =
            await Listeners.CreateAsync(
                channel,
                await DoHandshakeAsync(clientService));
        Task listenTask = clientListeners.DoListenLoop();

        Comms obj =
            new(
            clientService,
            clientListeners,
            listenTask);

        return obj;
    }

    private static async Task<string> DoHandshakeAsync(Straight.Generated.Service.ServiceClient clientService)
    {
        System.Console.WriteLine("Beginning handshake...");

        string id = (await clientService.RegisterClientAsync(new Straight.Generated.RegisterClientRequest() { Dummy = false })).BestowedId;

        System.Console.WriteLine($"...registered ourselves with server, received id '{id}'...");

        return id;
    }

}

class Listeners
{
    public string Id { get; init; }

    BundleOfReverseCalls _calls;

    private Listeners(
        string id,
        //Here we pass in all the pre-initialized calls;
        BundleOfReverseCalls calls
    )
    {
        Id = id;
        _calls = calls;
    }

    public static async Task<Listeners> CreateAsync(
        GrpcChannel channel,
        string id
    )
    {
        //Here we call all our two-way streaming functions, each of which will support
        // one of our "reverse" functions.

        Listeners obj =
            new(
                id,
                await DoHandshakeAsync(
                    id,
                    new Rpc.Reverse.Generated.Service.ServiceClient(channel))
            );

        return obj;
    }

    private static async Task<Grpc.Core.AsyncDuplexStreamingCall<Rpc.Reverse.Generated.EchoOutput, Rpc.Reverse.Generated.EchoInput>>
    DoEchoHandshakeAsync(
        string id,
        Rpc.Reverse.Generated.Service.ServiceClient reverseFuncClient
    )
    {
        var echoCall = reverseFuncClient.Echo();

        var inputStream = echoCall.ResponseStream;
        var outputStream = echoCall.RequestStream;

        //During this initial handshake, we repurpose the CallGuid field to hold our client id.
        Rpc.Reverse.Generated.EchoOutput clientHandshake = new() { CallGuid = id, TheEcho = "hello" };
        await outputStream.WriteAsync(clientHandshake);
        await inputStream.MoveNext(CancellationToken.None);
        var serverHandshake = inputStream.Current;

        if (serverHandshake.ToEcho == clientHandshake.TheEcho && serverHandshake.CallGuid == clientHandshake.CallGuid)
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
        Rpc.Reverse.Generated.Service.ServiceClient reverseFuncClient
    )
    {
        return
            new BundleOfReverseCalls
            {
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

            var funcOutput = new Rpc.Reverse.Generated.EchoOutput { CallGuid = funcInput.CallGuid, TheEcho = funcInput.ToEcho };

            System.Console.WriteLine($"responding with reverse-func output: CallGuid: {funcInput.CallGuid}, arg: '{funcInput.ToEcho}'. Sending output...");

            var task = echoOutputStream.WriteAsync(funcOutput);
            if (doAsync)
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
