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

    [Serializable()]
    public class TagReaderException : Exception
    {
        public TagReaderException(string message, int line, string parent, string word, char separator, Tag cause = null, string info = null) :
            base(message + $"\nreading file, line #{line}\n" + cause + $"\n{(info == null ? "" : info + "\r    ")}" +
                $"parent =\"{(parent == "" ? "none" : parent)}\"\r    word=\"{word}\"\r    separator=\'{(separator == '\r' || separator == '\n' ? "" : separator.ToString())}\'")
        { }
    }

    [Serializable()]
    public class TagStreamReaderException : Exception
    {
        public TagStreamReaderException(string message, int line, string word, string info = null) :
            base(message + $"reading file, line #{line}\n{(info == null ? "" : info + "\r    ")}word=\"{(word == "" ? "unavailable" : word)}\"")
        { }
    }
}
