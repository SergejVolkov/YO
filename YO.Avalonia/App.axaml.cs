using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using YO.Avalonia.Views;
using YO.Internals.Configuration;
using YO.Internals.ViewModels;

namespace YO.Avalonia
{
	public class App : Application
	{
		public IServiceProvider Services { get; }
		
		public App()
		{
			Services = ConfigureServices(new ServiceCollection());
		}
		
		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.MainWindow = new MainWindow
				{
					ViewModel = Services.GetRequiredService<MainWindowViewModel>(),
				};
			}

			base.OnFrameworkInitializationCompleted();
		}

		private static IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<IConfigurationManager, ConfigurationManager>();
			services.AddSingleton(provider => provider.GetRequiredService<IConfigurationManager>().Configuration);
			
			services.AddSingleton<MainWindowViewModel>();
			services.AddSingleton<LoginViewModel>();
			
			return services.BuildServiceProvider();
		}
	}
}