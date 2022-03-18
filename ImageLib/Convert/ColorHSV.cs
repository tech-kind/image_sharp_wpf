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
        public static Image<Rgb48> ConvertFromRGBToHSV(Image<Rgb24> image)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb48[] hsvBytes = new Rgb48[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            Parallel.For(0, rgbBytes.Length, _parallelOptions, (i) =>
            {
                GetHSV(rgbBytes[i].R, rgbBytes[i].G, rgbBytes[i].B,
                    out ushort h, out ushort s, out ushort v);
                hsvBytes[i].R = h;
                hsvBytes[i].G = s;
                hsvBytes[i].B = v;
            });

            return Image.LoadPixelData(hsvBytes, image.Width, image.Height);
        }

        public static Image<Rgb24> ConvertFromHSVToRGB(Image<Rgb48> image)
        {
            Rgb48[] hsvBytes = new Rgb48[image.Width * image.Height];
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(hsvBytes);

            Parallel.For(0, hsvBytes.Length, _parallelOptions, (i) =>
            {
                GetRGB(hsvBytes[i].R, hsvBytes[i].G, hsvBytes[i].B,
                    out double r, out double g, out double b);
                rgbBytes[i].R = (byte)r;
                rgbBytes[i].G = (byte)g;
                rgbBytes[i].B = (byte)b;
            });

            return Image.LoadPixelData(rgbBytes, image.Width, image.Height);
        }

        public static Image<Rgb48> InverseHue(Image<Rgb48> image)
        {
            Rgb48[] hsvBytes = new Rgb48[image.Width * image.Height];
            Rgb48[] inverseBytes = new Rgb48[image.Width * image.Height];
            image.CopyPixelDataTo(hsvBytes);

            Parallel.For(0, hsvBytes.Length, _parallelOptions, (i) =>
            {
                inverseBytes[i].R = (ushort)((hsvBytes[i].R + 180) % 360);
                inverseBytes[i].G = hsvBytes[i].G;
                inverseBytes[i].B = hsvBytes[i].B;
            });

            return Image.LoadPixelData(inverseBytes, image.Width, image.Height);
        }

        /// <summary>
        /// HSV値取得
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        private static void GetHSV(byte r, byte g, byte b,
            out ushort h, out ushort s, out ushort v)
        {
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            h = 0;

            if (min == max)
            {
                h = 0;
            }
            else if (min == b)
            {
                h = (ushort)(60 * (g - r) / (max - min) + 60);
            }
            else if (min == r)
            {
                h = (ushort)(60 * (b - g) / (max - min) + 180);
            }
            else if (min == g)
            {
                h = (ushort)(60 * (r - b) / (max - min) + 300);
            }

            s = (ushort)(max - min);

            v = max;
        }

        /// <summary>
        /// RGB値取得
        /// </summary>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        private static void GetRGB(ushort h, ushort s, ushort v,
            out double r, out double g, out double b)
        {
            var c = s / 255.0;
            var _v = v / 255.0;
            var _h = h / 60.0;
            var _x = c * (1 - Math.Abs(_h % 2 - 1));

            r = g = b = _v - c;

            if (_h < 1)
            {
                r += c;
                g += _x;
            }
            else if (_h < 2)
            {
                r += _x;
                g += c;
            }
            else if (_h < 3)
            {
                g += c;
                b += _x;
            }
            else if (_h < 4)
            {
                g += _x;
                b += c;
            }
            else if (_h < 5)
            {
                r += _x;
                b += c;
            }
            else if (_h < 6)
            {
                r += c;
                b += _x;
            }

            r *= 255.0;
            g *= 255.0;
            b *= 255.0;

        }
    }
}
