using ImageSharpWpf.Modules;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static ImageSharpWpf.Utils.TopicName;

namespace ImageSharpWpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IImageManager _imageManager;
        private readonly IAsyncSubscriber<string, BitmapSource> _subscriber;
        private readonly IAsyncSubscriber<string, string> _strSubscriber;

        private BitmapSource? _srcImage;

        public BitmapSource? SrcImage
        {
            get { return _srcImage; }
            set { SetProperty(ref _srcImage, value); }
        }

        private BitmapSource? _dstImage;

        public BitmapSource? DstImage
        {
            get { return _dstImage; }
            set { SetProperty(ref _dstImage, value); }
        }

        private string? _elapsedTime;

        public string? ElapsedTime
        {
            get { return _elapsedTime; }
            set { SetProperty(ref _elapsedTime, value); }
        }

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _imageManager = serviceProvider.GetRequiredService<IImageManager>();

            _subscriber = serviceProvider.GetRequiredService<IAsyncSubscriber<string, BitmapSource>>();
            _strSubscriber = serviceProvider.GetRequiredService<IAsyncSubscriber<string, string>>();
            _subscriber.Subscribe(IMAGE_MANAGER_SRC_IMAGE, SetSrcImage);
            _subscriber.Subscribe(IMAGE_MANAGER_DST_IMAGE, SetDstImage);
            _strSubscriber.Subscribe(IMAGE_MANAGER_ELAPSED_TIME, SetElapsedTime);
        }

        private ValueTask SetSrcImage(BitmapSource bitmap, CancellationToken token)
        {
            SrcImage = bitmap;
            return ValueTask.CompletedTask;
        }

        private ValueTask SetDstImage(BitmapSource bitmap, CancellationToken token)
        {
            DstImage = bitmap;
            return ValueTask.CompletedTask;
        }

        private ValueTask SetElapsedTime(string time, CancellationToken token)
        {
            ElapsedTime = time;
            return ValueTask.CompletedTask;
        }
    }
}
