using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Extensions;
using YO.Internals.Factories;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.ViewModels
{
	public class ScheduleViewModel : ViewModelBase
	{
		private readonly LoginViewModel _loginViewModel;
		private readonly AnimeViewModelFactory _animeViewModelFactory;
		private readonly IShikimoriApi _shikimoriApi;

		public ScheduleViewModel(LoginViewModel loginViewModel,
								 AnimeViewModelFactory animeViewModelFactory,
								 IShikimoriApi shikimoriApi)
		{
			_loginViewModel = loginViewModel;
			_animeViewModelFactory = animeViewModelFactory;
			_shikimoriApi = shikimoriApi;

			_loginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						   .WhereNullValues()
						   .SubscribeDiscard(OnNullUser);
			
			_loginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						   .WhereNotNullValues()
						   .SubscribeAsyncDiscard(UpdateAnimeList);
		}
		
		[Reactive]
		public IList<AnimeViewModel>? Titles { get; private set; }
		
		[Reactive]
		public bool IsLoading { get; private set; }
		
		public async Task UpdateAnimeList()
		{
			if (!_loginViewModel.IsAuthorized)
			{
				throw new Exception("No authorization");
			}

			IsLoading = true;
			
			var animeRates = await _shikimoriApi.UserRates
												.GetUserRates()
												.WithUserId(_loginViewModel.UserInfo!.Id)
												.WithTargetType(DataType.Anime)
												.WithStatus(RateStatus.Watching);

			if (animeRates is null || animeRates.Count == 0)
			{
				throw new Exception("Error while loading watchlist...");
			}
			
			var titles = await _shikimoriApi.Animes
											.GetAnimes()
											.WithIds(animeRates.Select(ur => ur.TargetId))
											.WithLimit(50);

			if (titles is null || titles.Count < animeRates.Count)
			{
				throw new Exception("Error while loading titles...");
			}

			Titles = titles.Select(_animeViewModelFactory.Create)
						   .ToList();

			IsLoading = false;
		}
		
		private void OnNullUser()
		{
			IsLoading = false;
			Titles = null;
		}
	}
}