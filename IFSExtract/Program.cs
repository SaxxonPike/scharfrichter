using Scharfrichter.Codec;
using Scharfrichter.Codec.Archives;
using Scharfrichter.Codec.Heuristics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IFSExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DJSLACKERS - IFSExtract");
            args = Subfolder.Parse(args);

            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Debugger attached. Input file name:");
                args = new string[] { Console.ReadLine() };
                if (args[0] == "")
                {
                    args[0] = @"D:\BMS\sound\[IFS]\10006.ifs";
                    //args[0] = @"d:\bms\sound\[ifs]\01000.ifs";
                }
            }

            if (args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: IFSExtract <input file>");
                Console.WriteLine();
                Console.WriteLine("Drag and drop with files and folders is fully supported for this application.");
                Console.WriteLine();
                Console.WriteLine("Supported formats:");
                Console.WriteLine("IFS");
            }

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string filename = args[i];

                    if (File.Exists(filename) && Path.GetExtension(filename).ToUpper() == @".IFS")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Processing file " + args[i]);

                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            string outputPath = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
                            string outputFileBase = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(Path.GetFileName(filename)));

                            Directory.CreateDirectory(outputPath);

                            BemaniIFS archive = BemaniIFS.Read(fs);
                            int count = archive.RawDataCount;

                            //Console.WriteLine("Exporting files.");

                            //for (int j = 0; j < count; j++)
                            //{
                            //    string outputNumber = Util.ConvertToDecimalString(j, 4);
                            //    string outputFile = outputFileBase;
                            //    string extension = "dat";

                            //    byte[] data = archive.RawData[j];

                            //    // heuristics block
                            //    if (Heuristics.DetectBemaniModel2DXAC(data))
                            //        extension = "model";
                            //    else if (Heuristics.DetectBemani2DXArchive(data))
                            //        extension = "2dx";
                            //    else if (Heuristics.DetectBemani1(data))
                            //        extension = "1";
                            //    else if (Heuristics.DetectBemaniImage2DXAC(data))
                            //        extension = "cimg";

                            //    outputFile += "-" + outputNumber + "." + extension;
                            //    File.WriteAllBytes(outputFile, archive.RawData[j]);
                            //}
                        }
                    }
                }
            }
        }
    }
}
