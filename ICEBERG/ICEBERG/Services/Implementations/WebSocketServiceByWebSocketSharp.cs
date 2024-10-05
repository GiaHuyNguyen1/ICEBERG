using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using ICEBERG.Models;

namespace ICEBERG.Services
{
    public class BitcoinTransaction
    {
        public string ClientID { get; set; }
        public string Action { get; set; }  // "buy" or "sell"
        public float Amount { get; set; }  // Số lượng Bitcoin
        public float Price { get; set; }   // Giá của Bitcoin tại thời điểm giao dịch
    }

    public class Echo : WebSocketBehavior
    {
        private static float bitcoinPrice = 20000;  // Giá Bitcoin khởi điểm
        private static List<double> priceHistory = new List<double>(); // Lưu lịch sử giá
        private static readonly object lockObj = new object(); // Đảm bảo an toàn khi truy cập đồng thời

        public static Action<float> OnUpdatePrice {  get; set; }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                // Giải mã thông tin giao dịch từ client
                var transaction = JsonConvert.DeserializeObject<BitcoinTransaction>(e.Data);

                if (transaction != null)
                {
                    Console.WriteLine($"{transaction.ClientID} performed a {transaction.Action} transaction of {transaction.Amount} BTC at ${transaction.Price}");

                    // Cập nhật giá Bitcoin, phải đảm bảo đồng bộ (thread-safe) khi cập nhật giá
                    lock (lockObj)
                    {
                        UpdateBitcoinPrice(transaction);
                    }

                    // Lưu giá vào lịch sử
                    priceHistory.Add(bitcoinPrice);

                    // Tạo thông điệp chứa giá Bitcoin mới
                    string responseMessage = JsonConvert.SerializeObject(new { BitcoinPrice = bitcoinPrice });

                    // Gửi giá Bitcoin mới tới tất cả các clients
                    Sessions.Broadcast(responseMessage);

                    OnUpdatePrice?.Invoke(bitcoinPrice); 

                    Console.WriteLine($"New Bitcoin price broadcasted: ${bitcoinPrice}");
                }
                else
                {
                    Console.WriteLine("Received invalid transaction data.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void UpdateBitcoinPrice(BitcoinTransaction transaction)
        {
            if (transaction.Action == "buy")
            {
                bitcoinPrice += transaction.Amount * 10;  // Mua làm tăng giá
            }
            else if (transaction.Action == "sell")
            {
                bitcoinPrice -= transaction.Amount * 10;  // Bán làm giảm giá
            }

            // Giới hạn giá trong khoảng hợp lý (chỉ là một ví dụ)
            if (bitcoinPrice < 10000) bitcoinPrice = 10000;
            if (bitcoinPrice > 50000) bitcoinPrice = 50000;
        }

        // Nếu bạn có thêm logic quản lý slave
        public static Dictionary<string, SlaveInfo> GetSlaveLocations()
        {
            // Trả về thông tin các slave hiện có
            return new Dictionary<string, SlaveInfo>();
        }

        public static event Action<SlaveInfo> OnNewSlaveReceived;
    }
    public class WebSocketServiceByWebSocketSharp : IWebSocketService
    {
        private WebSocketServer _wssv;
        private WebSocket _clientWebSocket; // WebSocket phía client
        public event Action<float> OnBitcoinPriceUpdated;

        private static readonly Lazy<WebSocketServiceByWebSocketSharp> _instance =
            new Lazy<WebSocketServiceByWebSocketSharp>(() => new WebSocketServiceByWebSocketSharp());

        public static WebSocketServiceByWebSocketSharp Instance => _instance.Value;

        // Khởi động WebSocket server
        public void StartServer()
        {
            if (_wssv == null || !_wssv.IsListening)
            {
                _wssv = new WebSocketServer("ws://192.168.1.13:49152");
                _wssv.AddWebSocketService<Echo>("/Echo");
                _wssv.Start();

                Echo.OnUpdatePrice -= OnBitcoinPriceUpdated;
                Echo.OnUpdatePrice += OnBitcoinPriceUpdated;

                Console.WriteLine("WebSocket server started at ws://192.168.1.13:49152/Echo");
            }
            else
            {
                Console.WriteLine("WebSocket server is already running.");
            }
        }

        // Dừng WebSocket server
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

        // Khởi động kết nối WebSocket từ phía client
        public void StartClientConnection()
        {
            _clientWebSocket = new WebSocket("ws://192.168.1.13:49152/Echo");
            _clientWebSocket.OnMessage += (sender, e) =>
            {
                try
                {
                    // Nhận giá Bitcoin cập nhật từ server
                    var data = JsonConvert.DeserializeObject<dynamic>(e.Data);
                    float newPrice = (float)data.BitcoinPrice;

                    // Kích hoạt sự kiện để cập nhật giá Bitcoin mới
                    OnBitcoinPriceUpdated?.Invoke(newPrice);  // Bắn sự kiện giá Bitcoin mới
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
            };

            _clientWebSocket.Connect();
        }

        // Dừng kết nối WebSocket phía client
        public void StopClientConnection()
        {
            if (_clientWebSocket != null && _clientWebSocket.IsAlive)
            {
                _clientWebSocket.Close();
            }
        }

        // Gửi giao dịch Bitcoin từ phía client
        public async Task SendBitcoinTransactionAsync(BitcoinTransaction transaction)
        {
            if (_clientWebSocket != null && _clientWebSocket.IsAlive)
            {
                string message = JsonConvert.SerializeObject(transaction);
                _clientWebSocket.Send(message);
            }
        }

        // Lấy danh sách vị trí của tất cả các slave
        public Dictionary<string, SlaveInfo> GetAllSlaveLocations()
        {
            return Echo.GetSlaveLocations();
        }
    }
}
