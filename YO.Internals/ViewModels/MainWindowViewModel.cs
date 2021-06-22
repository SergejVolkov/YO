using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Configuration;

namespace YO.Internals.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IConfiguration _configuration;
		public Interaction<LoginViewModel, string> ShowLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> OpenLoginDialog { get; }
		
		[Reactive]
		public bool IsAuthorized { get; set; }

		public MainWindowViewModel(IConfiguration configuration)
		{
			_configuration = configuration;

			_configuration.WhenAnyValue(c => c.ShikimoriUsername)
						  .Subscribe(OnUserNameChanged);
			ShowLoginDialog = new Interaction<LoginViewModel, string>();
			OpenLoginDialog = ReactiveCommand.CreateFromTask(OpenLoginDialogImpl);
		}

		private async Task OpenLoginDialogImpl()
		{
			var loginViewModel = new LoginViewModel(_configuration.ShikimoriUsername);
			_configuration.ShikimoriUsername = await ShowLoginDialog.Handle(loginViewModel);
		}

		private void OnUserNameChanged(string? newName)
		{
			IsAuthorized = !string.IsNullOrEmpty(newName);
		}
	}
}