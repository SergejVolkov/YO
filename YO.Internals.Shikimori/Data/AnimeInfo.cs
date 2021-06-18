using System;
using Newtonsoft.Json;

namespace YO.Internals.Shikimori.Data
{
	public class AnimeInfo : BaseObject
	{
		[JsonProperty("status")]
		public AnimeStatus Status { get; set; }
		
		[JsonProperty("episodes")]
		public int Episodes { get; set; }

		[JsonProperty("episodes_aired")]
		public int AiredEpisodes { get; set; }

		[JsonProperty("updated_at")]
		public DateTime? NextEpisodeTime { get; set; }
	}
}