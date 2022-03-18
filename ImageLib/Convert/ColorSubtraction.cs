using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLib
{
    public static partial class ImageOperator
    {
        public static Image<Rgb24> ColorSubtraction(Image<Rgb24> image, int dev)
        {
            Rgb24[] imgBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] devBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            int th = 256 / dev;

            Parallel.For(0, imgBytes.Length, _parallelOptions, (i) =>
            {
                devBytes[i].R = (byte)(imgBytes[i].R / th * th + th / 2);
                devBytes[i].G = (byte)(imgBytes[i].G / th * th + th / 2);
                devBytes[i].B = (byte)(imgBytes[i].B / th * th + th / 2);
            });

            return Image.LoadPixelData(devBytes, image.Width, image.Height);
        }
    }
}
