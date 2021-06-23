using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Cache;
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
		private readonly IImageCache _imageCache;

		public Interaction<LoginViewModel, string> ShowLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> OpenLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> RefreshList { get; }

		[Reactive]
		public bool IsAuthorized { get; set; }

		[Reactive]
		public bool IsLoading { get; set; }

		[Reactive]
		public IEnumerable<AnimeViewModel>? Animes { get; set; }
		
		[Reactive]
		public string? UserName { get; set; }
		
		[Reactive]
		public Bitmap? UserPicture { get; set; }

		public MainWindowViewModel(IConfiguration configuration,
								   IShikimoriApi shikimoriApi, IImageCache imageCache)
		{
			_configuration = configuration;
			_shikimoriApi = shikimoriApi;
			_imageCache = imageCache;

			_configuration.WhenAnyValue(c => c.ShikimoriUsername)
						  .Subscribe(async newName => await OnUserNameChanged(newName));
			
			ShowLoginDialog = new Interaction<LoginViewModel, string>();
			
			OpenLoginDialog = ReactiveCommand.CreateFromTask(OpenLoginDialogImpl);
			RefreshList = ReactiveCommand.CreateFromTask(UpdateAnimeList);
		}

		private async Task OpenLoginDialogImpl()
		{
			var loginViewModel = new LoginViewModel(_configuration.ShikimoriUsername);
			_configuration.ShikimoriUsername = await ShowLoginDialog.Handle(loginViewModel);
		}

		private async Task OnUserNameChanged(string? newName)
		{
			IsAuthorized = !string.IsNullOrEmpty(newName);
			UserName = IsAuthorized ? newName : "Ошибка авторизации...";

			if (IsAuthorized)
			{
				await UpdateAnimeList();
			} else
			{
				UserPicture = null;
				Animes = null;
			}
		}

		private async Task UpdateAnimeList()
		{
			Animes = null;

			if (!IsAuthorized)
			{
				return;
			}
			
			IsLoading = true;

			var user = await _shikimoriApi.Users
										  .GetByNickname(_configuration.ShikimoriUsername);
			UserPicture = await _imageCache.TryGetUserPicture(user);
			var animeRates = await _shikimoriApi.UserRates
												.GetUserRates()
												.WithUserId(user.Id)
												.WithTargetType(DataType.Anime)
												.WithStatus(RateStatus.Watching);
			var animes = await _shikimoriApi.Animes
											.GetAnimes()
											.WithIds(animeRates.Select(ur => ur.TargetId))
											.WithLimit(50);

			var list = new List<AnimeViewModel>();
			foreach (var animeInfo in animes)
			{
				var anime = new AnimeViewModel(animeInfo)
				{
					Poster = await _imageCache.TryGetAnimePoster(animeInfo)
				};
				list.Add(anime);
			}

			Animes = list;

			IsLoading = false;
		}
	}
}