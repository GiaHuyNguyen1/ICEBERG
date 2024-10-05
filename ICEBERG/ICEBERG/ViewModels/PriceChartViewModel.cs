using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using SkiaChart;
using SkiaChart.Charts;
using SkiaSharp;
using ICEBERG.Services;

public class PriceChartViewModel : BindableObject
{
    private readonly IWebSocketService _webSocketService;
    private List<float> _bitcoinPrices;
    private int _priceCounter = 0;
    public Chart<LineChart> Chart { get; set; }

    public SKColor GridColor { get; set; }
    public float LabelTextSize { get; set; }
    public float LegendItemSpacing { get; set; }

    public PriceChartViewModel()
    {
        _webSocketService = WebSocketServiceByWebSocketSharp.Instance;
        _bitcoinPrices = new List<float>();

        // Initialize chart with default values
        Chart = new Chart<LineChart>(GenerateLineCharts())
        {
            YTitle = "Bitcoin Price",
            XTitle = "Time",
        };

        GridColor = SKColors.LightBlue;

        switch (Device.RuntimePlatform)
        {
            case Device.WPF:
            case Device.GTK:
            case Device.macOS:
            case Device.UWP:
                {
                    LabelTextSize = 15f;
                    LegendItemSpacing = 20f;
                    break;
                }
            default:
                {
                    LabelTextSize = 30f;
                    LegendItemSpacing = 40f;
                    break;
                }
        }

        // Subscribe to WebSocket event to receive price updates
        _webSocketService.OnBitcoinPriceUpdated += OnBitcoinPriceUpdated;

        // Start the WebSocket server (or connect to it)
        _webSocketService.StartServer();
    }

    private void OnBitcoinPriceUpdated(float newPrice)
    {
        // Update the price list with the new price
        _bitcoinPrices.Add(newPrice);

        // Keep the list limited to the last 500 prices for better chart performance
        if (_bitcoinPrices.Count > 500)
        {
            _bitcoinPrices.RemoveAt(0);
        }

        // Refresh the chart data
        UpdateChart();
    }

    private void UpdateChart()
    {
        // Create a new chart instance with the updated bitcoin prices
        Chart = new Chart<LineChart>(new List<LineChart>
        {
            new LineChart(GetXValues(), _bitcoinPrices)
            {
                ChartColor = SKColors.Orange,
                ChartName = "Bitcoin Real-Time Price",
                ShowPoints = true
            }
        })
        {
            YTitle = "Bitcoin Price",
            XTitle = "Time",
        };
        GridColor = SKColors.LightBlue;
        LabelTextSize = LabelTextSize;
        LegendItemSpacing = LegendItemSpacing;

        // Notify UI that Chart has changed (if necessary)
        OnPropertyChanged(nameof(Chart));
    }

    private IEnumerable<float> GetXValues()
    {
        for (int i = 0; i < _bitcoinPrices.Count; i++)
        {
            yield return i + 1;
        }
    }

    private IEnumerable<LineChart> GenerateLineCharts()
    {
        // Initially, display a placeholder chart with no data
        return new List<LineChart>
        {
            new LineChart(new float[] { 0 }, new float[] { 0 })
            {
                ChartColor = SKColors.Gray,
                ChartName = "Waiting for Data",
                ShowPoints = true
            }
        };
    }

    public void Dispose()
    {
        // Unsubscribe from the WebSocket event and stop the server when disposing the ViewModel
        _webSocketService.OnBitcoinPriceUpdated -= OnBitcoinPriceUpdated;
        _webSocketService.StopServer();
    }
}
