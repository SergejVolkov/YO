using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YO.Internals.ViewModels;

namespace YO.Avalonia.Views
{
	public class LoginWindow : ReactiveWindow<LoginViewModel>
	{
		public LoginWindow()
		{
			InitializeComponent();
		#if DEBUG
			this.AttachDevTools();
		#endif
			
			this.WhenActivated(d => d(ViewModel!.Confirm.Subscribe(Close)));
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}