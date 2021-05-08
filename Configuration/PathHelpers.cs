using System;
using System.IO;

namespace Configuration
{
    public static class PathHelpers
    {
        public static string FullOrRelativePath(string path)
        {
            return Path.IsPathFullyQualified(path) ? path : Path.Combine(Environment.CurrentDirectory, path);
        }
    }
}