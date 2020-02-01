namespace HdProduction.MessageQueue.RabbitMq.Events
{
  public abstract class HdMessage
  {
    public string Name => GetType().Name;

    public override string ToString()
    {
      return Name;
    }
  }
}