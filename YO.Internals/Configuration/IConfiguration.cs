namespace YO.Internals.Configuration
{
	public interface IConfiguration
	{
		string? ShikimoriUsername { get; set; }
		int EpisodesPerDay { get; set; }
		int DaysLimit { get; set; }
		int DelayForNewSeries { get; set; }
	}
}