using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace YO.Internals.Configuration
{
	public class Configuration : ReactiveObject, IConfiguration
	{
		[JsonProperty, Reactive]
		public string ShikimoriUsername { get; set; } = string.Empty;
	}
}