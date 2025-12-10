using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Http;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ICpuMonitor _cpu;
    private readonly IMemoryMonitor _mem;
    private readonly IDiskMonitor _disk;
    private readonly INetworkMonitor _net;

    private readonly HttpClient _http;

    private const string API_URL = "https://mmvnezkbbmlbxisurubl.supabase.co/functions/v1/rapid-endpoint";
    private const string API_TOKEN = "asset-monitor-agent-2024";

    private const int TIMER = 2_000;
    private readonly int timerSeconds = TIMER / 1000;

    public Worker(
        ILogger<Worker> logger,
        ICpuMonitor cpu,
        IMemoryMonitor mem,
        IDiskMonitor disk,
        INetworkMonitor net)
    {
        _logger = logger;
        _cpu = cpu;
        _mem = mem;
        _disk = disk;
        _net = net;

        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_TOKEN}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Monitor iniciado. Intervalo: {timerSeconds} segundos");

        var cpuUsage = _cpu.GetCpuUsage();
        var memUsage = _mem.GetMemoryUsage();
        var (diskUsed, diskTotal) = _disk.GetDiskUsage();
        var (ip, mac) = _net.GetNetworkInfo();

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendMetricsAsync(); // TODO: Melhorar
            await Task.Delay(TIMER, stoppingToken);
        }
    }

    private async Task SendMetricsAsync()
    {
        var (diskUsedGb, diskTotalGb) = _disk.GetDiskUsage();
        var (ip, mac) = _net.GetNetworkInfo();

        // TODO: MUDAR PARA ID REAL
        var assetId = Environment.MachineName;
        var hostname = Environment.MachineName;

        var payload = new
        {
            asset_id = assetId, // TODO: mudar para ID real
            hostname = hostname,
            cpu_usage = _cpu.GetCpuUsage(),
            memory_usage = _mem.GetMemoryUsage(),
            disk_usage = Math.Round((diskUsedGb / diskTotalGb) * 100, 2),
            network_in = 0,  // TODO: implementar
            network_out = 0,
            uptime = (long)Environment.TickCount64 / 1000,

            cpu_info = new
            {
                model = "unknown", // TODO: mostrar modelo
                cores = Environment.ProcessorCount
            },

            ram_info = new
            {
                usage_percent = _mem.GetMemoryUsage() // TODO: mostrar total em GB
            },

            storage_info = new
            {
                disks = new[]
                {
                    new { name = "C:", used_gb = diskUsedGb, total_gb = diskTotalGb }
                }
            },

            ip_address = ip,
            mac_address = mac,
            os_name = RuntimeInformation.OSDescription,
            os_version = Environment.OSVersion.Version.ToString()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(API_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro ao enviar métricas: {Status}", response.StatusCode);
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Detalhes: {Detail}", errorText);
            }
            else
            {
                _logger.LogInformation("Métricas enviadas com sucesso.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar métricas.");
        }
    }
}
