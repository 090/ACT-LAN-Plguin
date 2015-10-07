using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace LanPlugin
{
    public class TcpInfo
    {
        public TcpClient client { get; set; }
        public NetworkStream stream { get; set; }
    }
    public enum TcpMode { None=0,Server, Client }
    public class TcpController
    {
        LanPlugin _plugin;
        ACTServer _server=null;
        ACTClient _client=null;

        public string Ip { get; set; }
        public int Port { get; set; }
        public TcpController(LanPlugin plugin)
        {
            _plugin = plugin;
        }
        bool _active=false;
        public bool Active {
            get { return _active; }
        }
        public event EventHandler ActiveUpdate;
        public void OnActiveUpdate()
        {
            if (ActiveUpdate != null)
                ActiveUpdate(this, EventArgs.Empty);
        }

        TcpMode _mode;
        public TcpMode Mode {
            get { return _mode; }
            set
            {
                if (_active)
                    return;
                if (_mode == value)
                    return;
                _mode = value;
                OnModeUpdate();
            }
        }
        public event EventHandler ModeUpdate;
        public void OnModeUpdate()
        {
            if (ModeUpdate != null)
                ModeUpdate(this, EventArgs.Empty);
        }

        public void Start()
        {
            if (_active)
                return;
            _active = true;
            OnActiveUpdate();
            if (_mode == TcpMode.Server) {
                _server = new ACTServer(_plugin);
                _server.Close += OnClose;
                _server.Start();
            }
            else if(_mode == TcpMode.Client) { 
                _client = new ACTClient(_plugin);
                 _client.Close += OnClose;
               _client.Start();
            }

        }
        public void Stop()
        {
            if (!_active)
                return;

            if (_mode == TcpMode.Server)
            {
                if(_server != null)
                    _server.Stop();
                _server = null;
            }
            else if (_mode == TcpMode.Client)
            {
                if(_client != null)
                    _client.Stop();
                _client = null;
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            _active = false;
            OnActiveUpdate();
        }

        public IPAddress GetIP()
        {
            IPAddress addr = null;
            try
            {
                IPHostEntry iphe = Dns.GetHostEntry(this.Ip);
                foreach (var i in iphe.AddressList)
                {

                    if (i.AddressFamily == AddressFamily.InterNetwork)
                    {
                        addr = i;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

            }
            if (addr == null)
            {
                addr = IPAddress.Any;
            }
            return addr;
        }
    }
}
