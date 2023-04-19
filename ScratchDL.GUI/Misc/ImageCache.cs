using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScratchDL.GUI
{
    public static class ImageCache
    {
        static ConditionalWeakTable<string, Bitmap> cachedImages = new();
        static KeyValuePair<string, Bitmap> latestImage = new();

        public static void AddImage(string id, Bitmap bitmap)
        {
            cachedImages.Add(id, bitmap);
            latestImage = new KeyValuePair<string, Bitmap>(id, bitmap);
        }

        public static bool TryGetImage(string id, out Bitmap? image)
        {
            if (latestImage.Key == id)
            {
                image = latestImage.Value;
                return true;
            }
            return cachedImages.TryGetValue(id, out image);
        }
    }
}
