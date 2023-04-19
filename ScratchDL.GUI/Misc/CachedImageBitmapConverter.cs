using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;

namespace ScratchDL.GUI
{
    public class CachedImageBitmapConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is string rawUri && targetType.IsAssignableFrom(typeof(Bitmap)) && ImageCache.TryGetImage(rawUri,out var image))
            {
                return image;
            }

            throw new NotSupportedException();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
