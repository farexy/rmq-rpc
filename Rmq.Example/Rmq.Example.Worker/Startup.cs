using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HdProduction.MessageQueue.RabbitMq;
using HdProduction.MessageQueue.RabbitMq.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rmq.Example.Client
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>(c => new RabbitMqConnection("amqp://localhost/"));
            services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>(c =>
                new RabbitMqPublisher("rmq_ex_events", c.GetRequiredService<IRabbitMqConnection>()));
            services.AddSingleton<IHostedService, RpcCallConsumer<TestRequest, TestReply>>(c => 
                new RpcCallConsumer<TestRequest, TestReply>("rmq_ex_rpc", c, c.GetRequiredService<IRabbitMqConnection>()));
            services.AddTransient<IRpcCallHandler<TestRequest, TestReply>, Handler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
        }
    }
}