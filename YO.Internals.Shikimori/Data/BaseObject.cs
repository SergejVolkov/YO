using Newtonsoft.Json;

namespace YO.Internals.Shikimori.Data
{
	public abstract class BaseObject
	{
		[JsonProperty("id")]
		public long Id { get; set; }
	}
}