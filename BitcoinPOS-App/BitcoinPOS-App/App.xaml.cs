using System;
using System.Diagnostics;
using Autofac;
using BitcoinPOS_App.Interfaces.Services.Navigation;
using BitcoinPOS_App.ViewModels;
using NBitcoin;
using Plugin.Iconize;
using Plugin.Iconize.Fonts;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace BitcoinPOS_App
{
    public partial class App
    {
        public static ILifetimeScope Container { get; private set; }

        public App(Action<ContainerBuilder> configDI)
        {
            InitializeComponent();
            CreateDIContainer(configDI);

            // Adds entropy to the random utility used by NBitcoin
            RandomUtils.AddEntropy($"bitcoin-pos-app-{DateTime.Now.Ticks}-{Device.RuntimePlatform}");

            Iconize.With(new MaterialModule());

            Container.Resolve<INavigationService>()
                .InitializeAsync<MainPageViewModel>();
        }

        private void CreateDIContainer(Action<ContainerBuilder> configDI)
        {
            if (configDI == null)
                throw new ArgumentNullException(nameof(configDI));

            var cb = new ContainerBuilder();

            // register views
            cb.RegisterAssemblyTypes(typeof(App).Assembly)
                .InNamespace(nameof(BitcoinPOS_App) + "." + nameof(Views))
                .AsSelf()
                .InstancePerLifetimeScope();
            // register view models
            cb.RegisterAssemblyTypes(typeof(App).Assembly)
                .InNamespace(nameof(BitcoinPOS_App) + "." + nameof(ViewModels))
                .AsSelf()
                .InstancePerLifetimeScope();

            // register services
            cb.RegisterAssemblyTypes(typeof(App).Assembly)
                .InNamespace(nameof(BitcoinPOS_App) + "." + nameof(Services))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
            // register providers
            cb.RegisterAssemblyTypes(typeof(App).Assembly)
                .InNamespace(nameof(BitcoinPOS_App) + "." + nameof(Providers))
                .AsImplementedInterfaces()
                .SingleInstance();

            // register platform specific
            configDI(cb);

            Container = cb.Build().BeginLifetimeScope();
        }

        protected override void OnStart()
        {
            Debug.WriteLine("Iniciando...", "APP");
        }

        protected override void OnSleep()
        {
            Debug.WriteLine("Dormindo...", "APP");
        }

        protected override void OnResume()
        {
            Debug.WriteLine("Resumindo...", "APP");
        }
    }
}
