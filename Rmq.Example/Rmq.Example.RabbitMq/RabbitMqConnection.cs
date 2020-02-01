using System;
using System.Net.Sockets;
using System.Reflection;
using HdProduction.MessageQueue.RabbitMq.Helpers;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HdProduction.MessageQueue.RabbitMq
{
  public class RabbitMqConnection : IRabbitMqConnection, IDisposable
  {
    private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private bool _disposed;
    private IConnection _connection;
    private readonly ConnectionFactory _connectionFactory;
    private readonly object _sync = new object();

    public RabbitMqConnection(string uri)
    {
      _connectionFactory = new ConnectionFactory
      {
        Uri = new Uri(uri),
        AutomaticRecoveryEnabled = true,
        DispatchConsumersAsync = true
      };
    }
    

    public IModel CreateChannel()
    {
      if (!IsConnected)
        throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
      return _connection.CreateModel();
    }

    public void Connect()
    {
      lock (_sync)
      {
        RetryPolicy.ExecuteAndCapture<SocketException, BrokerUnreachableException>(5, TimeSpan.FromSeconds(3),
          () => _connection = _connectionFactory.CreateConnection());
        if (IsConnected)
        {
          _connection.CallbackException += OnCallbackException;
          _connection.RecoverySucceeded += OnRecoverySucceeded;
          _connection.ConnectionShutdown += OnConnectionShutdown;
          Log.InfoFormat("RabbitMQ persistent connection acquired a connection {0}", _connection.Endpoint.HostName);
        }
        else
        {
          Log.Fatal("FATAL ERROR: RabbitMQ connections could not be created and opened");
        }
      }
    }

    public bool IsConnected => _connection != null && _connection.IsOpen;

    public void Dispose()
    {
      if(_disposed) return;
      _disposed = true;

      if(!IsConnected) return;

      _connection.Dispose();
    }

    private void OnRecoverySucceeded(object sender, EventArgs e)
    {
      if (_disposed) return;
      Log.Warn("A RabbitMQ connection has been recovered.");
    }

    private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
      if (_disposed) return;
      Log.Warn("A RabbitMQ connection throw exception.", e.Exception);
    }

    private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
      if (_disposed) return;
      Log.Warn("A RabbitMQ connection is on shutdown.");
    }
  }
}