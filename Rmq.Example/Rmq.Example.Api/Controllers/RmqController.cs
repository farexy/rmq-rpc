using System.Net.Http;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq;
using HdProduction.MessageQueue.RabbitMq.Events;
using HdProduction.MessageQueue.RabbitMq.Rpc;
using Microsoft.AspNetCore.Mvc;

namespace Rmq.Example.Api.Controllers
{
    [Route("rmq")]
    public class RmqController
    {
        private readonly IRabbitMqPublisher _publisher;
        private readonly IRpcClient<TestRequest, TestReply> _rpcClient;
        private static HttpClient _client;

        public RmqController(IRabbitMqPublisher publisher, IRpcClient<TestRequest, TestReply> rpcClient)
        {
            _publisher = publisher;
            _rpcClient = rpcClient;
            _client = new HttpClient();
        }

        [HttpGet("async")]
        public async Task Async()
        {
            await _publisher.PublishAsync(new Event1 {Test = "test"});
        }
        
        [HttpGet("rpc")]
        public async Task<TestReply> Rpc()
        {
            return await _rpcClient.CallAsync(new TestRequest{Text = "hello"});
        }
        
        [HttpGet("http")]
        public async Task<string> Http()
        { 
            var r = await _client.GetAsync("http://localhost:5005");
            return await r.Content.ReadAsStringAsync();
        }
    }
}