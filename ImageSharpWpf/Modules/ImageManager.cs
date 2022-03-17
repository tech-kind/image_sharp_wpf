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
using Microsoft.Extensions.Hosting;
using ImageSharpWpf.Utils;

namespace ImageSharpWpf.Modules
{
    public interface IImageManager
    {

    }

    public class ImageManager : BackgroundService, IImageManager
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
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _subscriber.Subscribe(IMAGE_MANAGER_SET_IMAGE, SetImage);
            _subscriber.Subscribe(IMAGE_MANAGER_THRESHOLD, Threshold);
            _subscriber.Subscribe(IMAGE_MANAGER_GRAY_SCALE, GrayScaleImage);
            _subscriber.Subscribe(IMAGE_MANAGER_RGB_TO_BGR, RGBToBGR);
            _subscriber.Subscribe(IMAGE_MANAGER_OTSU_THRESHOLD, OtsuThreshold);
            _subscriber.Subscribe(IMAGE_MANAGER_HSV, HSV);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10, stoppingToken);
            }
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
            var gray = ImageLib.ConvertGray(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, gray);
        }

        private ValueTask Threshold(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();

            var gray = ImageLib.ConvertGray(_srcImage);
            var th = ImageLib.BinaryThreshold(gray, 127);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, th);
        }

        private ValueTask OtsuThreshold(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();

            var gray = ImageLib.ConvertGray(_srcImage);
            var th = ImageLib.OtsuThreshold(gray);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, th);
        }

        private ValueTask RGBToBGR(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var dst = ImageLib.ConvertFromRGBToBGR(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, dst);
        }

        private ValueTask HSV(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var hsv = ImageLib.ConvertFromRGBToHSV(_srcImage);
            var inverse = ImageLib.InverseHue(hsv);
            var rgb = ImageLib.ConvertFromHSVToRGB(inverse);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, rgb);
        }

        private ValueTask PublishBitmapSource(OutputType type, Image<Rgb24> image)
        {
            var bmp = ImageLib.ConvertFromImageToBitmapSource(image);

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
            var bmp = ImageLib.ConvertFromImageToBitmapSource(image);

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
        
    }
}
