using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageSharpWpf.Utils.TopicName;

namespace ImageSharpWpf.ViewModels
{
    public class HomeMenuViewModel : BindableBase
    {
        private readonly IAsyncPublisher<string, string> _publisher;

        public DelegateCommand FileSelectCommand { get; private set; }

        public DelegateCommand ThresholdCommand { get; private set; }

        public DelegateCommand GrayScaleCommand { get; private set; }

        public DelegateCommand RGBToBGRCommand { get; private set; }

        public DelegateCommand OtsuThresholdCommand { get; private set; }

        public DelegateCommand HSVCommand { get; private set; }

        public DelegateCommand SubtractionCommand { get; private set; }

        public HomeMenuViewModel(IServiceProvider serviceProvider)
        {
            _publisher = serviceProvider.GetRequiredService<IAsyncPublisher<string, string>>();

            FileSelectCommand = new DelegateCommand(FileSelection);
            ThresholdCommand = new DelegateCommand(Threshold);
            GrayScaleCommand = new DelegateCommand(ConvertGrayscale);
            RGBToBGRCommand = new DelegateCommand(RGBToBGR);
            OtsuThresholdCommand = new DelegateCommand(OtsuThreshold);
            HSVCommand = new DelegateCommand(HSV);
            SubtractionCommand = new DelegateCommand(Subtraction);
        }

        private void FileSelection()
        {
            var dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                var file = dialog.FileName;
                _publisher.Publish(IMAGE_MANAGER_SET_IMAGE, file);
            }
        }

        private void ConvertGrayscale()
        {
            _publisher.Publish(IMAGE_MANAGER_GRAY_SCALE, "");
        }

        private void Threshold()
        {
            _publisher.Publish(IMAGE_MANAGER_THRESHOLD, "");
        }

        private void OtsuThreshold()
        {
            _publisher.Publish(IMAGE_MANAGER_OTSU_THRESHOLD, "");
        }

        private void RGBToBGR()
        {
            _publisher.Publish(IMAGE_MANAGER_RGB_TO_BGR, "");
        }

        private void HSV()
        {
            _publisher.Publish(IMAGE_MANAGER_HSV, "");
        }

        private void Subtraction()
        {
            _publisher.Publish(IMAGE_MANAGER_SUBTRACTION, "");
        }
    }
}
