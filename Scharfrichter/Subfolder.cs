using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// class designed to assist in parsing directory structures

namespace Scharfrichter.Codec
{
    static public class Subfolder
    {
        static public string[] Parse(string target)
        {
            List<string> result = new List<string>();

            if (Directory.Exists(target))
            {
                // this is a directory
                DirectoryInfo dir = new DirectoryInfo(target);

                foreach (FileInfo file in dir.GetFiles())
                    result.Add(file.FullName);

                foreach (DirectoryInfo subdir in dir.GetDirectories())
                    result.AddRange(Parse(subdir.FullName));
            }

            if (File.Exists(target))
            {
                // this is a file
                result.Add(target);
            }

            return result.ToArray();
        }

        static public string[] Parse(string[] args)
        {
            List<string> result = new List<string>();

            foreach (string path in args)
                result.AddRange(Parse(path));

            return result.ToArray();
        }
    }
}
