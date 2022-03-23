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
        public static Image<Rgb24> GaussianFilter(Image<Rgb24> image, (int w, int h) kernel, double sigma = 1.3)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] gaussianBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreateKernelBytes(kernel, padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h, _parallelOptions, (y) =>
            {
                if ((y + kernel.h) > paddingSize.h)
                {
                    return;
                }
                for (int x = 0; x < paddingSize.w; x++)
                {
                    if ((x + kernel.w) > paddingSize.w)
                    {
                        return;
                    }
                    double vr = 0;
                    double vg = 0;
                    double vb = 0;

                    for (int k = 0; k < kernel.h * kernel.w; k++)
                    {
                        int dy = k / kernel.h;
                        int dx = k % kernel.w;
                        int currentByte = (y + dy) * paddingSize.w + (x + dx);
                        vr += paddingBytes[currentByte].R * kernelBytes[k];
                        vg += paddingBytes[currentByte].G * kernelBytes[k];
                        vb += paddingBytes[currentByte].B * kernelBytes[k];
                    }

                    int inputRow = image.Width * y + x;
                    gaussianBytes[inputRow].R = (byte)vr;
                    gaussianBytes[inputRow].G = (byte)vg;
                    gaussianBytes[inputRow].B = (byte)vb;
                }
            });

            return Image.LoadPixelData(gaussianBytes, image.Width, image.Height);
        }

        private static double[] CreateKernelBytes((int w, int h) kernel, (int w, int h) padding, double sigma = 1.3)
        {
            double[] tmpBytes = new double[kernel.w * kernel.h];

            int _x = 0, _y = 0;
            double kernel_sum = 0;

            for (int y = 0; y < kernel.h; y++)
            {
                for (int x = 0; x < kernel.w; x++)
                {
                    _y = y - padding.h;
                    _x = x - padding.w;
                    int currentByte = y * kernel.w + x;
                    tmpBytes[currentByte] = 1 / (2 * Math.PI * sigma * sigma) * Math.Exp(-(_x * _x + _y * _y) / (2 * sigma * sigma));
                    kernel_sum += tmpBytes[currentByte];
                }
            }

            for (int y = 0; y < kernel.h; y++)
            {
                for (int x = 0; x < kernel.h; x++)
                {
                    int currentByte = y * kernel.h + x;
                    tmpBytes[currentByte] = tmpBytes[currentByte] / kernel_sum;
                }
            }

            return tmpBytes;
        }
    }
}
