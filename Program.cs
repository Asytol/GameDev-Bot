// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;

using NetCord.Hosting.Gateway;

namespace GameDevBot
{
    internal class MainProgram
    {
        static async Task Main(string[] args){
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddDiscordGateway();

            IHost host = builder.Build();

            await host.RunAsync();

            Console.WriteLine("test");
        }
    }
}