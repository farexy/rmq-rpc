using System.Threading;
using System.Threading.Tasks;

namespace HdProduction.MessageQueue.RabbitMq.Rpc
{
    public interface IRpcCallHandler<TRequest, TReply>
    {
        Task<TReply> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}