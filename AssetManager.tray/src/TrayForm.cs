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
