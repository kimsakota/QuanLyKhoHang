using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace UiDesktopApp1.Behaviors
{
    public class StringToColorConverter : IValueConverter
    {
        // Singleton instance để dùng x:Static cho tiện
        public static readonly StringToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                // Màu sắc kiểu "Badge" (nền nhạt, dịu mắt)

                // Nếu là Nhập kho -> Màu Xanh lá nhạt
                if (type.Contains("Nhập", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1E7DD"));

                // Nếu là Xuất kho -> Màu Vàng cam nhạt
                if (type.Contains("Xuất", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));

                // --- THÊM MỚI ---
                // Nếu là Kiểm kê -> Màu Xanh dương nhạt
                if (type.Contains("Kiểm kê", StringComparison.OrdinalIgnoreCase))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFE2FF"));
            }

            // Mặc định trong suốt
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}