namespace HdProduction.MessageQueue.RabbitMq
{
    public class RabbitMqConnectionOptions
    {
        public string Url { get; set; }
        
        public string ExchangeName { get; set; }
        
        public string RpcExchangeName { get; set; }
        
        public string QueueName { get; set; }
    }
}