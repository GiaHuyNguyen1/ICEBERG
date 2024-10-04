using ICEBERG.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ICEBERG.Services
{
    public interface IWebSocketService
    {
        void StartServer(); // Khởi động server WebSocket
        void StopServer();  // Dừng server WebSocket
        void StartClientConnection(); 
        void StopClientConnection();  
        Dictionary<string, SlaveInfo> GetAllSlaveLocations(); // Lấy danh sách vị trí của tất cả các slave


        // Gửi giao dịch mua/bán Bitcoin từ client
        Task SendBitcoinTransactionAsync(BitcoinTransaction transaction);

        // Đăng ký sự kiện nhận được giá Bitcoin mới từ server
        event Action<float> OnBitcoinPriceUpdated;
    }
}
