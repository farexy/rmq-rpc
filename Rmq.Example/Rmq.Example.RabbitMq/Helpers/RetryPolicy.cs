using System;
using System.Threading;
using System.Threading.Tasks;

namespace HdProduction.MessageQueue.RabbitMq.Helpers
{
  public static class RetryPolicy
  {
    public static void ExecuteAndCapture<TEx>(int retries, TimeSpan delay, Action action) where TEx : Exception
    {
      try
      {
        action();
      }
      catch (TEx)
      {
        if (retries == 0) throw;
        Thread.Sleep(delay);
        ExecuteAndCapture<TEx>(--retries, delay, action);
      }
    }

    public static void ExecuteAndCapture<TEx, TEx1>(int retries, TimeSpan delay, Action action) where TEx : Exception where TEx1 : Exception
    {
      try
      {
        action();
      }
      catch (TEx)
      {
        if (retries == 0) throw;
        Thread.Sleep(delay);
        ExecuteAndCapture<TEx, TEx1>(--retries, delay, action);
      }
      catch (TEx1)
      {
        if (retries == 0) throw;
        Thread.Sleep(delay);
        ExecuteAndCapture<TEx, TEx1>(--retries, delay, action);
      }
    }

    public static async Task ExecuteAndCaptureAsync<TEx>(int retries, TimeSpan delay, Func<Task> action) where TEx : Exception
    {
      try
      {
        await action();
      }
      catch (TEx)
      {
        if (retries == 0) throw;
        await Task.Delay(delay);
        await ExecuteAndCaptureAsync<TEx>(--retries, delay, action);
      }
    }

    public static async Task ExecuteAndCaptureAsync<TEx, TEx1>(int retries, TimeSpan delay, Func<Task> action)
      where TEx : Exception where TEx1 : Exception
    {
      try
      {
        await action();
      }
      catch (TEx)
      {
        if (retries == 0) throw;
        await Task.Delay(delay);
        await ExecuteAndCaptureAsync<TEx, TEx1>(--retries, delay, action);
      }
      catch (TEx1)
      {
        if (retries == 0) throw;
        await Task.Delay(delay);
        await ExecuteAndCaptureAsync<TEx, TEx1>(--retries, delay, action);
      }
    }
  }
}