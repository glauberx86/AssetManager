using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AssetManager.Tray
{
    public partial class DebugConsole : Form
    {
        public DebugConsole()
        {
            InitializeComponent();
            txtLog.ReadOnly = true;
        }

        public void WriteLine(string text)
        {
            txtLog.AppendText($"{text}\r\n");
        }
    }
}
