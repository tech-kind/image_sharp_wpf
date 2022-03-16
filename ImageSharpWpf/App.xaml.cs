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

            services.AddSingleton<IImageManager, ImageManager>();

            ServiceProvider provider = services.BuildServiceProvider();

            // image_manager
            {
                // var pathPublisher = provider.GetRequiredService<IRequestHandler<string, string>>();
                // var pathSubscriber = provider.GetRequiredService<IRequestHandler<string, string>>();
                // var path_manager = new PathManager.PathManager(pathPublisher, pathSubscriber);
                // path_manager.StartAsync(new System.Threading.CancellationToken());
            }

            containerRegistry.Register<IServiceProvider>(() => provider);
        }
    }
}
