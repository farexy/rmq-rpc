using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HdProduction.MessageQueue.RabbitMq.Rpc
{
    public class RpcClient<TRequest, TReply> : IRpcClient<TRequest, TReply>
    {
        private const int DefaultTimeout = 20000;
        private const string DefaultMessageExpiration = "10000";
        
        private readonly string _exchangeName;
        private readonly IRabbitMqConnection _connection;
        private readonly IModel _consumerChannel;
        private AsyncEventingBasicConsumer _consumer;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TReply>> _pendingMessages;

        private readonly string _requestQueue;
        private readonly string _replyQueue;

        public RpcClient(string exchangeName, IRabbitMqConnection connection)
        {
            _exchangeName = exchangeName;
            _connection = connection;
            _requestQueue = typeof(TRequest).Name;
            _replyQueue = typeof(TReply).Name;
 
            _connection.Connect();
            _consumerChannel = _connection.CreateChannel();
 
            _consumerChannel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, true);
            _consumerChannel.QueueDeclare(_requestQueue, true, false, false, null);
            _consumerChannel.QueueDeclare(_replyQueue, true, false, false, null);
 
            _consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            _consumer.Received += Consumer_Received;
            _consumerChannel.BasicConsume(_replyQueue, true, _consumer);
            _consumerChannel.QueueBind(_replyQueue, _exchangeName, _replyQueue);

            _pendingMessages = new ConcurrentDictionary<Guid, TaskCompletionSource<TReply>>();
        }
        
        public Task<TReply> CallAsync(TRequest request)
        {
            var tcs = new TaskCompletionSource<TReply>();
            var correlationId = Guid.NewGuid();
 
            _pendingMessages[correlationId] = tcs;
 
            Publish(request, correlationId);
 
            return TimeoutAfter(tcs.Task, DefaultTimeout);
        }

        private static async Task<TReply> TimeoutAfter(Task<TReply> task, int millisecondsTimeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout))) 
                return await task;
            throw new TimeoutException("Call timed out");
        }
        
        private void Publish(TRequest request, Guid correlationId)
        {
            var props = _consumerChannel.CreateBasicProperties();
            props.CorrelationId = correlationId.ToString();
            props.ReplyTo = _replyQueue;
            props.Expiration = DefaultMessageExpiration;
 
            var body = JsonSerializer.SerializeToUtf8Bytes(request);
            _consumerChannel.BasicPublish(_exchangeName, _requestQueue, props, body);
        }
        
        private Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey != _replyQueue)
            {
                return Task.CompletedTask;
            }
            
            var correlationId = e.BasicProperties.CorrelationId;
            var message = JsonSerializer.Deserialize<TReply>(e.Body);
 
            _pendingMessages.TryRemove(Guid.Parse(correlationId), out var tcs);
            tcs?.SetResult(message);
            return Task.CompletedTask;
        }
    }
}