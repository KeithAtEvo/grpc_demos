using Grpc.Core;
using Microsoft.AspNetCore.Identity.Data;
using System.Collections.Concurrent;


//We're not actually doing anything with this yet.
class Comms
{
    public static ConcurrentDictionary<string, bool> clients = new();
};

namespace RpcServices
{

    public class StraightFuncService : RpcGenerated.Straight.StraightBase
    {
        private readonly ILogger<StraightFuncService> _logger;

        public StraightFuncService(ILogger<StraightFuncService> logger)
        {
            _logger = logger;
        }

        public override Task<RpcGenerated.RegisterClientResponse>
        RegisterClient(
            RpcGenerated.RegisterClientRequest request,
            ServerCallContext context)
        {
            string id = Guid.NewGuid().ToString();
            Comms.clients.TryAdd(id, true);
            return Task.FromResult(new RpcGenerated.RegisterClientResponse(){ BestowedId = id });
        }

        public override Task<RpcGenerated.DeregisterClientResponse>
        DeregisterClient(
            RpcGenerated.DeregisterClientRequest request,
            ServerCallContext context)
        {
            bool ok = Comms.clients.TryRemove(request.BestowedId, out _);
            if(!ok)
            {
                Console.WriteLine($"client identifying as '{request.BestowedId}' requested deregistration, but no such registration was found.");
            }
            return Task.FromResult(new RpcGenerated.DeregisterClientResponse() { Ok = ok });
        }

    }

}
