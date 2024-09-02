using ICEBERG.Models;
using ICEBERG.Services;
using ICEBERG.Views;
using System;
using System.Collections.ObjectModel;
using Xamarin.Forms;

namespace ICEBERG
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<SlaveInfo> _slaves;

        private IWebSocketService _webSocketService;

        public MainPage()
        {
            InitializeComponent();

            _webSocketService = WebSocketServiceByWebSocketSharp.Instance;

            _slaves = new ObservableCollection<SlaveInfo>();

            SlavesListView.ItemsSource = _slaves;

            // Đăng ký sự kiện để cập nhật khi có slave mới
            Echo.OnNewSlaveReceived += OnNewSlaveReceived;

            LoadSlaves();
        }

        private void OnNewSlaveReceived(SlaveInfo newSlave)
        {
            // Sử dụng MainThread để cập nhật giao diện do ObservableCollection cần được cập nhật trên MainThread
            Device.BeginInvokeOnMainThread(() =>
            {
                _slaves.Add(newSlave);
            });
        }

        private void LoadSlaves()
        {
            var slaves = _webSocketService.GetAllSlaveLocations();
            _slaves.Clear();
            foreach (var slave in slaves.Values)
            {
                _slaves.Add(slave);
            }
        }

        private void OnSlaveSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is SlaveInfo selectedSlave)
            {
                DisplayAlert("Slave Selected", $"Name: {selectedSlave.Name}\nID: {selectedSlave.ID}", "OK");
                SlavesListView.SelectedItem = null;
            }
        }

        private void StartServerButton_Clicked(object sender, EventArgs e)
        {
            _webSocketService.StartServer();
            LoadSlaves();
        }

        private async void NavigationObservationSlaves(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ObservationSlaves());
        }
    }
}
