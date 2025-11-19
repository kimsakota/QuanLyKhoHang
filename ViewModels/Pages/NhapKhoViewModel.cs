using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiDesktopApp1.Models;
using UiDesktopApp1.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace UiDesktopApp1.ViewModels.Pages
{
    public partial class NhapKhoViewModel : ObservableObject, INavigationAware
    {
        private readonly INavigationService _navigationService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        [ObservableProperty]
        private DateTime _ngayNhap;
        public NhapKhoViewModel (INavigationService navigationService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _navigationService = navigationService;
            _dbContextFactory = dbContextFactory;
            InitCharts();
        }

        public Task OnNavigatedToAsync()
        {
            NgayNhap = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }

        [RelayCommand]
        public void AddCustomer()
        {
            _navigationService.Navigate(typeof(SanPhamPage));
        }


        public ISeries[] RevenueSeries { get; set; }
        public Axis[] RevenueXAxes { get; set; }
        public Axis[] RevenueYAxes { get; set; }

        // --- BIỂU ĐỒ 2: CƠ CẤU CHI PHÍ (Tròn) ---
        public ISeries[] CostPieSeries { get; set; }

        private void InitCharts()
        {
            // 1. Cấu hình Biểu đồ Cột (Revenue vs Cost)
            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Doanh thu",
                    Values = new double[] { 120, 150, 180, 140, 200, 230 },
                    Fill = new SolidColorPaint(SKColors.CornflowerBlue) // Màu xanh
                },
                new ColumnSeries<double>
                {
                    Name = "Chi phí",
                    Values = new double[] { 80, 90, 100, 95, 110, 120 },
                    Fill = new SolidColorPaint(SKColors.IndianRed) // Màu đỏ
                }
            };

            RevenueXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6" },
                    LabelsRotation = 0
                }
            };

            RevenueYAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = (value) => value.ToString("N0") + " tr", // Định dạng: 100 tr
                }
            };

            // 2. Cấu hình Biểu đồ Tròn (Cost Structure)
            CostPieSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Values = new double[] { 60 },
                    Name = "Nhập hàng",
                    InnerRadius = 50 // Tạo hiệu ứng Donut (rỗng giữa)
                },
                new PieSeries<double>
                {
                    Values = new double[] { 25 },
                    Name = "Nhân sự"
                },
                new PieSeries<double>
                {
                    Values = new double[] { 10 },
                    Name = "Marketing"
                },
                new PieSeries<double>
                {
                    Values = new double[] { 5 },
                    Name = "Khác"
                }
            };
        }
    }
}
