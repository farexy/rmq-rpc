using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq.Events;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HdProduction.MessageQueue.RabbitMq
{
  public interface IRabbitMqConsumer
  {
    IRabbitMqConsumer Subscribe<T>() where T : HdMessage;
    void StartConsuming();
  }

  public class RabbitMqConsumer<T> : IRabbitMqConsumer
  {
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
    private IModel _consumerChannel;
    private readonly Dictionary<string, Type> _supportedMessages;
    private readonly string _exchange;
    private readonly string _queueName;
    private readonly IRabbitMqConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    public RabbitMqConsumer(string exchange, string queueName, IServiceProvider serviceProvider, IRabbitMqConnection connection)
    {
      _exchange = exchange;
      _queueName = queueName;
      _connection = connection;
      _supportedMessages = new Dictionary<string, Type>();
      _serviceProvider = serviceProvider;
    }

    public IRabbitMqConsumer Subscribe<T>() where T : HdMessage
    {
      _supportedMessages.Add(typeof(T).Name, typeof(T));
      return this;
    }

    public void StartConsuming()
    {
      _consumerChannel = CreateConsumerChannel();

      foreach (string eventName in _supportedMessages.Keys)
      {
        _consumerChannel.QueueBind(_queueName, _exchange, eventName);
      }
    }

    private IModel CreateConsumerChannel()
    {
      if (!_connection.IsConnected)
      {
        _connection.Connect();
      }

      var channel = _connection.CreateChannel();

      channel.ExchangeDeclare(_exchange, ExchangeType.Direct, true);
      channel.QueueDeclare(_queueName, true, autoDelete: false, exclusive: false);

      var consumer = new AsyncEventingBasicConsumer(channel);
      consumer.Received += OnMessage;

      channel.BasicConsume(_queueName, false, consumer);

      return channel;
    }

    private async Task OnMessage(object sender, BasicDeliverEventArgs args)
    {
      try
      {
        string eventName = args.RoutingKey;
        string message = Encoding.UTF8.GetString(args.Body);

        if (!_supportedMessages.ContainsKey(eventName))
        {
          return;
        }

        var @event = JsonSerializer.Deserialize<T>(message);
        var handler = _serviceProvider.GetService<IMessageHandler<T>>();

        await handler.HandleAsync(@event);
        _consumerChannel.BasicAck(args.DeliveryTag, false);
      }
      catch (Exception ex)
      {
        Log.Error("Rabbit Mq message processing error", ex);
      }
    }
  }
}