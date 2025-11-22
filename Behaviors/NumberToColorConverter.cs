using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace UiDesktopApp1.Behaviors
{
    public class NumberToColorConverter : IValueConverter
    {
        public static readonly NumberToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Chuyển đổi giá trị sang số double để so sánh
            if (double.TryParse(value?.ToString(), out double number))
            {
                if (number > 0)
                    return Brushes.ForestGreen; // Số dương: Màu xanh lá (Thừa hàng)

                if (number < 0)
                    return Brushes.Red;         // Số âm: Màu đỏ (Thiếu hàng)
            }

            // Số 0 hoặc không phải số: Màu mặc định (đen/xám tùy theme)
            // Trả về DependencyProperty.UnsetValue để control dùng màu mặc định của nó
            return System.Windows.DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}