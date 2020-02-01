using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HdProduction.MessageQueue.RabbitMq.Rpc
{
    public class RpcCallConsumer<TRequest, TReply> : BackgroundService
    {
        private const string DefaultMessageExpiration = "10000";
        
        private IModel _consumerChannel;
        private readonly string _queueName;
        private readonly IRabbitMqConnection _connection;
        private readonly string _exchangeName;
        private readonly IServiceProvider _serviceProvider;
        private CancellationToken _stoppingToken;

        public RpcCallConsumer(string exchangeName, IServiceProvider serviceProvider, IRabbitMqConnection connection)
        {
            _queueName = typeof(TRequest).Name;
            _exchangeName = exchangeName;
            _serviceProvider = serviceProvider;
            _connection = connection;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumerChannel = CreateConsumerChannel();

            _consumerChannel.QueueBind(_queueName, _exchangeName, _queueName);

            _stoppingToken = stoppingToken;

            return Task.CompletedTask;
        }

        private IModel CreateConsumerChannel()
        {
            if (!_connection.IsConnected)
            {
                _connection.Connect();
            }

            var channel = _connection.CreateChannel();

            channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, true);
            channel.QueueDeclare(_queueName, true, autoDelete: false, exclusive: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += OnMessage;

            channel.BasicConsume(_queueName, true, consumer);

            return channel;
        }

        private async Task OnMessage(object sender, BasicDeliverEventArgs args)
        {
            string eventName = args.RoutingKey;
            if (eventName != _queueName)
            {
                return;
            }

            var request = JsonSerializer.Deserialize<TRequest>(args.Body);
            var handler = _serviceProvider.GetService<IRpcCallHandler<TRequest, TReply>>();

            var reply = await handler.HandleAsync(request, _stoppingToken);
            Publish(reply, args.BasicProperties.CorrelationId, args.BasicProperties.ReplyTo);
        }
        
        private void Publish(TReply reply, string correlationId, string replyTo)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(reply);
 
            var responseProps = _consumerChannel.CreateBasicProperties();
            responseProps.CorrelationId = correlationId;
            responseProps.Expiration = DefaultMessageExpiration;
 
            _consumerChannel.BasicPublish(_exchangeName, replyTo, responseProps, body);
        }
    }
}