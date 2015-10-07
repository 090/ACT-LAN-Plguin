using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace LanPlugin
{
    public class LanPlugin : IActPluginV1
    {
        Label lblStatus;
        MainTabPage maintabpage;
        public TcpController Controller { get; set; }
        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {

            Controller = new TcpController(this);
            Controller.Mode = TcpMode.Server;
            maintabpage = new MainTabPage(this);

            lblStatus = pluginStatusText;
            pluginScreenSpace.Controls.Add(maintabpage);
            maintabpage.Dock = DockStyle.Fill;

            lblStatus.Text = "Plugin Started";
            pluginScreenSpace.Text = "LAN Settings";


            maintabpage.AutoStart();
        }

        void IActPluginV1.DeInitPlugin()
        {
            maintabpage.SaveSettings();
            lblStatus.Text = "Plugin Exited";
        }

        public void Log(string text)
        {
            text = text.Trim();
            maintabpage.AppendLog(text);
            Console.WriteLine(text);
        }
    }
    }
