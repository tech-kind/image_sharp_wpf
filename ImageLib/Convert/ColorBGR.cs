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
        public static Image<Rgb24> RGB2BGR(Image<Rgb24> image)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] bgrBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            Parallel.For(0, rgbBytes.Length, _parallelOptions, (i) =>
            {
                bgrBytes[i].R = rgbBytes[i].B;
                bgrBytes[i].G = rgbBytes[i].G;
                bgrBytes[i].B = rgbBytes[i].R;
            });

            return Image.LoadPixelData(bgrBytes, image.Width, image.Height);
        }
    }
}
