using System.ComponentModel;
using System.Diagnostics.Metrics;

using ReverseFuncs;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

namespace ReverseFuncs.Services;
public class ReverseFuncService : ReverseFunc.ReverseFuncBase
{
    private readonly ILogger<ReverseFuncService> _logger;
    public ReverseFuncService(ILogger<ReverseFuncService> logger) {
        _logger = logger;
    }

    //TODO: mutex this
    static public HashSet<IServerStreamWriter<ReverseFuncInputPayload>> knownClients = new ();

    public override async Task
        ReverseFuncProcessor(
            IAsyncStreamReader<ReverseFuncOutputPayload> funcOutputStreamIn,
            IServerStreamWriter<ReverseFuncInputPayload> funcInputStreamOut,
            ServerCallContext context)
    {
        System.Console.WriteLine("client connected: " + funcInputStreamOut.GetHashCode().ToString());
        knownClients.Add(funcInputStreamOut);

        while (await funcOutputStreamIn.MoveNext()) {
            var funcOutput = funcOutputStreamIn.Current;

            System.Console.WriteLine("received output: '" + funcOutput.TheEcho + "' for input id: '" + funcOutput.CallGuid + "'");
          
        }

        System.Console.WriteLine("client disconnected: " + funcInputStreamOut.GetHashCode().ToString());
        knownClients.Remove(funcInputStreamOut);

    }
}
