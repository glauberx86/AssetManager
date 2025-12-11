using System;
using System.Windows.Forms;
using System.ServiceProcess;
using System.IO.Pipes;

namespace AssetManager.Tray
{
    public class TrayForm : Form
    {
        private readonly NotifyIcon trayIcon;
        private readonly ContextMenuStrip trayMenu;
        private const string PIPE_NAME = "asset-monitor-pipe";
        private DebugConsole? debugConsole;
        private readonly List<string> logBuffer = new();

        private void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            logBuffer.Add(line);

            debugConsole?.WriteLine(line);
        }

        private void OpenConsole()
        {
            if (debugConsole == null || debugConsole.IsDisposed)
                debugConsole = new DebugConsole();

            debugConsole.Show();
            debugConsole.BringToFront();

            foreach (var line in logBuffer)
                debugConsole.WriteLine(line);
        }

        private async Task StartPipeServer()
        {
            while (true)
            {
                try
                {
                    using var pipeServer = new NamedPipeServerStream(
                        PIPE_NAME,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipeServer.WaitForConnectionAsync();

                    Log("Conexão estabelecida com o serviço.");

                    using var reader = new StreamReader(pipeServer);
                    string? json = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        Log($"[{DateTime.Now:HH:mm:ss}] Dados recebidos:");
                        Log(json);
                        Log(new string('-', 40));
                    }
                }
                catch (Exception ex)
                {
                    debugConsole?.WriteLine($"Erro no pipe: {ex.Message}");
                }
            }
        }
        private void EnsureServiceRunning()
        {
            const string serviceName = "AssetManager";

            try
            {
                using var sc = new ServiceController(serviceName);

                if (sc.Status == ServiceControllerStatus.Stopped ||
                    sc.Status == ServiceControllerStatus.StopPending)
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar serviço: {ex.Message}");
            }
        }

        public TrayForm()
        {
            // Ocultar a janela
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;

            // Menu
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Abrir Console", null, (_, __) => OpenConsole());
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
            EnsureServiceRunning();
            _ = Task.Run(StartPipeServer);
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
    }
}
