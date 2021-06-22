using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace YO.Internals.ViewModels
{
	public class LoginViewModel : ViewModelBase
	{
		[Reactive]
		public string ShikimoriUsername { get; set; }
		
		public ReactiveCommand<Unit, string> Confirm { get; }

		public LoginViewModel(string shikimoriUsername)
		{
			ShikimoriUsername = shikimoriUsername;
			Confirm = ReactiveCommand.Create(() => ShikimoriUsername);
		}
	}
}