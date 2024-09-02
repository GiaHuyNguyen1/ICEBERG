using System;
using WebSocketSharp.Server;
using WebSocketSharp;
using ICEBERG.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICEBERG.Services
{
    public class Echo : WebSocketBehavior
    {
        private static Dictionary<string, SlaveInfo> slaveLocations = new Dictionary<string, SlaveInfo>();

        public static event Action<SlaveInfo> OnNewSlaveReceived;

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                var slaveInfo = JsonConvert.DeserializeObject<SlaveInfo>(e.Data);

                if (slaveInfo != null && !string.IsNullOrEmpty(slaveInfo.ID))
                {
                    // Kiểm tra xem slave đã tồn tại chưa, nếu chưa thì phát sự kiện
                    bool isNewSlave = !slaveLocations.ContainsKey(slaveInfo.ID);
                    slaveLocations[slaveInfo.ID] = slaveInfo;

                    Console.WriteLine($"Received location from {slaveInfo.Name} (ID: {slaveInfo.ID}): ({slaveInfo.Latitude}, {slaveInfo.Longitude})");

                    // Phát sự kiện khi có slave mới
                    if (isNewSlave)
                    {
                        OnNewSlaveReceived?.Invoke(slaveInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing message: {ex.Message}");
            }
        }

        public static Dictionary<string, SlaveInfo> GetSlaveLocations()
        {
            return slaveLocations;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
        }
    }

    public class WebSocketServiceByWebSocketSharp : IWebSocketService
    {
        private WebSocketServer _wssv;
        private static readonly Lazy<WebSocketServiceByWebSocketSharp> _instance =
            new Lazy<WebSocketServiceByWebSocketSharp>(() => new WebSocketServiceByWebSocketSharp());

        private WebSocketServiceByWebSocketSharp()
        {
            // Đăng ký sự kiện OnNewSlaveReceived từ Echo
            Echo.OnNewSlaveReceived += SlaveReceivedHandler;
        }

        public static WebSocketServiceByWebSocketSharp Instance => _instance.Value;

        public void StartServer()
        {
            if (_wssv == null || !_wssv.IsListening)
            {
                _wssv = new WebSocketServer("ws://localhost:49152");
                _wssv.AddWebSocketService<Echo>("/Echo");
                _wssv.Start();
                Console.WriteLine("WebSocket server started at ws://localhost:49152/Echo");
            }
            else
            {
                Console.WriteLine("WebSocket server is already running.");
            }
        }

        public void StopServer()
        {
            if (_wssv != null && _wssv.IsListening)
            {
                _wssv.Stop();
                Console.WriteLine("WebSocket server stopped.");
            }
            else
            {
                Console.WriteLine("WebSocket server is not running.");
            }
        }

        public Dictionary<string, SlaveInfo> GetAllSlaveLocations()
        {
            return Echo.GetSlaveLocations();
        }

        // Hàm xử lý khi có slave mới
        private void SlaveReceivedHandler(SlaveInfo slaveInfo)
        {
            // Logic bổ sung nếu cần
        }
    }
}
