using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq.Events;

namespace HdProduction.MessageQueue.RabbitMq
{
  public interface IMessageHandler<in T>
  {
    Task HandleAsync(T ev);
  }
}