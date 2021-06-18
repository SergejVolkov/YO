using YO.Internals.Shikimori.Data;

namespace YO.Internals.Shikimori.Parameters
{
	public class GetUserRatesParameters : ParametersBase
	{
		public long? UserId
		{
			get => GetParameter<long?>("user_id");
			set => SetParameter("user_id", value);
		}
		
		public long? TargetId
		{
			get => GetParameter<long?>("target_id");
			set => SetParameter("target_id", value);
		}
		
		public DataType? TargetType
		{
			get => GetParameter<DataType>("target_type");
			set => SetParameter("target_type", value);
		}
		
		public RateStatus? Status
		{
			get => GetParameter<RateStatus?>("status");
			set => SetParameter("status", value);
		}
		
		public int? Page
		{
			get => GetParameter<int?>("page");
			set => SetParameter("page", value);
		}
		
		public int? Limit
		{
			get => GetParameter<int?>("limit");
			set => SetParameter("limit", value);
		}
	}
}