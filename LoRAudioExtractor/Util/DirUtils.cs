using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoRAudioExtractor.Util
{
    public static class DirUtils
    {
        private static void RecursivelyList(ISet<string> output, string dirName, string pattern)
        {
            if (!Directory.Exists(dirName))
                return;
            
            foreach (string file in Directory.EnumerateFiles(dirName, pattern))
                output.Add(file);

            foreach (string dir in Directory.EnumerateDirectories(dirName))
                RecursivelyList(output, dir, pattern);
        }
        
        public static string[] RecursivelyList(string dir, string pattern)
        {
            HashSet<string> files = new ();
            RecursivelyList(files, dir, pattern);
            return files.ToArray();
        }
    }
}