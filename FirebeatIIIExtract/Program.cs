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

namespace FirebeatIIIExtract
{
    class Program
    {
        const int CHUNK_LENGTH = 0x2000000;
        const int DATA_START = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("DJSLACKERS - FirebeatIIIExtract");

            args = Subfolder.Parse(args);

            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Debugger attached. Input file name:");
                args = new string[] { Console.ReadLine() };
                if (args[0] == "")
                {
                    args[0] = @"d:\CHDs\iiifinal.zip";
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

                    var chartOffsets = new int[] { 0x802000, 0x807000, 0x80C000, 0x811000, 0x816000, 0x81B000, 0x820000, 0x825000, 0x82A000, 0x82F000, 0x834000, 0x839000 };
                    var sampleMapAssignment = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    var sampleMapOffsets = new int[] { 0x800000 };

                    Configuration config = new Configuration();
                    config["BMS"].SetDefaultValue("QuantizeNotes", 192);
                    config["BMS"].SetDefaultString("Difficulty1", "0x802000");
                    config["BMS"].SetDefaultString("Difficulty2", "0x807000");
                    config["BMS"].SetDefaultString("Difficulty3", "0x80C000");
                    config["BMS"].SetDefaultString("Difficulty4", "0x811000");
                    config["BMS"].SetDefaultString("Difficulty5", "0x816000");
                    config["BMS"].SetDefaultString("Difficulty6", "0x81B000");
                    config["BMS"].SetDefaultString("Difficulty7", "0x820000");
                    config["BMS"].SetDefaultString("Difficulty8", "0x825000");
                    config["BMS"].SetDefaultString("Difficulty9", "0x82A000");
                    config["BMS"].SetDefaultString("Difficulty10", "0x82F000");
                    config["BMS"].SetDefaultString("Difficulty11", "0x834000");
                    config["BMS"].SetDefaultString("Difficulty12", "0x839000");
                    config["IIDX"].SetDefaultString("Difficulty0", "1");
                    config["IIDX"].SetDefaultString("Difficulty1", "2");
                    config["IIDX"].SetDefaultString("Difficulty2", "3");
                    config["IIDX"].SetDefaultString("Difficulty3", "4");
                    config["IIDX"].SetDefaultString("Difficulty4", "5");
                    config["IIDX"].SetDefaultString("Difficulty5", "6");
                    config["IIDX"].SetDefaultString("Difficulty6", "7");
                    config["IIDX"].SetDefaultString("Difficulty7", "8");
                    config["IIDX"].SetDefaultString("Difficulty8", "9");
                    config["IIDX"].SetDefaultString("Difficulty9", "10");
                    config["IIDX"].SetDefaultString("Difficulty10", "11");
                    config["IIDX"].SetDefaultString("Difficulty11", "12");

                    var source = ConvertHelper.StreamAdapter.Open(args[i]);

                    using (var fs = source.Stream)
                    {
                        BinaryReader reader = new BinaryReader(fs);

                        long totalChunks = (int)((source.Length - DATA_START) / (long)CHUNK_LENGTH);
                        if (source.Length > CHUNK_LENGTH)
                        {
                            if (DATA_START > 0)
                            {
                                Util.DiscardBytes(fs, DATA_START);
                            }
                        }
                        else
                        {
                            totalChunks = 1;
                        }

                        var rawData = new byte[CHUNK_LENGTH];

                        for (int j = 0; j < totalChunks; j++)
                        {
                            if (fs.Read(rawData, 0, CHUNK_LENGTH) < CHUNK_LENGTH)
                            {
                                break;
                            }
                            Util.ByteSwapInPlace16(rawData);

                            using (MemoryStream ms = new MemoryStream(rawData))
                            {
                                if (rawData[0x7FFFFF] == 0)
                                {
                                    FirebeatChunk chunk = FirebeatChunk.Read(ms, chartOffsets, sampleMapOffsets, 0);
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
                                else
                                {
                                    Console.WriteLine("Skipping suspected corrupted set " + j.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
