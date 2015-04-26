using Scharfrichter.Codec;
using Scharfrichter.Codec.Compression;
using Scharfrichter.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LZDecompress
{
    class Program
    {
        static void Main(string[] args)
        {
            Splash.Show("LZDecompress");
            args = Subfolder.Parse(args);

            if (args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: LZDecompress <input file>");
                Console.WriteLine();
                Console.WriteLine("Drag and drop with files and folders is fully supported for this application.");
                Console.WriteLine("The output file will be the input file appended with a .DEC extension.");
                return;
            }

            if (args.Length > 0)
            {
                foreach (string filename in args)
                {
                    try
                    {
                        Console.Write("Decompressing " + filename + "... ");
                        byte[] data = File.ReadAllBytes(filename);
                        using (MemoryStream mem = new MemoryStream(data))
                        {
                            using (MemoryStream output = new MemoryStream())
                            {
                                BemaniLZ.Decompress(mem, output);
                                File.WriteAllBytes(filename + ".dec", output.ToArray());
                            }
                        }
                        Console.WriteLine("Success.");
                    }
                    catch
                    {
                        Console.WriteLine("FAILED.");
                    }
                }
            }
        }
    }
}
