using Grpc.Core;
using System.Collections.Concurrent;


namespace Rpc
{

    public class Service : Generated.Service.ServiceBase
    {
        private readonly ILogger<Service> _logger;

        public Service(ILogger<Service> logger)
        {
            _logger = logger;
        }

        public override Task<Generated.Resp>
        Func(
            Generated.Req request,
            ServerCallContext context)
        {
            _logger.LogInformation($"client sent grass: '{request.T.Grass}',  gravel: '{request.T.Gravel}',  field: '{request.Field}'.");

            return Task.FromResult(
                new Generated.Resp()
                {
                    Field = "mowed",
                    T = new Generated.InternalType() { Grass = "green", Gravel = "gray" }
                });
        }

    }

}
