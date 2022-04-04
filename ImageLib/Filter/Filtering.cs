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
        public enum DiffMode
        {
            x = 0,
            y
        }

        public static Image<Rgb24> GaussianFilter(Image<Rgb24> image, (int w, int h) kernel, double sigma = 1.3)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] gaussianBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreateGaussianKernel(kernel, padding, sigma);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double vr = 0;
                double vg = 0;
                double vb = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
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
            });

            return Image.LoadPixelData(gaussianBytes, image.Width, image.Height);
        }

        public static Image<Rgb24> MedianFilter(Image<Rgb24> image, (int w, int h) kernel)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] medianBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                byte[] vrBytes = new byte[kernel.h * kernel.w];
                byte[] vgBytes = new byte[kernel.h * kernel.w];
                byte[] vbBytes = new byte[kernel.h * kernel.w];

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    vrBytes[k] = paddingBytes[currentByte].R;
                    vgBytes[k] = paddingBytes[currentByte].G;
                    vbBytes[k] = paddingBytes[currentByte].B;
                }

                Sort(vrBytes, 0, vrBytes.Length - 1);
                Sort(vgBytes, 0, vgBytes.Length - 1);
                Sort(vbBytes, 0, vbBytes.Length - 1);

                int inputRow = image.Width * y + x;
                medianBytes[inputRow].R = vrBytes[vrBytes.Length / 2 + 1];
                medianBytes[inputRow].G = vgBytes[vgBytes.Length / 2 + 1];
                medianBytes[inputRow].B = vbBytes[vbBytes.Length / 2 + 1];
            });

            return Image.LoadPixelData(medianBytes, image.Width, image.Height);
        }

        public static Image<Rgb24> SmoothingFilter(Image<Rgb24> image, (int w, int h) kernel)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] smoothBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double vr = 0;
                double vg = 0;
                double vb = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    vr += paddingBytes[currentByte].R;
                    vg += paddingBytes[currentByte].G;
                    vb += paddingBytes[currentByte].B;
                }

                int size = kernel.h * kernel.w;
                vr /= size;
                vg /= size;
                vb /= size;

                int inputRow = image.Width * y + x;
                smoothBytes[inputRow].R = (byte)vr;
                smoothBytes[inputRow].G = (byte)vg;
                smoothBytes[inputRow].B = (byte)vb;
            });

            return Image.LoadPixelData(smoothBytes, image.Width, image.Height);
        }

        public static Image<Rgb24> MotionFilter(Image<Rgb24> image, (int w, int h) kernel)
        {
            Rgb24[] rgbBytes = new Rgb24[image.Width * image.Height];
            Rgb24[] motionBytes = new Rgb24[image.Width * image.Height];
            image.CopyPixelDataTo(rgbBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            Rgb24[] paddingBytes = GetPaddingImage(rgbBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreateMotionKernel(kernel);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double vr = 0;
                double vg = 0;
                double vb = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    vr += paddingBytes[currentByte].R * kernelBytes[k];
                    vg += paddingBytes[currentByte].G * kernelBytes[k];
                    vb += paddingBytes[currentByte].B * kernelBytes[k];
                }

                int inputRow = image.Width * y + x;
                motionBytes[inputRow].R = (byte)vr;
                motionBytes[inputRow].G = (byte)vg;
                motionBytes[inputRow].B = (byte)vb;
            });

            return Image.LoadPixelData(motionBytes, image.Width, image.Height);
        }

        public static Image<L8> MaxMinFilter(Image<Rgb24> image, (int w, int h) kernel)
        {
            var gray = Grayscale(image);
            return MaxMinFilter(gray, kernel);
        }

        public static Image<L8> MaxMinFilter(Image<L8> image, (int w, int h) kernel)
        {
            L8[] grayBytes = new L8[image.Width * image.Height];
            L8[] maxminBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(grayBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            L8[] paddingBytes = GetPaddingImage(grayBytes, (image.Width, image.Height), padding);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                byte[] tmpBytes = new byte[kernel.h * kernel.w];
                
                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    tmpBytes[k] = paddingBytes[currentByte].PackedValue;
                }
                byte diff = (byte)(tmpBytes.Max() - tmpBytes.Min());

                int inputRow = image.Width * y + x;
                maxminBytes[inputRow].PackedValue = diff;
            });

            return Image.LoadPixelData(maxminBytes, image.Width, image.Height);
        }

        public static Image<L8> DiffFilter(Image<Rgb24> image, DiffMode mode)
        {
            var gray = Grayscale(image);
            return DiffFilter(gray, mode);
        }

        public static Image<L8> DiffFilter(Image<L8> image, DiffMode mode)
        {
            L8[] grayBytes = new L8[image.Width * image.Height];
            L8[] diffBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(grayBytes);

            (int w, int h) kernel = new()
            {
                h = 3,
                w = 3
            };

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            L8[] paddingBytes = GetPaddingImage(grayBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreateDiffKernel(mode);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double value = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    value += paddingBytes[currentByte].PackedValue * kernelBytes[k];
                }

                value = Math.Max(value, 0);
                value = Math.Min(value, 255);

                int inputRow = image.Width * y + x;
                diffBytes[inputRow].PackedValue = (byte)value;
            });

            return Image.LoadPixelData(diffBytes, image.Width, image.Height);
        }

        public static Image<L8> PrewittFilter(Image<Rgb24> image, (int w, int h) kernel, DiffMode mode)
        {
            var gray = Grayscale(image);
            return PrewittFilter(gray, kernel, mode);
        }

        public static Image<L8> PrewittFilter(Image<L8> image, (int w, int h) kernel, DiffMode mode)
        {
            L8[] grayBytes = new L8[image.Width * image.Height];
            L8[] diffBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(grayBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            L8[] paddingBytes = GetPaddingImage(grayBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreatePrewittKernel(kernel, mode);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double value = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    value += paddingBytes[currentByte].PackedValue * kernelBytes[k];
                }

                value = Math.Max(value, 0);
                value = Math.Min(value, 255);

                int inputRow = image.Width * y + x;
                diffBytes[inputRow].PackedValue = (byte)value;
            });

            return Image.LoadPixelData(diffBytes, image.Width, image.Height);
        }

        public static Image<L8> SobelFilter(Image<Rgb24> image, (int w, int h) kernel, DiffMode mode)
        {
            var gray = Grayscale(image);
            return SobelFilter(gray, kernel, mode);
        }

        public static Image<L8> SobelFilter(Image<L8> image, (int w, int h) kernel, DiffMode mode)
        {
            L8[] grayBytes = new L8[image.Width * image.Height];
            L8[] diffBytes = new L8[image.Width * image.Height];
            image.CopyPixelDataTo(grayBytes);

            (int w, int h) padding = (kernel.w / 2, kernel.h / 2);
            L8[] paddingBytes = GetPaddingImage(grayBytes, (image.Width, image.Height), padding);

            double[] kernelBytes = CreateSobelKernel(kernel, mode);

            (int w, int h) paddingSize = new()
            {
                h = (image.Height + padding.h * 2),
                w = (image.Width + padding.w * 2)
            };

            Parallel.For(0, paddingSize.h * paddingSize.w, _parallelOptions, (i) =>
            {
                int y = i / paddingSize.w;
                int x = i % paddingSize.w;

                if (((y + kernel.h) > paddingSize.h) ||
                    ((x + kernel.w) > paddingSize.w))
                {
                    return;
                }

                double value = 0;

                for (int k = 0; k < kernel.h * kernel.w; k++)
                {
                    int dy = k / kernel.w;
                    int dx = k % kernel.w;
                    int currentByte = (y + dy) * paddingSize.w + (x + dx);
                    value += paddingBytes[currentByte].PackedValue * kernelBytes[k];
                }

                value = Math.Max(value, 0);
                value = Math.Min(value, 255);

                int inputRow = image.Width * y + x;
                diffBytes[inputRow].PackedValue = (byte)value;
            });

            return Image.LoadPixelData(diffBytes, image.Width, image.Height);
        }

        private static double[] CreateGaussianKernel((int w, int h) kernel, (int w, int h) padding, double sigma = 1.3)
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

        private static double[] CreateMotionKernel((int w, int h) kernel)
        {
            double[] tmpBytes = new double[kernel.w * kernel.h];

            for (int y = 0; y < kernel.h; y++)
            {
                for (int x = 0; x < kernel.w; x++)
                {
                    int currentByte = y * kernel.w + x;
                    if (y == x)
                    {
                        tmpBytes[currentByte] = 1 / (double)kernel.w;
                    }
                    else
                    {
                        tmpBytes[currentByte] = 0;
                    }
                }
            }

            return tmpBytes;
        }

        private static double[] CreateDiffKernel(DiffMode mode)
        {
            double[] tmpBytes = new double[3 * 3];

            if (mode == DiffMode.x)
            {
                tmpBytes[3] = -1;
                tmpBytes[5] = 1;
            }
            else
            {
                tmpBytes[1] = -1;
                tmpBytes[7] = 1;
            }

            return tmpBytes;
        }

        private static double[] CreatePrewittKernel((int w, int h) kernel, DiffMode mode)
        {
            double[] tmpBytes = new double[kernel.w * kernel.h];

            for (int y = 0; y < kernel.h; y++)
            {
                for (int x = 0; x < kernel.w; x++)
                {
                    int currentByte = y * kernel.w + x;
                    if (mode == DiffMode.x && x == 0)
                    {
                        tmpBytes[currentByte] = -1;
                    }
                    else if (mode == DiffMode.x && x == kernel.w - 1)
                    {
                        tmpBytes[currentByte] = 1;
                    }
                    else if (mode == DiffMode.y && y == 0)
                    {
                        tmpBytes[currentByte] = -1;
                    }
                    else if (mode == DiffMode.y && y == kernel.h - 1)
                    {
                        tmpBytes[currentByte] = 1;
                    }
                }
            }

            return tmpBytes;
        }

        private static double[] CreateSobelKernel((int w, int h) kernel, DiffMode mode)
        {
            double[] tmpBytes = new double[kernel.w * kernel.h];

            (int w, int h) center = new()
            {
                w = kernel.w / 2,
                h = kernel.h / 2
            };

            for (int y = 0; y < kernel.h; y++)
            {
                for (int x = 0; x < kernel.w; x++)
                {
                    int currentByte = y * kernel.w + x;
                    if (mode == DiffMode.x && x == 0)
                    {
                        if (y == center.h - 1)
                        {
                            tmpBytes[currentByte] = -2;
                        }
                        else
                        {
                            tmpBytes[currentByte] = -1;
                        }
                    }
                    else if (mode == DiffMode.x && x == kernel.w - 1)
                    {
                        if (y == center.h - 1)
                        {
                            tmpBytes[currentByte] = 2;
                        }
                        else
                        {
                            tmpBytes[currentByte] = 1;
                        }
                    }
                    else if (mode == DiffMode.y && y == 0)
                    {
                        if (x == center.w - 1)
                        {
                            tmpBytes[currentByte] = -2;
                        }
                        else
                        {
                            tmpBytes[currentByte] = -1;
                        }
                    }
                    else if (mode == DiffMode.y && y == kernel.h - 1)
                    {
                        if (x == center.w - 1)
                        {
                            tmpBytes[currentByte] = 2;
                        }
                        else
                        {
                            tmpBytes[currentByte] = 1;
                        }
                    }
                }
            }

            return tmpBytes;
        }

        private static int Partition(byte[] array, int low, int high)
        {
            //1. Select a pivot point.
            int pivot = array[high];

            int lowIndex = (low - 1);

            //2. Reorder the collection.
            for (int j = low; j < high; j++)
            {
                if (array[j] <= pivot)
                {
                    lowIndex++;

                    byte temp = array[lowIndex];
                    array[lowIndex] = array[j];
                    array[j] = temp;
                }
            }

            byte temp1 = array[lowIndex + 1];
            array[lowIndex + 1] = array[high];
            array[high] = temp1;

            return lowIndex + 1;
        }

        private static void Sort(byte[] array, int low, int high)
        {
            if (low < high)
            {
                int partitionIndex = Partition(array, low, high);
                //3. Recursively continue sorting the array
                Sort(array, low, partitionIndex - 1);
                Sort(array, partitionIndex + 1, high);
            }
        }
    }
}
