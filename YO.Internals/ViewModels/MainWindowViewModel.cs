using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Cache;
using YO.Internals.Configuration;
using YO.Internals.Extensions;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly IConfiguration _configuration;
		private readonly IShikimoriApi _shikimoriApi;
		private readonly IImageCache _imageCache;

		public Interaction<LoginViewModel, Unit> ShowLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> OpenLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> RefreshList { get; }

		[Reactive]
		public bool IsLoading { get; set; }

		[Reactive]
		public IEnumerable<AnimeViewModel>? Animes { get; set; }

		public LoginViewModel LoginViewModel { get; }

		public MainWindowViewModel(LoginViewModel loginViewModel,
								   IConfiguration configuration,
								   IShikimoriApi shikimoriApi,
								   IImageCache imageCache)
		{
			LoginViewModel = loginViewModel;
			_configuration = configuration;
			_shikimoriApi = shikimoriApi;
			_imageCache = imageCache;

			LoginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						  .WhereNullValues()
						  .SubscribeDiscard(OnNullUser);
			LoginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						  .WhereNotNullValues()
						  .SubscribeAsyncDiscard(OnUserChanged);

			ShowLoginDialog = new Interaction<LoginViewModel, Unit>();

			OpenLoginDialog = ReactiveCommand.CreateFromTask(OpenLoginDialogImpl);
			RefreshList = ReactiveCommand.CreateFromTask(UpdateAnimeList);
		}

		private async Task OpenLoginDialogImpl()
		{
			await ShowLoginDialog.Handle(LoginViewModel);
		}

		private void OnNullUser()
		{
			Animes = null;
			IsLoading = false;
		}

		private async Task OnUserChanged()
		{
			await UpdateAnimeList();
		}

		private async Task UpdateAnimeList()
		{
			Animes = null;

			if (!LoginViewModel.IsAuthorized)
			{
				return;
			}

			IsLoading = true;

			var user = await _shikimoriApi.Users
										  .GetByNickname(_configuration.ShikimoriUsername);
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