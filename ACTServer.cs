using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
//using Codeplex.Data;
namespace LanPlugin
{
    class ACTServer
    {
        bool _active;
        LanPlugin _plugin;
        TcpListener _server = null;
        List<TcpInfo> _clients;
        Queue<string> _queue;
        ManualResetEvent _signal;

        public event EventHandler Close;
        public void OnClose()
        {
            if (Close != null)
                Close(this, EventArgs.Empty);
        }

        public ACTServer(LanPlugin plugin)
        {
            _plugin = plugin;
            _active = false;
            _clients = new List<TcpInfo>();
            _queue = new Queue<string>();
            _signal = new ManualResetEvent(false);
        }
        public async void Start()
        {
            if (_active)
                return;
            _active = true;

            //キューにログを詰め込む
            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(BeforeLogLineRead);
            //詰め込んだログを配信する
            queueCheckAsync();

            int port = _plugin.Controller.Port;
            IPAddress localAddr = _plugin.Controller.GetIP();
            try {
                _server = new TcpListener(localAddr, port);
                _server.Start();
                _plugin.Log("Starting... " + localAddr.ToString() + ":" + port.ToString());
                while (true)
                {
                    var client = await _server.AcceptTcpClientAsync();
                    client.NoDelay = true;

                    //dynamic newjson = new DynamicJson();
                    //newjson.Name = ActGlobals.oFormActMain.

                    lock (_clients)
                    {
                        _clients.Add(new TcpInfo {
                            client = client,
                            stream = client.GetStream()
                        });
                        _plugin.Log("Accepted client: "+ ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                    }
                }
            }
            catch (System.ObjectDisposedException)
            {
                _plugin.Log("close");
            }
            catch (Exception e) {
                _plugin.Log(e.ToString());

            }
            finally
            {
                if (_server != null)
                    _server.Stop();
                _server = null;
            }
            lock (_clients)
            {
                foreach (var c in _clients)
                {
                    c.stream.Close();
                    c.client.Close();
                }
                _clients.Clear();
            }
            ActGlobals.oFormActMain.BeforeLogLineRead -= new LogLineEventDelegate(BeforeLogLineRead);
            _active = false;
            OnClose();

        }
        public void Stop()
        {
            if (!_active)
                return;
            if (_server != null)
                _server.Stop();
            if (_signal != null)
                _signal.Set();
        }
        private void BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo) {
            if (!isImport)
            {
                lock (_queue)
                {
                    _queue.Enqueue(logInfo.logLine);
                    _signal.Set();
                }
            }
        }
        private async void queueCheckAsync()
        {
            await Task.Run(() => {
                var logs = new List<string>();
                while (_active)
                {
                    if (!_signal.WaitOne(1000))
                        continue;
                    lock (_queue)
                    {
                        if (_queue.Count > 0)
                        {
                            logs.AddRange(_queue.ToArray());
                            
                            _queue.Clear();
                        }
                        _signal.Reset();
                    }
                    if (logs.Count < 1)
                        continue;

                    //接続クライアントにログを送信
                    lock (_clients)
                    {
                        foreach (var log in logs) {
                            byte[] data = System.Text.Encoding.UTF8.GetBytes(log.TrimEnd()+"\r\n");
                            for (var i = _clients.Count - 1; i >= 0; i--)
                            {
                                var client = _clients[i];
                                try
                                {
                                    client.stream.Write(data, 0, data.Length);
                                    //client.stream.Flush();
                                }
                                catch
                                {
                                    client.stream.Close();
                                    client.client.Close();
                                    _clients.RemoveAt(i);
                                    _plugin.Log("close Client");
                                }
                            }
                        }
                    }
                    logs.Clear();
                }
            });
        }
    }
}
