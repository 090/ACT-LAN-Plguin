using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace LanPlugin
{
    public partial class MainTabPage : UserControl
    {
        LanPlugin _plugin;
        string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, @"Config\LanPlugin.config.xml");
        SettingsSerializer _xmlSettings;


        delegate void AddLogUpdate(string text);
        private AddLogUpdate _AddLogUpdate = null;
        private void addLog(string text)
        {
            textBoxLog.AppendText(text + "\r\n");
        }
        public void AppendLog(string text)
        {
            textBoxLog.Invoke(_AddLogUpdate, text);
        }

        public MainTabPage(LanPlugin plugin)
        {
            InitializeComponent();

            _plugin = plugin;
            _plugin.Controller.ActiveUpdate += Controller_ActiveUpdate;
            Controller_ActiveUpdate(this, null);
            _plugin.Controller.ModeUpdate += Controller_ModeUpdate;
            Controller_ModeUpdate(this, null);

            _AddLogUpdate = addLog;

            _xmlSettings = new SettingsSerializer(this);
            LoadSettings();
            if (textBoxPort.Text.Length == 0)
                textBoxPort.Text = "5000";
        }

        public void AutoStart()
        {
            if (checkBoxAutostart.Checked)
                _plugin.Controller.Start();
        }

        void LoadSettings()
        {

            _xmlSettings.AddControlSetting(textBoxIp.Name, textBoxIp);
            _xmlSettings.AddControlSetting(textBoxPort.Name, textBoxPort);
            _xmlSettings.AddControlSetting(checkBoxAutostart.Name, checkBoxAutostart);
            _xmlSettings.AddControlSetting(radioButtonClient.Name, radioButtonClient);
            _xmlSettings.AddControlSetting(radioButtonServer.Name, radioButtonServer);

            if (!File.Exists(settingsFile))
                return;

            FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            XmlTextReader xReader = new XmlTextReader(fs);
            while (xReader.Read())
            {
                if (xReader.NodeType == XmlNodeType.Element && xReader.LocalName == "SettingsSerializer")
                    _xmlSettings.ImportFromXml(xReader);
            }
            xReader.Close();
        }

        public void SaveSettings()
        {
            FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
            xWriter.Formatting = Formatting.Indented;
            xWriter.Indentation = 1;
            xWriter.IndentChar = '\t';
            xWriter.WriteStartDocument(true);
            xWriter.WriteStartElement("Config");
            xWriter.WriteStartElement("SettingsSerializer");
            _xmlSettings.ExportToXml(xWriter);
            xWriter.WriteEndElement();
            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();
            xWriter.Close();
        }

        private void Controller_ModeUpdate(object sender, EventArgs e)
        {
            if(_plugin.Controller.Mode == TcpMode.Server)
            {
                radioButtonServer.Checked = true;
            }
            else if (_plugin.Controller.Mode == TcpMode.Client)
            {
                radioButtonClient.Checked = true;
            }
        }
        private void Controller_ActiveUpdate(object sender, EventArgs e)
        {
            buttonStart.Enabled = !_plugin.Controller.Active;
            buttonStop.Enabled = _plugin.Controller.Active;
            groupBoxTCP.Enabled = buttonStart.Enabled;
            groupBoxMode.Enabled = buttonStart.Enabled;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            _plugin.Controller.Start();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            _plugin.Controller.Stop();
        }

        private void radioButtonClient_CheckedChanged(object sender, EventArgs e)
        {
            _plugin.Controller.Mode = TcpMode.Client;
        }

        private void radioButtonServer_CheckedChanged(object sender, EventArgs e)
        {
            _plugin.Controller.Mode = TcpMode.Server;
        }

        private void textBoxPort_TextChanged(object sender, EventArgs e)
        {
            Regex num = new Regex(@"^\d+$");
            if (num.IsMatch(textBoxPort.Text))
                _plugin.Controller.Port = Convert.ToInt32(textBoxPort.Text);
        }

        private void textBoxIp_TextChanged(object sender, EventArgs e)
        {
            _plugin.Controller.Ip = textBoxIp.Text;
        }


        private void textBoxPort_Validating(object sender, CancelEventArgs e)
        {
            Regex num = new Regex(@"\D");
            if (num.IsMatch(textBoxPort.Text))
            {
                this.errorProviderPort.SetError(textBoxPort, "Digital");
                e.Cancel = true;
            }
        }

        private void textBoxPort_Validated(object sender, EventArgs e)
        {
            this.errorProviderPort.SetError((TextBox)sender, null);
        }

        private void MainTabPage_Resize(object sender, EventArgs e)
        {
            var w = ClientSize.Width - groupBoxLog.Left;
            if (w > 0)
                groupBoxLog.Width = w;

            var h = ClientSize.Height - groupBoxLog.Top;
            if (h > 0)
                groupBoxLog.Height = h;

        }
    }
}
