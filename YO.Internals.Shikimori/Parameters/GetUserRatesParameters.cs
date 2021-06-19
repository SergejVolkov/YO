using YO.Internals.Shikimori.Data;

namespace YO.Internals.Shikimori.Parameters
{
	public class GetUserRatesParameters : ParametersBase
	{
		public long? UserId
		{
			get => (long?) base["user_id"];
			set => base["user_id"] = value;
		}
		
		public long? TargetId
		{
			get => (long?) base["target_id"];
			set => base["target_id"] = value;
		}
		
		public DataType? TargetType
		{
			get => (DataType?) base["target_type"];
			set => base["target_type"] = value;
		}
		
		public RateStatus? Status
		{
			get => (RateStatus?) base["status"];
			set => base["status"] = value;
		}
		
		public int? Page
		{
			get => (int?) base["page"];
			set => base["page"] = value;
		}
		
		public int? Limit
		{
			get => (int?) base["limit"];
			set => base["limit"] = value;
		}
	}
}