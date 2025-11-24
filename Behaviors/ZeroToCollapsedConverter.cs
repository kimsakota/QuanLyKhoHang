using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UiDesktopApp1.Behaviors
{
    public class ZeroToCollapsedConverter : IValueConverter
    {
        public static readonly ZeroToCollapsedConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = false;

            // Kiểm tra giá trị số
            if (value is int intValue)
                hasValue = intValue > 0;
            else if (value is long longValue)
                hasValue = longValue > 0;
            else if (value is double doubleValue)
                hasValue = doubleValue > 0;
            else if (value is decimal decimalValue)
                hasValue = decimalValue > 0;

            // Kiểm tra tham số "Inverse"
            bool isInverse = parameter is string str && str.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

            if (isInverse)
            {
                // Chế độ Đảo ngược: Có giá trị (>0) thì ẨN, Bằng 0 thì HIỆN
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                // Chế độ Mặc định: Có giá trị (>0) thì HIỆN, Bằng 0 thì ẨN
                return hasValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}