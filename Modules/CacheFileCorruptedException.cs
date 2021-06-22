using System;

namespace YO.Modules
{
	[Serializable]
	public class CacheFileCorruptedException : Exception
	{
		public CacheFileCorruptedException(string key) :
			base($"Cache file \"{key}\" corrupted!")
		{ }
	}
}