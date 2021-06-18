namespace YO.Internals.Configuration
{
	public interface IConfiguration
	{
		int EpisodesPerDay { get; }
		int DaysLimit { get; }
		int DelayForNewSeries { get; }
		string ShikimoriUserName { get; }
	}
}