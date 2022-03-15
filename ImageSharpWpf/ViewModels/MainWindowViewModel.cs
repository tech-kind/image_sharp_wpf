using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageSharpWpf.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private BitmapSource _srcImage;

        public BitmapSource SrcImage
        {
            get { return _srcImage; }
            set { SetProperty(ref _srcImage, value); }
        }


        private BitmapSource _dstImage;

        public BitmapSource DstImage
        {
            get { return _dstImage; }
            set { SetProperty(ref _dstImage, value); }
        }

        public DelegateCommand FileSelectCommand { get; private set; }

        public MainWindowViewModel()
        {
            FileSelectCommand = new DelegateCommand(FileSelection);
        }

        private void FileSelection()
        {
            var dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                var file = dialog.FileName;
                using Image<Rgb24> image = Image.Load<Rgb24>(file);
                byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
                image.CopyPixelDataTo(pixelBytes);

                var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Rgb24, null, pixelBytes, image.Width * 3);
                SrcImage = bmp;
            }
        }
    }
}
