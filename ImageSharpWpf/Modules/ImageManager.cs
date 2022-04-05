using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessagePipe;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static ImageSharpWpf.Utils.TopicName;
using Microsoft.Extensions.Hosting;
using ImageLib;

namespace ImageSharpWpf.Modules
{
    public class ImageManager : BackgroundService
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
            _subscriber.Subscribe(IMAGE_MANAGER_SUBTRACTION, Subtraction);
            _subscriber.Subscribe(IMAGE_MANAGER_AVERAGE_POOLING, AveragePooling);
            _subscriber.Subscribe(IMAGE_MANAGER_MAX_POOLING, MaxPooling);
            _subscriber.Subscribe(IMAGE_MANAGER_GAUSSIAN_FILTER, GaussianFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_MEDIAN_FILTER, MedianFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_SMOOTH_FILTER, SmoothFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_MOTION_FILTER, MotionFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_MAXMIN_FILTER, MaxMinFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_DIFF_FILTER, DiffFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_PREWITT_FILTER, PrewittFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_SOBEL_FILTER, SobelFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_LAPLACIAN_FILTER, LaplacianFilter);
            _subscriber.Subscribe(IMAGE_MANAGER_EMBOSS_FILTER, EmbossFilter);

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
            var gray = ImageOperator.Grayscale(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, gray);
        }

        private ValueTask Threshold(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();

            var th = ImageOperator.BinaryThreshold(_srcImage, 127);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, th);
        }

        private ValueTask OtsuThreshold(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var th = ImageOperator.OtsuThreshold(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();
            return PublishBitmapSource(OutputType.Dst, th);
        }

        private ValueTask RGBToBGR(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var dst = ImageOperator.RGB2BGR(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, dst);
        }

        private ValueTask HSV(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var hsv = ImageOperator.RGB2HSV(_srcImage);
            var inverse = ImageOperator.InverseHue(hsv);
            var rgb = ImageOperator.HSV2RGB(inverse);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, rgb);
        }

        private ValueTask Subtraction(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var dev = ImageOperator.ColorSubtraction(_srcImage, 4);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, dev);
        }

        private ValueTask AveragePooling(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var ave = ImageOperator.AveragePooling(_srcImage, (8, 8), (0, 0), (8, 8));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, ave);
        }

        private ValueTask MaxPooling(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var ave = ImageOperator.MaxPooling(_srcImage, (8, 8), (0, 0), (8, 8));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, ave);
        }

        private ValueTask GaussianFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var gaussian = ImageOperator.GaussianFilter(_srcImage, (3, 3));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, gaussian);
        }

        private ValueTask MedianFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var median = ImageOperator.MedianFilter(_srcImage, (3, 3));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, median);
        }

        private ValueTask SmoothFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var smooth = ImageOperator.SmoothingFilter(_srcImage, (5, 5));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, smooth);
        }

        private ValueTask MotionFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var motion = ImageOperator.MotionFilter(_srcImage, (5, 5));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, motion);
        }

        private ValueTask MaxMinFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var motion = ImageOperator.MaxMinFilter(_srcImage, (5, 5));

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, motion);
        }

        private ValueTask DiffFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var diff = ImageOperator.DiffFilter(_srcImage, ImageOperator.DiffMode.x);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, diff);
        }

        private ValueTask PrewittFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var prewitt = ImageOperator.PrewittFilter(_srcImage, (5, 5), ImageOperator.DiffMode.x);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, prewitt);
        }

        private ValueTask SobelFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var sobel = ImageOperator.SobelFilter(_srcImage, (3, 3), ImageOperator.DiffMode.x);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, sobel);
        }

        private ValueTask LaplacianFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var laplacian = ImageOperator.LaplacianFilter(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, laplacian);
        }

        private ValueTask EmbossFilter(string message, CancellationToken token)
        {
            if (_srcImage == null) return ValueTask.FromException(new Exception());

            _stopWatch.Restart();
            var emboss = ImageOperator.EmbossFilter(_srcImage);

            _stopWatch.Stop();
            PublishElapsedTime();

            return PublishBitmapSource(OutputType.Dst, emboss);
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

    }
}
