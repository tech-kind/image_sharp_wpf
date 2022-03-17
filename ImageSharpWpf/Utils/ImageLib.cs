using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.CompilerServices;
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
            var dst = new Image<Rgb24>(image.Width, image.Height);
            int height = image.Height;
            image.ProcessPixelRows(dst, (srcAccessor, dstAccessor) =>
            {
                for (int i = 0; i < height; i++)
                {
                    Span<Rgb24> srcPixelRow = srcAccessor.GetRowSpan(i);
                    Span<Rgb24> dstPixelRow = dstAccessor.GetRowSpan(i);

                    for (int x = 0; x < srcPixelRow.Length; x++)
                    {
                        dstPixelRow[x].R = srcPixelRow[x].B;
                        dstPixelRow[x].G = srcPixelRow[x].G;
                        dstPixelRow[x].B = srcPixelRow[x].R;
                    }
                }
            });
            return dst;
        }

        public static Image<L8> ConvertGray(Image<Rgb24> color)
        {
            var gray = new Image<L8>(color.Width, color.Height);
            int height = color.Height;
            color.ProcessPixelRows(gray, (srcAccessor, grayAccessor) =>
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

        public static void BinaryThreshold(ref Image<L8> image, int threshold)
        {
            image.ProcessPixelRows(accessor =>
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
                        if (pixel.PackedValue > threshold)
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
        }

        public static void OtsuThreshold(ref Image<L8> image)
        {
            int height = image.Height;
            int width = image.Width;

            // determine threshold
            double w0 = 0, w1 = 0;
            double m0 = 0, m1 = 0;
            double max_sb = 0, sb = 0;
            int th = 0;

            for (int t = 1; t < 254; t++)
            {
                w0 = 0;
                w1 = 0;
                m0 = 0;
                m1 = 0;

                image.ProcessPixelRows(accessor =>
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
                            if (pixel.PackedValue < t)
                            {
                                w0++;
                                m0 += pixel.PackedValue;
                            }
                            else
                            {
                                w1++;
                                m1 += pixel.PackedValue;
                            }
                        }
                    }
                });

                m0 /= w0;
                m1 /= w1;
                w0 /= (height * width);
                w1 /= (height * width);
                sb = w0 * w1 * Math.Pow((m0 - m1), 2);

                if (sb > max_sb)
                {
                    max_sb = sb;
                    th = t;
                }
            }

            BinaryThreshold(ref image, th);
        }

        public static Image<Rgb48> ConvertFromRGBToHSV(Image<Rgb24> image)
        {
            var dst = new Image<Rgb48>(image.Width, image.Height);
            int height = image.Height;

            double r, g, b;

            image.ProcessPixelRows(dst, (srcAccessor, dstAccessor) =>
            {
                for (int i = 0; i < height; i++)
                {
                    Span<Rgb24> srcPixelRow = srcAccessor.GetRowSpan(i);
                    Span<Rgb48> dstPixelRow = dstAccessor.GetRowSpan(i);

                    for (int x = 0; x < srcPixelRow.Length; x++)
                    {
                        r = srcPixelRow[x].R;
                        g = srcPixelRow[x].G;
                        b = srcPixelRow[x].B;
                        
                        GetHSV(r, g, b, out double h, out double s, out double v);

                        dstPixelRow[x].R = (ushort)h;
                        dstPixelRow[x].G = (ushort)s;
                        dstPixelRow[x].B = (ushort)v;
                    }
                }
            });
            return dst;
        }

        public static Image<Rgb24> ConvertFromHSVToRGB(Image<Rgb48> image)
        {
            var dst = new Image<Rgb24>(image.Width, image.Height);
            int height = image.Height;

            double h, s, v;

            image.ProcessPixelRows(dst, (srcAccessor, dstAccessor) =>
            {
                for (int i = 0; i < height; i++)
                {
                    Span<Rgb48> srcPixelRow = srcAccessor.GetRowSpan(i);
                    Span<Rgb24> dstPixelRow = dstAccessor.GetRowSpan(i);

                    for (int x = 0; x < srcPixelRow.Length; x++)
                    {
                        h = srcPixelRow[x].R;
                        s = srcPixelRow[x].G;
                        v = srcPixelRow[x].B;

                        GetRGB(h, s, v, out double r, out double g, out double b);

                        dstPixelRow[x].R = (byte)r;
                        dstPixelRow[x].G = (byte)g;
                        dstPixelRow[x].B = (byte)b;
                    }
                }
            });
            return dst;
        }

        public static void InverseHue(ref Image<Rgb48> hsv)
        {
            hsv.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgb48> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        var h = pixelRow[x].R;
                        h = (ushort)((h + 180) % 360);
                        pixelRow[x].R = h;
                    }
                }
            });
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
        private static void GetHSV(double r, double g, double b,
            out double h, out double s, out double v)
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
                h = 60 * (g - r) / (max - min) + 60;
            }
            else if (min == r)
            {
                h = 60 * (b - g) / (max - min) + 180;
            }
            else if (min == g)
            {
                h = 60 * (r - b) / (max - min) + 300;
            }

            s = max - min;

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
        private static void GetRGB(double h, double s, double v,
            out double r, out double g, out double b)
        {
            var c = s / 255.0;
            var _v = v / 255.0;
            var _h = h / 60;
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
