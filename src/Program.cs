using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Registrando os services como Singleton
builder.Services.AddSingleton<ICpuMonitor, CpuMonitor>();
builder.Services.AddSingleton<IMemoryMonitor, MemoryMonitor>();
builder.Services.AddSingleton<IDiskMonitor, DiskMonitor>();
builder.Services.AddSingleton<INetworkMonitor, NetworkMonitor>();

var host = builder.Build();

// Resolve os services (DI)
var cpu = host.Services.GetRequiredService<ICpuMonitor>();
var mem = host.Services.GetRequiredService<IMemoryMonitor>();
var disk = host.Services.GetRequiredService<IDiskMonitor>();
var net = host.Services.GetRequiredService<INetworkMonitor>();

void PrintSystemInfo(
    ICpuMonitor cpuMonitor,
    IMemoryMonitor memoryMonitor,
    IDiskMonitor diskMonitor,
    INetworkMonitor networkMonitor)
{
    var (diskUsedGb, diskTotalGb) = diskMonitor.GetDiskUsage();
    var (ip, mac) = networkMonitor.GetNetworkInfo();

    var hostname = Environment.MachineName;
    var osDescription = RuntimeInformation.OSDescription;

    Console.WriteLine("\nInformacoes do Sistema:");
    Console.WriteLine("  Asset ID: N/A");
    Console.WriteLine($"  Hostname: {hostname}");
    Console.WriteLine($"  CPU: {cpuMonitor.GetCpuUsage():0.00}%");
    Console.WriteLine($"  RAM: {memoryMonitor.GetMemoryUsage():0.00}%");
    Console.WriteLine($"  Storage: {diskUsedGb:0.00}GB / {diskTotalGb:0.00}GB");
    Console.WriteLine($"  IP: {ip}");
    Console.WriteLine($"  MAC: {mac}");
    Console.WriteLine($"  OS: {osDescription}");
    Console.WriteLine("\nIntervalo: 2 segundos");
    Console.WriteLine(new string('=', 60));
    Console.WriteLine("\nIniciando monitoramento...\n");
}

PrintSystemInfo(cpu, mem, disk, net);

// Loop
while (true)
{
    var (diskUsedGb, diskTotalGb) = disk.GetDiskUsage();
    var (ip, mac) = net.GetNetworkInfo();

    Console.Clear();

    Console.WriteLine("==== SYSTEM MONITOR ====\n");

    Console.WriteLine($"CPU:  {cpu.GetCpuUsage():0.00}%");
    Console.WriteLine($"RAM:  {mem.GetMemoryUsage():0.00}%");
    Console.WriteLine($"ROM:  {diskUsedGb:0.00}GB / {diskTotalGb:0.00}GB");
    Console.WriteLine($"IP:    {ip}");
    Console.WriteLine($"MAC:   {mac}");

    Thread.Sleep(2_000);
}