using Newtonsoft.Json;

namespace YO.Internals.Shikimori.Data
{
	public class User : BaseObject
	{
		[JsonProperty("nickname")]
		public string Nickname { get; set; }

		[JsonProperty("avatar")]
		public string Avatar { get; set; }
	}
}