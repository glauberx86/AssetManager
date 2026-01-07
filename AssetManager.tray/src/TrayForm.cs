// Tenho que logar saporra

using System;
using System.Windows.Forms;
using System.ServiceProcess;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AssetManager.Tray
{
    public class TrayForm : Form
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly HttpDebugService _httpDebugService = new();
        private const string ServiceName = "AssetManager";
        private ClientWebSocket _ws = new ClientWebSocket();
        private readonly string? _apiUrl;
        private readonly string? _apiToken;
        private readonly bool _apiEnabled;
        public TrayForm()
        {
            // Ocultar a janela
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Menu
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Enviar agora", null, OnSendNow);
            trayMenu.Items.Add("Sair", null, OnExit);

            // Icon
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Asset Manager Agent",
                ContextMenuStrip = trayMenu
            };

            var config = Program.Configuration;
            _apiUrl = config["Api:Url"];
            _apiToken = config["Api:Token"];
            _apiEnabled = !string.IsNullOrWhiteSpace(_apiUrl);

            _ = Task.Run(_httpDebugService.StartAsync);
            _ = Task.Run(PipeReadLoop);
            EnsureServiceRunning();
        }
        private void EnsureServiceRunning()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }
            }
            catch
            {
                // TODO: Log
            }
        }
        private void StopService()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                }
            }
            catch
            {
                //TODO: Log
            }
        }

        private void OnSendNow(object? sender, EventArgs e)
        {
            MessageBox.Show("O envio é automático via WebSocket quando há dados no pipe.");
        }

        private async void OnExit(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            StopService();

            if (_ws.State == WebSocketState.Open)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "App closing", CancellationToken.None);
            }
            _ws.Dispose();

            Application.Exit();
        }

        private async Task PipeReadLoop()
        {
            while (true)
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(".", "asset-monitor-pipe", PipeDirection.In);
                    await pipe.ConnectAsync(3000);

                    using var reader = new StreamReader(pipe);
                    var json = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        HttpDebugService.UpdateSnapshot(json);
                        // Chama o envio via Socket
                        await SendToWebSocketAsync(json);
                    }
                }
                catch
                {
                    // Pipe indisponível ou erro de leitura, espera e tenta de novo
                }
                await Task.Delay(2000);
            }
        }

        // Retry e send
        private async Task SendToWebSocketAsync(string json)
        {
            if (!_apiEnabled || _apiUrl == null) return;

            try
            {
                if (_ws.State != WebSocketState.Open)
                {
                    if (_ws.State == WebSocketState.Aborted || _ws.State == WebSocketState.Closed)
                    {
                        _ws.Dispose();
                        _ws = new ClientWebSocket();
                    }
                    if (!string.IsNullOrWhiteSpace(_apiToken))
                    {
                        _ws.Options.SetRequestHeader("Authorization", $"Bearer {_apiToken}");
                    }

                    await _ws.ConnectAsync(new Uri(_apiUrl), CancellationToken.None);
                }

                // Envia os dados
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None
                );
            }
            catch
            {
                //TODO: Log
            }
        }
    }
}