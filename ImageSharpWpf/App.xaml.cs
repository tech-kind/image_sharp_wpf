using ImageSharpWpf.Modules;
using ImageSharpWpf.Views;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageSharpWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ServiceCollection services = new ServiceCollection();

            // MessagePipe の標準サービスを登録する
            services.AddMessagePipe();

            services.AddTransient(typeof(IAsyncPublisher<,>), typeof(AsyncMessageBroker<,>));
            services.AddTransient(typeof(IAsyncSubscriber<,>), typeof(AsyncMessageBroker<,>));

            ServiceProvider provider = services.BuildServiceProvider();

            // image_manager
            {
                var bmpPublisher = provider.GetRequiredService<IAsyncPublisher<string, BitmapSource>>();
                var strPublisher = provider.GetRequiredService<IAsyncPublisher<string, string>>();
                var subscriber = provider.GetRequiredService<IAsyncSubscriber<string, string>>();
                var image_manager = new ImageManager(bmpPublisher, strPublisher, subscriber);
                image_manager.StartAsync(new System.Threading.CancellationToken());
            }

            containerRegistry.Register<IServiceProvider>(() => provider);
        }
    }
}
