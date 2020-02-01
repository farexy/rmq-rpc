using RabbitMQ.Client;

namespace HdProduction.MessageQueue.RabbitMq
{
  public interface IRabbitMqConnection
  {
    IModel CreateChannel();
    void Connect();
    bool IsConnected { get; }
  }
}