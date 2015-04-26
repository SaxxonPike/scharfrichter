using Scharfrichter.Codec;
using Scharfrichter.Codec.Compression;
using Scharfrichter.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FirebeatDecompress
{
    class Program
    {
        static void Main(string[] args)
        {
            Splash.Show("FirebeatDecompress");
            args = Subfolder.Parse(args);

            if (args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: FirebeatDecompress <input file>");
                Console.WriteLine();
                Console.WriteLine("Drag and drop with files and folders is fully supported for this application.");
                Console.WriteLine("The output file will be the input file appended with a .DEC extension.");
                return;
            }

            foreach (string filename in args)
            {
                using (MemoryStream source = new MemoryStream(File.ReadAllBytes(filename)), target = new MemoryStream())
                {
                    BemaniLZSS2.DecompressFirebeat(source, target, (int)source.Length, 0);
                    File.WriteAllBytes(filename + ".dec", target.ToArray());
                }
            }
        }
    }
}
