using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLib
{
    public static partial class ImageOperator
    {
        public static Image<L8> BinaryThreshold(Image<L8> image, int threshold)
        {
            L8[] imgBytes = new L8[image.Width * image.Height];
            L8[] threshBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            Parallel.For(0, imgBytes.Length, _parallelOptions, (i) =>
            {
                threshBytes[i].PackedValue = GetThreshValue(imgBytes[i].PackedValue, threshold);
            });

            return Image.LoadPixelData(threshBytes, image.Width, image.Height);
        }

        public static Image<L8> BinaryThreshold(Image<Rgb24> image, int threshold)
        {
            Rgb24[] imgBytes = new Rgb24[image.Width * image.Height];
            L8[] threshBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            Parallel.For(0, imgBytes.Length, _parallelOptions, (i) =>
            {
                byte grayValue = GetGray(imgBytes[i].R, imgBytes[i].G, imgBytes[i].B);
                threshBytes[i].PackedValue = GetThreshValue(grayValue, threshold);
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

            Parallel.For(1, 254, _parallelOptions, (t) =>
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

        public static Image<L8> OtsuThreshold(Image<Rgb24> image)
        {
            Rgb24[] imgBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(imgBytes);

            int height = image.Height;
            int width = image.Width;

            // determine threshold
            double max_sb = 0;
            int th = 0;

            var syncObject = new object();

            Parallel.For(1, 254, _parallelOptions, (t) =>
            {
                double w0 = 0;
                double w1 = 0;
                double m0 = 0;
                double m1 = 0;

                for (int i = 0; i < imgBytes.Length; i++)
                {
                    var grayValue = GetGray(imgBytes[i].R, imgBytes[i].G, imgBytes[i].B);
                    if (grayValue < t)
                    {
                        w0++;
                        m0 += grayValue;
                    }
                    else
                    {
                        w1++;
                        m1 += grayValue;
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

        private static byte GetThreshValue(byte value, int threshold)
        {
            byte th = value switch
            {
                byte i when i < threshold => 0,
                _ => 255,
            };
            return th;
        }
    }
}
