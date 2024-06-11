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
            _logger.LogInformation($"client sent {request.Field}, {request.NewField}.");

            return Task.FromResult(new Generated.Resp(){ Field = request.Field, NewField = "newVal"});
        }

    }

}
