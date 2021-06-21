using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YO.Modules
{
    [Serializable()]
    public class CacheFileCorruptedException : Exception
    {
        public CacheFileCorruptedException(string key) :
            base($"Cache file \"{key}\" corrupted!")
        { }
    }
}
