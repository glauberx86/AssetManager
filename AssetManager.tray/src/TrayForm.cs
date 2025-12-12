using System;
using System.Windows.Forms;
using System.ServiceProcess;
using System.IO.Pipes;
using System.Net.WebSockets;

namespace AssetManager.Tray
{
    public class TrayForm : Form
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private readonly HttpDebugService _httpDebugService = new();

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
                Icon = SystemIcons.Information,   // TODO: trocar icone
                Visible = true,
                Text = "Asset Manager Agent",
                ContextMenuStrip = trayMenu
            };
            _ = Task.Run(_httpDebugService.StartAsync);
            _ = Task.Run(PipeReadLoop);
        }

        private void OnSendNow(object? sender, EventArgs e)
        {
            MessageBox.Show("Envio manual ainda não implementado.");
        }

        private void OnExit(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
        private async Task PipeReadLoop()
        {
            while (true)
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(
                        ".",
                        "asset-monitor-pipe",
                        PipeDirection.In
                    );

                    await pipe.ConnectAsync(3000);

                    using var reader = new StreamReader(pipe);
                    var json = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        HttpDebugService.UpdateSnapshot(json);
                    }
                }
                catch
                {
                    // Serviço pode estar indisponível ainda
                    // ignorar
                }

                await Task.Delay(2000);
            }
        }
    }
}
