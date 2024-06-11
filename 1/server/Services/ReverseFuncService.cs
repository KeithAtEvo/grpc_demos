using System.ComponentModel;
using System.Diagnostics.Metrics;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RpcGenerated;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RpcServices;

public class ReverseFuncService : RpcGenerated.Reverse.ReverseBase
{
    private readonly ILogger<ReverseFuncService> _logger;
    public ReverseFuncService(ILogger<ReverseFuncService> logger) {
        _logger = logger;
    }

    public static ConcurrentDictionary<string, EchoClientComms> clients = new();

    public class EchoClientComms
    {
        public IServerStreamWriter<RpcGenerated.EchoInput> InputStream { get; private init; }
        public IAsyncStreamReader<RpcGenerated.EchoOutput> OutputStream { get; private init; }

        //calls to this client
        public ConcurrentDictionary<string, TaskCompletionSource<RpcGenerated.EchoOutput>> OpenCalls { get; private init; }

        public EchoClientComms(
            IServerStreamWriter<RpcGenerated.EchoInput> inputStream,
            IAsyncStreamReader<RpcGenerated.EchoOutput> outputStream
        )
        {
            InputStream = inputStream;
            OutputStream = outputStream;

            OpenCalls = new();
        }
    };

    async Task<string> DoEchoHandshake(
        EchoClientComms comms)
    {
        if (!await comms.OutputStream.MoveNext())
        {
            string errStr = "unkown client of Echo disconnected before registering itself";
            System.Console.WriteLine(errStr);
            throw new System.Exception(errStr);
        }

        EchoOutput clientHandshake = comms.OutputStream.Current;

        //During this initial handshake, we repurpose the CallGuid field to hold our client id.
        var clientId = clientHandshake.CallGuid;

        if (!global::Comms.clients.ContainsKey(clientId))
        {
            string errStr =
                "Expected handshake from known client, but received either a non-handshake"
                + " or a handshake from an unknown client."
                + " Closing this connection...";
            System.Console.WriteLine(errStr);
            throw new System.Exception(errStr);
        }
        if (!clients.TryAdd(clientId, comms))
        {
            string errStr =
                "client was already registered for this reverse-func."
                + " Closing this connection...";
            System.Console.WriteLine(errStr);
            throw new System.Exception(errStr);
        }

        await comms.InputStream.WriteAsync(
            new RpcGenerated.EchoInput()
            {
                ToEcho = "hello",
                CallGuid = clientHandshake.CallGuid
            });

        System.Console.WriteLine($"client {clientId} subscribed to our Echo reverse-func service.");

        return clientId;
    }

    public override async Task
        Echo(
            IAsyncStreamReader<RpcGenerated.EchoOutput> funcOutputStreamIn,
            IServerStreamWriter<RpcGenerated.EchoInput> funcInputStreamOut,
            ServerCallContext context)
    {
        EchoClientComms clientComms = new(funcInputStreamOut, funcOutputStreamIn);

        //Initial "output" message from client (with no input) does the job of:
        //- Iniitalizing connectivity for this function for this client..
        //- Registering this client as supporting this function.
        string clientId = await DoEchoHandshake(clientComms);

        while (await funcOutputStreamIn.MoveNext())
        {
            var funcOutput = funcOutputStreamIn.Current;

            bool openCallExists =
                clientComms.OpenCalls.TryGetValue(
                    funcOutput.CallGuid,
                    out TaskCompletionSource<RpcGenerated.EchoOutput>? promise);
            if(!openCallExists)
            {
                System.Console.WriteLine(
                    $"client '{clientId}' sent output for unrecognized call '{funcOutput.CallGuid}'.");
                return;
            }
            if(promise is null)
            {
                System.Console.WriteLine(
                    $"internal bookkeeping error: client '{clientId}': call '{funcOutput.CallGuid}': our promise object was null.");
                return;
            }
            promise.SetResult(funcOutput);
        };

        System.Console.WriteLine("client disconnected: " + clientId);
        if(clientComms.OpenCalls.Count > 0)
            System.Console.WriteLine($"client {clientId} had {clientComms.OpenCalls.Count} calls open when it disconnected");

        clients.TryRemove(clientId, out _);
    }
}
