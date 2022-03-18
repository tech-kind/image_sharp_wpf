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
        public static Image<L8> Grayscale(Image<Rgb24> color)
        {
            Rgb24[] colorBytes = new Rgb24[color.Width * color.Height];
            L8[] grayBytes = new L8[color.Width * color.Height];
            color.CopyPixelDataTo(colorBytes);

            Parallel.For(0, colorBytes.Length, _parallelOptions, (i) =>
            {
                grayBytes[i].PackedValue = GetGray(colorBytes[i].R, colorBytes[i].G, colorBytes[i].B);
            });

            return Image.LoadPixelData(grayBytes, color.Width, color.Height);
        }

        private static byte GetGray(byte R, byte G, byte B)
        {
            return (byte)(R * 0.2126 + G * 0.7152 + B * 0.0722);
        }
    }
}
