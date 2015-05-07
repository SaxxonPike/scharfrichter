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
        const int CHUNK_LENGTH = 0x1A00000;
        const int DATA_START = 0x8000000;

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
                    args[0] = @"d:\CHDs\iidx8th.zip";
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

                    var chartOffsets = new int[] { 0x002000, 0x006000, 0x00A000, 0x00E000, 0x012000, 0x016000, 0x01A000, 0x01E000 };
                    var sampleMapAssignment = new int[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                    var sampleMapOffsets = new int[] { 0x000000 };

                    Configuration config = new Configuration();
                    config["BMS"].SetDefaultString("Difficulty1", "5key");
                    config["BMS"].SetDefaultString("Difficulty2", "light");
                    config["BMS"].SetDefaultString("Difficulty3", "normal");
                    config["BMS"].SetDefaultString("Difficulty4", "another");
                    config["BMS"].SetDefaultString("Difficulty5", "12000");
                    config["BMS"].SetDefaultString("Difficulty6", "16000");
                    config["BMS"].SetDefaultString("Difficulty7", "1A000");
                    config["BMS"].SetDefaultString("Difficulty8", "1E000");
                    config["IIDX"].SetDefaultString("Difficulty0", "3");
                    config["IIDX"].SetDefaultString("Difficulty1", "2");
                    config["IIDX"].SetDefaultString("Difficulty2", "1");
                    config["IIDX"].SetDefaultString("Difficulty3", "4");
                    config["IIDX"].SetDefaultString("Difficulty4", "3");
                    config["IIDX"].SetDefaultString("Difficulty5", "2");
                    config["IIDX"].SetDefaultString("Difficulty6", "4");
                    config["IIDX"].SetDefaultString("Difficulty7", "8");

                    var source = ConvertHelper.StreamAdapter.Open(args[i]);
                    using (var fs = source.Stream)
                    {
                        BinaryReader reader = new BinaryReader(fs);

                        long totalChunks = (int)((source.Length - DATA_START) / (long)CHUNK_LENGTH);
                        if (source.Length > CHUNK_LENGTH)
                        {
                            Util.DiscardBytes(fs, DATA_START);
                        }
                        else
                        {
                            totalChunks = 1;
                        }

                        byte[] rawData = new byte[CHUNK_LENGTH];

                        for (int j = 0; j < totalChunks; j++)
                        {
                            Console.WriteLine("Reading " + j.ToString());
                            if (fs.Read(rawData, 0, CHUNK_LENGTH) < CHUNK_LENGTH)
                            {
                                break;
                            }

                            using (MemoryStream ms = new MemoryStream(rawData))
                            {
                                TwinkleChunk chunk = TwinkleChunk.Read(ms, chartOffsets, sampleMapOffsets, 0x100000);
                                string soundPath = Path.Combine(targetPath, Util.ConvertToDecimalString(j, 3));
                                string chartPath = Path.Combine(soundPath, Util.ConvertToDecimalString(j, 3));

                                if (chunk.ChartCount > 0)
                                {
                                    Console.WriteLine("Converting set " + j.ToString());
                                    Directory.CreateDirectory(soundPath);
                                    for (int chartIndex = 0; chartIndex < chunk.ChartCount; chartIndex++)
                                    {
                                        ConvertHelper.BemaniToBMS.ConvertChart(chunk.Charts[chartIndex], config, chartPath, chartIndex, chunk.SampleMaps[sampleMapAssignment[chartIndex]]);
                                    }
                                    Console.WriteLine("Consolidating set " + j.ToString());
                                    ConvertHelper.StereoCombiner.Process(chunk.Sounds, chunk.Charts);
                                    Console.WriteLine("Writing set " + j.ToString());
                                    ConvertHelper.BemaniToBMS.ConvertSounds(chunk.Sounds, soundPath, 0.6f);
                                }
                                else
                                {
                                    bool chunkEmpty = true;
                                    byte byteZero = rawData[0];
                                    for (int k = 0; k < CHUNK_LENGTH; k++)
                                    {
                                        if (rawData[k] != byteZero)
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
