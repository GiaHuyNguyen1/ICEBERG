using ICEBERG.Services;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ICEBERG.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PriceChartPage : ContentPage
    {
        public PriceChartPage()
        {
            InitializeComponent();

            BindingContext = new PriceChartViewModel();
        }
    }
}