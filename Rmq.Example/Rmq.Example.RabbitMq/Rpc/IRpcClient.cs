using System.Threading.Tasks;

namespace HdProduction.MessageQueue.RabbitMq.Rpc
{
    public interface IRpcClient<in TRequest, TReply>
    {
        Task<TReply> CallAsync(TRequest request);
    }
}