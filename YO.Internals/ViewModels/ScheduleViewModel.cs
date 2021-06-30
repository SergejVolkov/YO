using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Extensions;
using YO.Internals.Factories;
using YO.Internals.Schedule;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.ViewModels
{
	public class ScheduleViewModel : ViewModelBase
	{
		private readonly LoginViewModel _loginViewModel;
		private readonly AnimeViewModelFactory _animeViewModelFactory;
		private readonly IShikimoriApi _shikimoriApi;
		private readonly ShikimoriScheduler _scheduler;

		public ScheduleViewModel(LoginViewModel loginViewModel,
								 AnimeViewModelFactory animeViewModelFactory,
								 IShikimoriApi shikimoriApi,
								 ShikimoriScheduler scheduler)
		{
			_loginViewModel = loginViewModel;
			_animeViewModelFactory = animeViewModelFactory;
			_shikimoriApi = shikimoriApi;
			_scheduler = scheduler;

			_loginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						   .WhereNullValues()
						   .SubscribeDiscard(OnNullUser);
			
			_loginViewModel.WhenPropertyChanged(vm => vm.UserInfo)
						   .WhereNotNullValues()
						   .SubscribeAsyncDiscard(UpdateAnimeList);
		}
		
		[Reactive]
		public IDictionary<DateTime,  List<ScheduledEpisodeViewModel>>? Titles { get; private set; }
		
		[Reactive]
		public bool IsLoading { get; private set; }
		
		public async Task UpdateAnimeList()
		{
			if (!_loginViewModel.IsAuthorized)
			{
				throw new Exception("No authorization");
			}

			IsLoading = true;
			_scheduler.Clear();
			
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
			
			// workaround for https://shikimori.one/comments/7432725
			var allTitles = titles.Where(a => a.Status != AnimeStatus.Ongoing)
								  .ToList();
			foreach (var ongoing in titles.Where(a => a.Status == AnimeStatus.Ongoing))
			{
				var ongoingInfo = await _shikimoriApi.Animes.GetAnime(ongoing.Id);

				if (ongoingInfo is null)
				{
					throw new Exception("Error while ongoings loading");
				}
				
				allTitles.Add(ongoingInfo);
			}

			foreach (var anime in allTitles)
			{
				_scheduler.ScheduleAnime(anime, animeRates.Single(r => r.TargetId == anime.Id));
			}
			
			_scheduler.UpdateSchedule();

			var episodes = new Dictionary<DateTime, List<ScheduledEpisodeViewModel>>();

			foreach (var scheduledEpisode in _scheduler.ScheduledEntries)
			{
				if (!episodes.ContainsKey(scheduledEpisode.ScheduledTime))
				{
					episodes[scheduledEpisode.ScheduledTime] = new List<ScheduledEpisodeViewModel>();
				}
				
				episodes[scheduledEpisode.ScheduledTime].Add(_animeViewModelFactory.Create(scheduledEpisode));
			}

			Titles = episodes;

			IsLoading = false;
		}
		
		private void OnNullUser()
		{
			IsLoading = false;
			Titles = null;
		}
	}
}