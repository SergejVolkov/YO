using System.Collections.Generic;

namespace YO.Internals.Shikimori.Parameters
{
	public class GetAnimesParameters : ParametersBase
	{
		public int? Page
		{
			get => base["page"] as int?;
			set => base["page"] = value;
		}

		public int? Limit
		{
			get => base["limit"] as int?;
			set => base["limit"] = value;
		}

		public IEnumerable<long>? Ids
		{
			get => base["ids"] as IEnumerable<long>;
			set => base["ids"] = value;
		}
	}
}