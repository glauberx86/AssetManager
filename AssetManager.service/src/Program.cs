using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManager.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "AssetManager";
            });

            builder.Services.AddSingleton<ICpuMonitor, CpuMonitor>();
            builder.Services.AddSingleton<IMemoryMonitor, MemoryMonitor>();
            builder.Services.AddSingleton<IDiskMonitor, DiskMonitor>();
            builder.Services.AddSingleton<INetworkMonitor, NetworkMonitor>();
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
