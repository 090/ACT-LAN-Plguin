using System;
using Advanced_Combat_Tracker;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace LanPlugin
{
    public class ACTClient
    {
        bool Active;

        public event EventHandler Close;
        public void OnClose()
        {
            if (Close != null)
                Close(this, EventArgs.Empty);
        }
        LanPlugin _plugin;
        TcpClient _client;
        CancellationTokenSource _cancen;

        public ACTClient(LanPlugin plugin)
        {
            _plugin = plugin;
        }

        public async void Start()
        {
            if (Active)
                return;
            _cancen = new CancellationTokenSource();
            Active = true;

            string ip = _plugin.Controller.Ip;
            int port = _plugin.Controller.Port;
            while (!_cancen.IsCancellationRequested)
            {
                _client = null;
                NetworkStream stream=null;
                try
                {
                    _client = new TcpClient();
                    _plugin.Log("connect to "+ ip + ":" + port.ToString());
                    await _client.ConnectAsync(ip, port);
                    stream = _client.GetStream();
                    byte[] buffer = new byte[_client.ReceiveBufferSize];
                    int offset = 0;
                    int endPosition = 0;
                    int startPosition = 0;
                    int size = 0;
                    _plugin.Log("recv start");
                    while (true)
                    {
                        var i = await stream.ReadAsync(buffer, offset, buffer.Length - offset, _cancen.Token);
                        if (i < 1 || _cancen.IsCancellationRequested)
                            break;
                        size = offset + i;
                        startPosition = 0;
                        while ((endPosition = Array.IndexOf(buffer, (byte)10, offset, size - offset)) > -1)
                        {
                            string data = Encoding.UTF8.GetString(buffer, startPosition, endPosition - startPosition + 1);
#if DEBUG
                            _plugin.Log(data.TrimEnd());
#endif
                            toAct(data.TrimEnd());
                            offset = startPosition = endPosition + 1;
                            if (offset == size)
                                break;
                        }
                        if (size > offset)
                        {
                            Buffer.BlockCopy(buffer, offset, buffer, 0, size - offset);
                            offset = size - offset;
                        }
                        else
                        {
                            offset = 0;
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    _plugin.Log("error:" + e.Message);
                }
                catch (Exception e)
                {
                    _plugin.Log("error:" + e.Message);
                }
                finally
                {
                    _plugin.Log("close connect");
                    if (stream != null)
                        stream.Close();
                    if (_client != null)
                        _client.Close();
                }
                _plugin.Log("wait...(5s)");
                for (var i = 0; i < 10; i++)
                {
                    if (_cancen.IsCancellationRequested)
                        break;
                    await Task.Delay(500);
                }
            }
            _cancen = null;
            Active = false;
            OnClose();
        }
        public void Stop()
        {
            if(_cancen != null)
                _cancen.Cancel();
        }
        private void toAct(string txt)
        {
            try {
                ActGlobals.oFormActMain.GlobalTimeSorter++;
                ActGlobals.oFormActMain.LastKnownTime = ActGlobals.oFormActMain.GetDateTimeFromLog(txt);
                ActGlobals.oFormActMain.ParseRawLogLine(false, ActGlobals.oFormActMain.LastKnownTime, txt);
            }
            catch (Exception e){
                _plugin.Log("error:" + e.Message);
            }
        }
    }
}