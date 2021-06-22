using System;
using Newtonsoft.Json;

namespace YO.Internals.Shikimori.Data
{
	public class UserRate : BaseObject
	{
		[JsonProperty("user_id")]
		public long UserId { get; set; }
		
		[JsonProperty("target_id")]
		public long TargetId { get; set; }
		
		[JsonProperty("target_type")]
		public DataType TargetType { get; set; }
		
		[JsonProperty("score")]
		public int Score { get; set; }

		[JsonProperty("status")]
		public RateStatus Status { get; set; }
		
		[JsonProperty("rewatches")]
		public int Rewatches { get; set; }

		[JsonProperty("episodes")]
		public int Episodes { get; set; }

		[JsonProperty("volumes")]
		public int Volumes { get; set; }

		[JsonProperty("chapters")]
		public int Chapters { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("text_html")]
		public string RawText { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreateTime { get; set; }

		[JsonProperty("updated_at")]
		public DateTime UpdateTime { get; set; }
	}
}