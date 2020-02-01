using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq.Events;

namespace HdProduction.MessageQueue.RabbitMq
{
  internal class MessageHandler
  {
    private readonly ConcurrentDictionary<Type, HandlerMetadata> _handlerTypesCache = new ConcurrentDictionary<Type, HandlerMetadata>();

    private readonly IServiceProvider _serviceProvider;

    public MessageHandler(IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task ProcessAsync(Type eventType, string message)
    {
      var @event = JsonSerializer.Deserialize(message, eventType);

      var meta = _handlerTypesCache.GetOrAdd(eventType, key =>
      {
        Type handlerType = typeof(IMessageHandler<>).MakeGenericType(key);
        return new HandlerMetadata(handlerType.GetMethod(nameof(IMessageHandler<HdMessage>.HandleAsync)),
          typeof(IEnumerable<>).MakeGenericType(handlerType));
      });

      await HandleInternal(meta, @event);
    }

    private async Task HandleInternal(HandlerMetadata metadata, object @event)
    {
      var handlers = (IEnumerable<object>)_serviceProvider.GetService(metadata.HandlerType);
      foreach (var handler in handlers)
      {
        await (Task)metadata.HandlerMethod.Invoke(handler, new[] {@event});
      }
    }

    protected class HandlerMetadata
    {
      public HandlerMetadata(MethodInfo handlerMethod, Type handlerType)
      {
        HandlerMethod = handlerMethod ?? throw new ArgumentNullException(nameof(handlerMethod));
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
      }

      public MethodInfo HandlerMethod { get; }
      public Type HandlerType { get; }
    }
  }
}