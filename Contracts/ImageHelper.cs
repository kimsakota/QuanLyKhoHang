using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace UiDesktopApp1.Contracts
{
    public static class ImageHelper
    {
        private static readonly BitmapImage DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/logo-image.png", UriKind.Absolute));
        public static BitmapImage? LoadBitmap(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return DefaultImage;

                if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(path, UriKind.Absolute);
                    // Nếu là ảnh mặc định, trả về thể hiện (instance) đã tải
                    if (uri == DefaultImage.UriSource)
                        return DefaultImage;

                    return new BitmapImage(uri);
                }

                if(File.Exists(path)) return new BitmapImage(new Uri(Path.GetFullPath(path), UriKind.Absolute));

                return DefaultImage;
            }
            catch
            {
                return DefaultImage;
            }
        }
    }
}
