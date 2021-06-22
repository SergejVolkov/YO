using YO.Internals.Shikimori.Categories.Abstractions;

namespace YO.Internals.Shikimori
{
	public interface IShikimoriApi
	{
		IAnimes Animes { get; }
		IUsers Users { get; }
		IUserRates UserRates { get; }
	}
}