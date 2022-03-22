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
        public static Image<Rgb24> AveragePooling(Image<Rgb24> image, (int w, int h) kernel, (int w, int h) padding,
            (int w, int h) stride)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            (int w, int h) newSize = GetImageSizeAfterFiltering((image.Width, image.Height), kernel, padding, stride);
            Rgb24[] aveBytes = new Rgb24[newSize.w * newSize.h];
            image.CopyPixelDataTo(rgbBytes);

            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h / stride.h, _parallelOptions, (y, loop) =>
            {
                var currentRow = y * stride.h;
                if ((currentRow + kernel.h) > paddingSize.h)
                {
                    return;
                }
                int row = paddingSize.w / stride.w;
                for (int x = 0; x < row; x++)
                {
                    var currentColumn = x * stride.w;
                    if ((currentColumn + kernel.w) > paddingSize.w)
                    {
                        return;
                    }
                    double vr = 0;
                    double vg = 0;
                    double vb = 0;
                    for (int dy = 0; dy < kernel.h; dy++)
                    {
                        for (int dx = 0; dx < kernel.w; dx++)
                        {
                            int currentByte = (currentRow + dy) * paddingSize.w + (currentColumn + dx);
                            vr += paddingBytes[currentByte].R;
                            vg += paddingBytes[currentByte].G;
                            vb += paddingBytes[currentByte].B;
                        }
                    }
                    int size = kernel.h * kernel.w;
                    vr /= size;
                    vg /= size;
                    vb /= size;

                    int inputRow = newSize.w * y + x;
                    aveBytes[inputRow].R = (byte)vr;
                    aveBytes[inputRow].G = (byte)vg;
                    aveBytes[inputRow].B = (byte)vb;                    
                }
            });

            return Image.LoadPixelData(aveBytes, newSize.w, newSize.h);
        }

        public static Image<Rgb24> MaxPooling(Image<Rgb24> image, (int w, int h) kernel, (int w, int h) padding,
            (int w, int h) stride)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            (int w, int h) newSize = GetImageSizeAfterFiltering((image.Width, image.Height), kernel, padding, stride);
            Rgb24[] maxBytes = new Rgb24[newSize.w * newSize.h];
            image.CopyPixelDataTo(rgbBytes);

            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h / stride.h, _parallelOptions, (y, loop) =>
            {
                var currentRow = y * stride.h;
                if ((currentRow + kernel.h) > paddingSize.h)
                {
                    return;
                }
                int row = paddingSize.w / stride.w;
                for (int x = 0; x < row; x++)
                {
                    var currentColumn = x * stride.w;
                    if ((currentColumn + kernel.w) > paddingSize.w)
                    {
                        return;
                    }
                    byte vr = 0;
                    byte vg = 0;
                    byte vb = 0;
                    for (int dy = 0; dy < kernel.h; dy++)
                    {
                        for (int dx = 0; dx < kernel.w; dx++)
                        {
                            int currentByte = (currentRow + dy) * paddingSize.w + (currentColumn + dx);
                            vr = Math.Max(paddingBytes[currentByte].R, vr);
                            vg = Math.Max(paddingBytes[currentByte].G, vg);
                            vb = Math.Max(paddingBytes[currentByte].B, vb);
                        }
                    }

                    int inputRow = newSize.w * y + x;
                    maxBytes[inputRow].R = vr;
                    maxBytes[inputRow].G = vg;
                    maxBytes[inputRow].B = vb;
                }
            });

            return Image.LoadPixelData(maxBytes, newSize.w, newSize.h);
        }

        private static (int, int) GetImageSizeAfterFiltering((int w, int h) imageSize, (int w, int h) kernel, (int w, int h) padding,
            (int w, int h) stride)
        {
            (int w, int h) newSize = new();
            newSize.w = (int)(Math.Ceiling((double)((imageSize.w - kernel.w + 2 * padding.w) / stride.w)) + 1);
            newSize.h = (int)(Math.Ceiling((double)((imageSize.h - kernel.h + 2 * padding.h) / stride.h)) + 1);

            return newSize;
        }

        private static Rgb24[] GetPaddingImage(Rgb24[] image, (int w, int h) imageSize, (int w, int h) padding)
        {
            Rgb24[] paddingImg = new Rgb24[(imageSize.w + padding.w * 2) * (imageSize.h + padding.h * 2)];

            Parallel.For(0, imageSize.h, _parallelOptions, (h) =>
            {
                for (int w = 0; w < imageSize.w; w++)
                {
                    paddingImg[((imageSize.w + padding.w * 2) * (padding.h + h)) + (padding.w + w)].R = image[(imageSize.w * h) + w].R;
                    paddingImg[((imageSize.w + padding.w * 2) * (padding.h + h)) + (padding.w + w)].G = image[(imageSize.w * h) + w].G;
                    paddingImg[((imageSize.w + padding.w * 2) * (padding.h + h)) + (padding.w + w)].B = image[(imageSize.w * h) + w].B;
                }
            });

            return paddingImg;
        }
    }
}
