using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Fluent;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IUsers
	{
		FluentApiRequest<User?> GetById(long id);
		FluentApiRequest<User?> GetByNickname(string nickname);
	}
}