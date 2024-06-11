
using System.Collections.Concurrent;

namespace RpcServices
{

    public class EchoFunc
    {

        public class Result
        {
            public string EchoedVal { get; set; }
            // etc...

            public Result(
                string echoedVal
            //, etc ...
            )
            {
                EchoedVal = echoedVal;
                // etc...
            }

            internal Result(
                Rpc.Reverse.Generated.EchoOutput output
            //, etc ...
            ) : this(output.TheEcho)
            {
            }
        }

        public class Args
        {
            public string ValToEcho { get; set; }
            // etc...

            public Args(
                string valToEcho
            //, etc ...
            )
            {
                ValToEcho = valToEcho;
                // etc...
            }

            internal Args(
                Rpc.Reverse.Generated.EchoInput input
            //, etc ...
            ) : this(input.ToEcho)
            {
            }
        }

        static ConcurrentDictionary<string, Task<Result>> openCalls = new();

        public static async Task<Result> Run(string id, Args args)
        {
            bool clientHasFunctionality = Rpc.Reverse.Service.Clients.TryGetValue(id, out Rpc.Reverse.Service.EchoClientComms? comms);
            if (!clientHasFunctionality)
            {
                System.Console.WriteLine(
                    $"tried to call reverse-function Echo to client {id}"
                    + ", but no client with that id has advertised support of this function.");

                return new Result("<see error message>");
            }
            if (comms is null)
            {
                System.Console.WriteLine(
                    $"internal bookkeeping error: client {id} has advertised support of this function, but our comms object is null.");

                return new Result("<see error message>");
            }

            Rpc.Reverse.Generated.EchoInput input = new() { ToEcho = args.ValToEcho, CallGuid = Guid.NewGuid().ToString() };

            TaskCompletionSource<Rpc.Reverse.Generated.EchoOutput> promise = new();

            if (!comms.OpenCalls.TryAdd(
                input.CallGuid,
                promise))
            {
                System.Console.WriteLine(
                    $"internal bookkeeping error: client {id}: tried to register a second call with id {input.CallGuid}.");

                return new Result("<see error message>");
            }

            System.Console.WriteLine(
                "sending reverse-func input:"
                + $" client: {id}"
                + $" call: {input.CallGuid}"
                + $" arg: {input.ToEcho}");

            await comms.InputStream.WriteAsync(input);

            //Task is completed by EchoService when we receive the response.
            Rpc.Reverse.Generated.EchoOutput output = await promise.Task;

            System.Console.WriteLine(
                "client responded with output:"
                + $" call: {output.CallGuid}"
                + $" arg: {output.TheEcho}");

            return new Result(output);
        }
    }

}
