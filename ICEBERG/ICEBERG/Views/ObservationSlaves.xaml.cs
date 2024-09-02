using ICEBERG.Models;
using ICEBERG.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ICEBERG.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ObservationSlaves : ContentPage
    {
        // Lấy instance duy nhất của WebSocketServiceByWebSocketSharp
        private IWebSocketService _webSocketService;

        public ObservationSlaves()
        {
            InitializeComponent();
            _webSocketService = WebSocketServiceByWebSocketSharp.Instance; // Sử dụng instance duy nhất

            var slaveLocations = _webSocketService.GetAllSlaveLocations();

            LoadMap(slaveLocations); // Tải bản đồ khi khởi tạo trang
        }
        private void LoadMap(Dictionary<string, SlaveInfo> slaveInfos)
        {
            // Chuyển đổi danh sách SlaveInfo thành chuỗi JSON an toàn cho JavaScript
            var markersData = string.Join(", ", slaveInfos.Select(slave =>
                $"{{latitude: {slave.Value.Latitude.ToString().Replace(",", ".")}, longitude: {slave.Value.Longitude.ToString().Replace(",", ".")}, name: '{slave.Value.Name}'}}"));

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='initial-scale=1.0, user-scalable=no' />
    <style>
        html, body, #map {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
        }}
    </style>
    <link rel='stylesheet' href='https://unpkg.com/leaflet/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet/dist/leaflet.js'></script>
    <script>
        let map;
        let markers = [];

        document.addEventListener('DOMContentLoaded', function() {{
            loadMap();
        }});

        function loadMap() {{
            try {{
                // Khởi tạo bản đồ tại vị trí mặc định
                map = L.map('map').setView([10.762622, 106.660172], 13);
                L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
                    maxZoom: 19,
                    attribution: '© OpenStreetMap contributors'
                }}).addTo(map);

                // Gọi hàm để thêm các marker của các slave
                addMarkers([{markersData}]);
            }} catch (error) {{
                console.error('Error loading map:', error);
                alert('Error loading the map. Please check your connection and try again.');
            }}
        }}

        function clearMarkers() {{
            // Xóa tất cả các marker trên bản đồ
            markers.forEach(marker => marker.remove());
            markers = [];
        }}

        function addMarkers(slaves) {{
            // Thêm marker cho từng slave từ danh sách đã được truyền vào
            clearMarkers(); // Xóa các marker cũ trước khi thêm marker mới
            slaves.forEach(slave => {{
                if (slave.latitude && slave.longitude) {{
                    const location = [slave.latitude, slave.longitude];
                    const marker = L.marker(location).addTo(map)
                        .bindPopup(`<b>${{slave.name}}</b>`)
                        .openPopup();
                    markers.push(marker);
                }}
            }});
        }}
    </script>
</head>
<body>
    <div id='map' style='width: 100%; height: 100vh;'></div>
</body>
</html>";

            // Đặt mã HTML vào WebView
            MapWebView.Source = new HtmlWebViewSource
            {
                Html = html
            };
        }


        // Hàm xử lý khi nhấn nút Update Slave Locations
        private async void OnUpdateSlaveLocationsClicked(object sender, EventArgs e)
        {
            // Lấy danh sách vị trí các slave từ WebSocket Service
            var slaveLocations = _webSocketService.GetAllSlaveLocations();

            try
            {
                // Xóa tất cả marker cũ trên bản đồ
                var clearResult = await MapWebView.EvaluateJavaScriptAsync("clearMarkers();");

                // Kiểm tra kết quả từ hàm JavaScript
                if (clearResult != null)
                {
                    Console.WriteLine("Markers cleared successfully.");
                }

                // Thêm các marker mới cho từng slave
                foreach (var slave in slaveLocations.Values)
                {
                    // Chuyển thông tin mỗi slave thành JavaScript để thêm marker
                    string addMarkerJs = $"addMarker({slave.Latitude.ToString().Replace(",", ".")}, {slave.Longitude.ToString().Replace(",", ".")}, '{slave.Name}');";

                    // Gọi JavaScript để thêm marker và đợi kết quả
                    var addResult = await MapWebView.EvaluateJavaScriptAsync(addMarkerJs);

                    // Kiểm tra xem JavaScript đã thực thi thành công hay chưa
                    if (addResult != null)
                    {
                        Console.WriteLine($"Marker added for {slave.Name} at ({slave.Latitude}, {slave.Longitude}).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating slave locations: {ex.Message}");
            }
        }

    }
}