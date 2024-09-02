using ICEBERG.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ICEBERG.Services
{
    public interface IWebSocketService
    {
        void StartServer(); // Khởi động server WebSocket
        void StopServer();  // Dừng server WebSocket
        Dictionary<string, SlaveInfo> GetAllSlaveLocations(); // Lấy danh sách vị trí của tất cả các slave
    }
}
