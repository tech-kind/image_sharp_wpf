using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageSharpWpf.Utils
{
    public static class ImageLib
    {
        public static BitmapSource? ConvertFromImageToBitmapSource(Image<Rgb24> image)
        {
            byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgb24>()];
            image.CopyPixelDataTo(pixelBytes);

            var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Rgb24, null, pixelBytes, image.Width * 3);

            return bmp;
        }

        public static BitmapSource? ConvertFromImageToBitmapSource(Image<L8> image)
        {
            byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<L8>()];
            image.CopyPixelDataTo(pixelBytes);

            var bmp = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Gray8, null, pixelBytes, image.Width);

            return bmp;
        }

        public static Image<Rgb24> ConvertFromRGBToBGR(Image<Rgb24> image)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] bgrBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            Parallel.For(0, rgbBytes.Length, (i) =>
            {
                bgrBytes[i].R = rgbBytes[i].B;
                bgrBytes[i].G = rgbBytes[i].G;
                bgrBytes[i].B = rgbBytes[i].R;
            });

            return Image.LoadPixelData(bgrBytes, image.Width, image.Height);
        }

        public static Image<L8> ConvertGray(Image<Rgb24> color)
        {
            Rgb24[] colorBytes = new Rgb24[color.Width * color.Height];
            L8[] grayBytes = new L8[color.Width * color.Height];
            color.CopyPixelDataTo(colorBytes);

            Parallel.For(0, colorBytes.Length, (i) =>
            {
                grayBytes[i].PackedValue = (byte)(colorBytes[i].R * 0.2126 + colorBytes[i].G * 0.7152
                    + colorBytes[i].B * 0.0722);
            });

            return Image.LoadPixelData(grayBytes, color.Width, color.Height);
        }

        public static Image<L8> BinaryThreshold(Image<L8> image, int threshold)
        {
            L8[] imgBytes = new L8[image.Width * image.Height];
            L8[] threshBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            Parallel.For(0, imgBytes.Length, (i) =>
            {
                if (imgBytes[i].PackedValue < threshold)
                {
                    threshBytes[i].PackedValue = 0;
                }
                else
                {
                    threshBytes[i].PackedValue = 255;
                }
            });

            return Image.LoadPixelData(threshBytes, image.Width, image.Height);
        }

        public static Image<L8> OtsuThreshold(Image<L8> image)
        {
            L8[] imgBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            int height = image.Height;
            int width = image.Width;

            // determine threshold
            double max_sb = 0;
            int th = 0;

            var syncObject = new object();

            Parallel.For(1, 254, (t) =>
            {
                double w0 = 0;
                double w1 = 0;
                double m0 = 0;
                double m1 = 0;

                for (int i = 0; i < imgBytes.Length; i++)
                {
                    if (imgBytes[i].PackedValue < t)
                    {
                        w0++;
                        m0 += imgBytes[i].PackedValue;
                    }
                    else
                    {
                        w1++;
                        m1 += imgBytes[i].PackedValue;
                    }
                }

                m0 /= w0;
                m1 /= w1;
                w0 /= (height * width);
                w1 /= (height * width);
                double sb = w0 * w1 * Math.Pow((m0 - m1), 2);

                lock (syncObject)
                {
                    if (sb > max_sb)
                    {
                        max_sb = sb;
                        th = t;
                    }
                }
            });

            return BinaryThreshold(image, th);
        }

        public static Image<Rgb48> ConvertFromRGBToHSV(Image<Rgb24> image)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb48[] hsvBytes = new Rgb48[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            Parallel.For(0, rgbBytes.Length, (i) =>
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

            Parallel.For(0, hsvBytes.Length, (i) =>
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

            Parallel.For(0, hsvBytes.Length, (i) =>
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
