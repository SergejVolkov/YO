using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Configuration;
using YO.Internals.Extensions;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IConfiguration _configuration;
		private readonly IShikimoriApi _shikimoriApi;

		public Interaction<LoginViewModel, string> ShowLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> OpenLoginDialog { get; }
		
		[Reactive]
		public bool IsAuthorized { get; set; }
		
		[Reactive]
		public bool IsLoading { get; set; }
		
		[Reactive]
		public IEnumerable<AnimeViewModel> Animes { get; set; }

		public MainWindowViewModel(IConfiguration configuration, 
								   IShikimoriApi shikimoriApi,
								   HttpClient httpClient)
		{
			_configuration = configuration;
			_shikimoriApi = shikimoriApi;

			_configuration.WhenAnyValue(c => c.ShikimoriUsername)
						  .Subscribe(async newName => await OnUserNameChanged(newName));
			ShowLoginDialog = new Interaction<LoginViewModel, string>();
			OpenLoginDialog = ReactiveCommand.CreateFromTask(OpenLoginDialogImpl);
		}

		private async Task OpenLoginDialogImpl()
		{
			var loginViewModel = new LoginViewModel(_configuration.ShikimoriUsername);
			_configuration.ShikimoriUsername = await ShowLoginDialog.Handle(loginViewModel);
		}

		private async Task OnUserNameChanged(string? newName)
		{
			IsAuthorized = !string.IsNullOrEmpty(newName);
			
			if (IsAuthorized)
			{
				IsLoading = true;
				var user = await _shikimoriApi.Users
											  .GetByNickname(newName);
				var animeRates = await _shikimoriApi.UserRates
													.GetUserRates()
													.WithUserId(user.Id)
													.WithTargetType(DataType.Anime)
													.WithStatus(RateStatus.Watching);
				var animes = await _shikimoriApi.Animes
												.GetAnimes()
												.WithIds(animeRates.Select(ur => ur.TargetId))
												.WithLimit(50);
				
				using (var webClient = new WebClient())
				{
					var list = new List<AnimeViewModel>();
					foreach (var animeInfo in animes)
					{
						var anime = new AnimeViewModel(animeInfo)
						{
							Poster = await animeInfo.GetAnimePoster(webClient)
						};
						list.Add(anime);
					}
					Animes = list;
				}
				
				IsLoading = false;
			}
		}
	}
}