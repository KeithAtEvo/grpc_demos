using Grpc.Core;
using System.Collections.Concurrent;


namespace Rpc
{

    //We're not actually doing anything with this yet.
    class Comms
    {
        public static ConcurrentDictionary<string, bool> clients = new();
    };

}

namespace Rpc.Straight
{

    public class Service : Generated.Service.ServiceBase
    {
        private readonly ILogger<Service> _logger;

        public Service(ILogger<Service> logger)
        {
            _logger = logger;
        }

        public override Task<Generated.RegisterClientResponse>
        RegisterClient(
            Generated.RegisterClientRequest request,
            ServerCallContext context)
        {
            string id = Guid.NewGuid().ToString();
            Comms.clients.TryAdd(id, true);

            System.Console.WriteLine($"registered client, bestowed id {id}.");

            return Task.FromResult(new Generated.RegisterClientResponse(){ BestowedId = id });
        }

        public override Task<Generated.DeregisterClientResponse>
        DeregisterClient(
            Generated.DeregisterClientRequest request,
            ServerCallContext context)
        {
            bool ok = Comms.clients.TryRemove(request.BestowedId, out _);
            if (!ok)
            {
                Console.WriteLine($"client identifying as '{request.BestowedId}' requested deregistration, but no such registration was found.");
            }
            else
            {
                System.Console.WriteLine($"deregistered client {request.BestowedId}.");
            }

            return Task.FromResult(new Generated.DeregisterClientResponse() { Ok = ok });
        }

    }

}
