using ConvertHelper;

using Scharfrichter.Codec;
using Scharfrichter.Codec.Archives;
using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Media;
using Scharfrichter.Codec.Sounds;
using Scharfrichter.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TwinkleIIDXExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DJSLACKERS - TwinkleIIDXExtract");

            args = Subfolder.Parse(args);

            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Debugger attached. Input file name:");
                args = new string[] { Console.ReadLine() };
                if (args[0] == "")
                {
					args[0] = @"d:\8th Style HDD.vhd";
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (File.Exists(args[i]))
                {
                    string sourceFileName = Path.GetFileNameWithoutExtension(args[i]);
                    string sourcePath = Path.GetDirectoryName(args[i]);
                    string targetPath = Path.Combine(sourcePath, sourceFileName);
                    Directory.CreateDirectory(targetPath);

                    Console.WriteLine();
                    Console.WriteLine("Processing " + args[i]);

                    Configuration config = new Configuration();
                    config["BMS"].SetDefaultValue("QuantizeMeasure", 16);
                    config["BMS"].SetDefaultValue("QuantizeNotes", 192);
                    config["BMS"].SetDefaultString("Difficulty1", "02000");
                    config["BMS"].SetDefaultString("Difficulty2", "06000");
                    config["BMS"].SetDefaultString("Difficulty3", "0A000");
                    config["BMS"].SetDefaultString("Difficulty4", "0E000");
                    config["BMS"].SetDefaultString("Difficulty5", "12000");
                    config["BMS"].SetDefaultString("Difficulty6", "16000");
                    config["BMS"].SetDefaultString("Difficulty7", "1A000");
                    config["BMS"].SetDefaultString("Difficulty8", "1E000");
                    config["IIDX"].SetDefaultString("Difficulty0", "1");
                    config["IIDX"].SetDefaultString("Difficulty1", "2");
                    config["IIDX"].SetDefaultString("Difficulty2", "3");
                    config["IIDX"].SetDefaultString("Difficulty3", "4");
                    config["IIDX"].SetDefaultString("Difficulty4", "5");
                    config["IIDX"].SetDefaultString("Difficulty5", "6");
                    config["IIDX"].SetDefaultString("Difficulty6", "7");
                    config["IIDX"].SetDefaultString("Difficulty7", "8");

                    using (FileStream fs = new FileStream(args[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int chunkLength = 0x1A00000;
                        BinaryReader reader = new BinaryReader(fs);
                        fs.Position = 0x8000000;

                        long totalChunks = (int)((fs.Length - fs.Position) / (long)chunkLength);
                        byte[] rawData = new byte[chunkLength];

                        for (int j = 0; j < totalChunks; j++)
                        {
                            fs.Read(rawData, 0, rawData.Length);

                            using (MemoryStream ms = new MemoryStream(rawData))
                            {
                                TwinkleChunk chunk = TwinkleChunk.Read(ms, new int[] { 0x002000, 0x006000, 0x00A000, 0x00E000, 0x012000, 0x016000, 0x01A000, 0x01E000 }, new int[] { 0x000000 }, 0x100000);
                                string soundPath = Path.Combine(targetPath, Util.ConvertToDecimalString(j, 3));
                                string chartPath = Path.Combine(soundPath, Util.ConvertToDecimalString(j, 3));

                                if (chunk.ChartCount > 0)
                                {
                                    Console.WriteLine("Converting set " + j.ToString());
                                    Directory.CreateDirectory(soundPath);
                                    for (int chartIndex = 0; chartIndex < chunk.ChartCount; chartIndex++)
                                    {
                                        ConvertHelper.BemaniToBMS.ConvertChart(chunk.Charts[chartIndex], config, chartPath, chartIndex, chunk.SampleMaps[0]);
                                    }
                                    Console.WriteLine("Consolidating set " + j.ToString());
                                    ConvertHelper.StereoCombiner.Process(chunk.Sounds, chunk.Charts);
                                    Console.WriteLine("Writing set " + j.ToString());
                                    ConvertHelper.BemaniToBMS.ConvertSounds(chunk.Sounds, soundPath, 0.6f);
                                }
                                else
                                {
                                    bool chunkEmpty = true;
                                    for (int k = 0; k < chunkLength; k++)
                                    {
                                        if (rawData[k] != 0)
                                        {
                                            chunkEmpty = false;
                                            break;
                                        }
                                    }
                                    if (!chunkEmpty)
                                    {
                                        Console.WriteLine("Nonempty chunk failed heuristic: " + j.ToString());
                                        File.WriteAllBytes(soundPath, rawData);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Skipping empty set " + j.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
