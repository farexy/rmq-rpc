using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq.Rpc;

namespace Rmq.Example.Client
{
    public class Handler : IRpcCallHandler<TestRequest, TestReply>
    {
        public Task<TestReply> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            var reply = new string(request.Text.Reverse().ToArray());
            return Task.FromResult(new TestReply {Text = reply});
        }
    }
}