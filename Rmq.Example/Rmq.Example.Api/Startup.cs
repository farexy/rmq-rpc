using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq;
using HdProduction.MessageQueue.RabbitMq.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rmq.Example.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>(c => new RabbitMqConnection("amqp://localhost/"));

            services.AddSingleton<IRabbitMqConnection>(
                new RabbitMqConnection("amqp://localhost/"));
            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>(c => new RabbitMqPublisher("rmq_ex_rpc", c.GetRequiredService<IRabbitMqConnection>()));

            services.AddSingleton<IRpcClient<TestRequest, TestReply>>(c =>
                new RpcClient<TestRequest, TestReply>("rmq_ex_rpc", c.GetRequiredService<IRabbitMqConnection>()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            

            app.UseRouting();
            
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}