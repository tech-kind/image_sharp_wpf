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

        public DelegateCommand<string> ProcessingCommand { get; private set; }

        public HomeMenuViewModel(IServiceProvider serviceProvider)
        {
            _publisher = serviceProvider.GetRequiredService<IAsyncPublisher<string, string>>();

            FileSelectCommand = new DelegateCommand(FileSelection);
            ProcessingCommand = new DelegateCommand<string>(Processing);
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

        private void Processing(string topic)
        {
            _publisher.Publish(topic, "");
        }
    }
}
