using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UiDesktopApp1.Behaviors
{
    public class NumberVnConverter : IValueConverter
    {
        public static readonly NumberVnConverter Instance = new();

        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (decimal.TryParse(value.ToString(), out var amount))
                return string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}", amount); // chỉ N0, không kí hiệu
            return string.Empty;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString() ?? "";
            // giữ lại chữ số
            var digits = Regex.Replace(s, "[^0-9]", "");
            if (digits.Length == 0) return 0m;
            if (decimal.TryParse(digits, out var result))
                return result;
            return 0m;
        }
    }
}
