using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace YO.Internals.Configuration
{
	public class Configuration : ReactiveObject, IConfiguration
	{
		[JsonProperty, Reactive]
		public string? ShikimoriUsername { get; set; } = string.Empty;

		[JsonProperty, Reactive]
		public int EpisodesPerDay { get; set; } = 1;

		[JsonProperty, Reactive]
		public int DaysLimit { get; set; } = 7;

		[JsonProperty, Reactive]
		public int DelayForNewSeries { get; set; } = 1;
	}
}