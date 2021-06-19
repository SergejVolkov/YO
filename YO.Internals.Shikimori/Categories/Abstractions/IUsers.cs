using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IUsers
	{
		Task<User?> GetById(long id);
		Task<User?> GetByNickname(string nickname);
	}
}