using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessagePipe;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using static ImageSharpWpf.Utils.TopicName;

namespace ImageSharpWpf.Modules
{
    public interface IImageManager
    {

    }

    public class ImageManager : IImageManager
    {
        private Stopwatch _stopWatch = new Stopwatch();
        private enum OutputType
        {
            Src = 0,
            Dst,
        }

        private Image<Rgb24>? _srcImage;

        private readonly IAsyncPublisher<string, BitmapSource> _bitmapPublisher;
        private readonly IAsyncPublisher<string, string> _strPublisher;
        private readonly IAsyncSubscriber<string, string> _subscriber;

        public ImageManager(IAsyncPublisher<string, BitmapSource> bmpPub, IAsyncPublisher<string, string> strPub,
            IAsyncSubscriber<string, string> sub)
        {
            _bitmapPublisher = bmpPub;
            _strPublisher = strPub;
            _subscriber = sub;

            _subscriber.Subscribe(IMAGE_MANAGER_SET_IMAGE, SetImage);
            _subscriber.Subscribe(IMAGE_MANAGER_THRESHOLD, Threshold);
            _subscriber.Subscribe(IMAGE_MANAGER_GRAY_SCALE, GrayScaleImage);
        }

        private ValueTask SetImage(string path, CancellationToken token)
        {
            _srcImage = Image.Load<Rgb24>(path);
            return PublishBitmapSource(OutputType.Src, _srcImage);
        }

        private ValueTask GrayScaleImage(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();

            var gray = ConvertGray(_srcImage);

            // var gray = _srcImage.Clone(x => x.Grayscale());
            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, gray);
        }

        private ValueTask Threshold(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();

            var gray = ConvertGray(_srcImage);
            int height = _srcImage.Height;
            gray.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref L8 pixel = ref pixelRow[x];
                        if (pixel.PackedValue > 128)
                        { 
                            pixel.PackedValue = 255;
                        }
                        else
                        {
                            pixel.PackedValue = 0;
                        }
                    }
                }
            });

            // var gray = _srcImage.Clone(x => x.Grayscale());
            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, gray);
        }

        private ValueTask PublishBitmapSource(OutputType type, Image<Rgb24> image)
        {
            var bmp = ConvertFromImageToBitmapSource(image);

            if (bmp == null) return ValueTask.FromException(new Exception());

            if (type == OutputType.Src)
            {
                return _bitmapPublisher.PublishAsync(IMAGE_MANAGER_SRC_IMAGE, bmp);
            }
            else
            {
                return _bitmapPublisher.PublishAsync(IMAGE_MANAGER_DST_IMAGE, bmp);
            }
        }

        private ValueTask PublishBitmapSource(OutputType type, Image<L8> image)
        {
            var bmp = ConvertFromImageToBitmapSource(image);

            if (bmp == null) return ValueTask.FromException(new Exception());

            if (type == OutputType.Src)
            {
                return _bitmapPublisher.PublishAsync(IMAGE_MANAGER_SRC_IMAGE, bmp);
            }
            else
            {
                return _bitmapPublisher.PublishAsync(IMAGE_MANAGER_DST_IMAGE, bmp);
            }
        }

        private void PublishElapsedTime()
        {
            var str = $"{_stopWatch.ElapsedMilliseconds}ms";
            _strPublisher.Publish(IMAGE_MANAGER_ELAPSED_TIME, str);
        }

        private BitmapSource? ConvertFromImageToBitmapSource(Image<Rgb24> image)
        {
            byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
            image.CopyPixelDataTo(pixelBytes);

            var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Rgb24, null, pixelBytes, image.Width * 3);

            return bmp;
        }

        private BitmapSource? ConvertFromImageToBitmapSource(Image<L8> image)
        {
            byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<L8>()];
            image.CopyPixelDataTo(pixelBytes);

            var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Gray8, null, pixelBytes, image.Width);

            return bmp;
        }

        private Image<L8> ConvertGray(Image<Rgb24> color)
        {
            var gray = new Image<L8>(_srcImage.Width, _srcImage.Height);
            int height = _srcImage.Height;
            _srcImage.ProcessPixelRows(gray, (srcAccessor, grayAccessor) =>
            {
                for (int i = 0; i < height; i++)
                {
                    Span<Rgb24> srcPixelRow = srcAccessor.GetRowSpan(i);
                    Span<L8> grayPixelRow = grayAccessor.GetRowSpan(i);

                    for (int x = 0; x < srcPixelRow.Length; x++)
                    {
                        var gray_value = srcPixelRow[x].R * 0.2126 + srcPixelRow[x].G * 0.7152
                            + srcPixelRow[x].B * 0.0722;
                        if (gray_value > byte.MaxValue) gray_value = byte.MaxValue;
                        if (gray_value < byte.MinValue) gray_value = byte.MinValue;
                        grayPixelRow[x].PackedValue = (byte)gray_value;
                    }
                }
            });

            return gray;
        }
    }
}
