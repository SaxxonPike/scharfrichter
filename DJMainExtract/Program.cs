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

namespace DJMainExtract
{
    class Program
    {
        const int CHUNK_LENGTH = 0x1000000;

        static int[] chartOffsets;
        static Configuration config;
        static int[] sampleMapAssignment;
        static int[] sampleMapOffsets;
        static int soundOffset;
        static float stereoAmp = (float)Math.Sqrt(2);
        static long totalChunks;

        enum Mixes : int
        {
            Unidentified = 0,
            First,
            Second,
            Third,
            Complete,
            Fourth,
            Fifth,
            Complete2,
            Club,
            DCT,
            Core,
            Sixth,
            Seventh,
            Final
        }

        static string FormatLogValue(int v)
        {
            string result = v.ToString();
            if (result.Length >= 8)
            {
                result = result.Substring(0, 7);
            }
            return result;
        }

        static string FormatLogValue(float v)
        {
            string result = v.ToString();
            if (result.Length >= 8)
            {
                result = result.Substring(0, 7);
            }
            return result;
        }

        static Mixes IdentifyMix(Stream fs)
        {
            Mixes identifiedMix = Mixes.Unidentified;
            var enc = Encoding.GetEncoding(437);
            byte[] nameBytes = new byte[6];

            byte[] chunk0 = new byte[CHUNK_LENGTH];
            fs.Read(chunk0, 0, CHUNK_LENGTH);
            Array.Copy(chunk0, nameBytes, nameBytes.Length);

            Util.ByteSwapInPlace16(nameBytes);
            string name = enc.GetString(nameBytes).Trim().ToUpperInvariant();


            switch (name)
            {
                case @"GQ753": identifiedMix = Mixes.First; break;
                case @"GX853": identifiedMix = Mixes.Second; break;
                case @"GQ825": identifiedMix = Mixes.Third; break;
                case @"GQ847": identifiedMix = Mixes.Fourth; break;
                case @"GQ981": identifiedMix = Mixes.Fifth; break;
                case @"GCA21": identifiedMix = Mixes.Sixth; break;
                case @"GEB07": identifiedMix = Mixes.Seventh; break;
                case @"GQ993": identifiedMix = Mixes.Club; break;
                case @"GQ858": identifiedMix = Mixes.Complete; break;
                case @"GQ988": identifiedMix = Mixes.Complete2; break;
                case @"GQA05": identifiedMix = Mixes.Core; break;
                case @"GQ995": identifiedMix = Mixes.DCT; break;
                case @"GCC01": identifiedMix = Mixes.Final; break;
            }

            if (identifiedMix == Mixes.First || identifiedMix == Mixes.Second || (identifiedMix == Mixes.Unidentified && totalChunks <= 64))
            {
                // 1st, 2nd
                Console.WriteLine("Using 1gb config");
                soundOffset = 0x2000;
                chartOffsets = new int[] { 0x000400 };
                config["BMS"].SetDefaultString("Difficulty1", "normal");
                config["IIDX"].SetDefaultString("Difficulty0", "1");
            }
            else if (identifiedMix == Mixes.Third || identifiedMix == Mixes.Complete || (identifiedMix == Mixes.Unidentified && totalChunks <= 128))
            {
                // 3rd, complete
                Console.WriteLine("Using 2gb config");
                soundOffset = 0x2000;
                chartOffsets = new int[] { 0x000400, 0xF02000, 0xF03000 };
                config["BMS"].SetDefaultString("Difficulty1", "light");
                config["BMS"].SetDefaultString("Difficulty2", "normal");
                config["BMS"].SetDefaultString("Difficulty3", "another");
                config["IIDX"].SetDefaultString("Difficulty0", "2");
                config["IIDX"].SetDefaultString("Difficulty1", "1");
                config["IIDX"].SetDefaultString("Difficulty2", "3");
            }
            else if (identifiedMix == Mixes.Fourth || identifiedMix == Mixes.Fifth)
            {
                // 4th, 5th
                Console.WriteLine("Using 2gb alternate config");
                soundOffset = 0x2000;
                chartOffsets = new int[] { 0x000800, 0xF02000, 0xF03000 };
                config["BMS"].SetDefaultString("Difficulty1", "light");
                config["BMS"].SetDefaultString("Difficulty2", "normal");
                config["BMS"].SetDefaultString("Difficulty3", "another");
                config["IIDX"].SetDefaultString("Difficulty0", "2");
                config["IIDX"].SetDefaultString("Difficulty1", "1");
                config["IIDX"].SetDefaultString("Difficulty2", "3");
            }
            else if (totalChunks <= 260)
            {
                // club, complete2, core, dct
                Console.WriteLine("Using 4gb config");
                soundOffset = 0x2000;
                chartOffsets = new int[] { 0x000800, 0xF02000, 0xF03000 };
                config["BMS"].SetDefaultString("Difficulty1", "light");
                config["BMS"].SetDefaultString("Difficulty2", "normal");
                config["BMS"].SetDefaultString("Difficulty3", "another");
                config["IIDX"].SetDefaultString("Difficulty0", "2");
                config["IIDX"].SetDefaultString("Difficulty1", "1");
                config["IIDX"].SetDefaultString("Difficulty2", "3");
            }
            else if (totalChunks <= 400)
            {
                // 6th, 7th
                Console.WriteLine("Using 6gb config");
                soundOffset = 0x2000;
                chartOffsets = new int[] { 0x000800, 0xF02000, 0xF03000 };
                config["BMS"].SetDefaultString("Difficulty1", "light");
                config["BMS"].SetDefaultString("Difficulty2", "normal");
                config["BMS"].SetDefaultString("Difficulty3", "another");
                config["IIDX"].SetDefaultString("Difficulty0", "2");
                config["IIDX"].SetDefaultString("Difficulty1", "1");
                config["IIDX"].SetDefaultString("Difficulty2", "3");
            }
            else
            {
                // final
                Console.WriteLine("Using FINAL config");
                soundOffset = 0x20000;
                chartOffsets = new int[] { 0x002000, 0x006000, 0x00A000, 0x00E000, 0x012000, 0x016000 };
                sampleMapOffsets = new int[] { 0x000000, 0x001000 };
                config["BMS"].SetDefaultString("Difficulty1", "normal");
                config["BMS"].SetDefaultString("Difficulty2", "light");
                config["BMS"].SetDefaultString("Difficulty3", "another");
                config["IIDX"].SetDefaultString("Difficulty0", "2");
                config["IIDX"].SetDefaultString("Difficulty1", "1");
                config["IIDX"].SetDefaultString("Difficulty2", "3");
                config["IIDX"].SetDefaultString("Difficulty3", "2");
                config["IIDX"].SetDefaultString("Difficulty4", "1");
                config["IIDX"].SetDefaultString("Difficulty5", "3");
                sampleMapAssignment = new int[] { 0, 0, 0, 1, 1, 1 };
            }

            return identifiedMix;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("DJSLACKERS - DJMainExtract");

            args = Subfolder.Parse(args);

            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Debugger attached. Input file name:");
                args = new string[] { Console.ReadLine() };
                if (args[0] == "")
                {
                    args[0] = @"D:\chds\bmfinal.zip";
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

                    // default config
                    config = new Configuration();
                    config["BMS"].SetDefaultValue("QuantizeMeasure", 16);
                    config["BMS"].SetDefaultValue("QuantizeNotes", 192);
                    sampleMapOffsets = new int[] { 0x000000, 0x000200 };
                    sampleMapAssignment = new int[] { 0, 0, 0, 0, 0, 0 };
                    soundOffset = 0x002000;

                    var source = ConvertHelper.StreamAdapter.Open(args[i]);
                    using (var fs = source.Stream)
                    {
                        int chunkLength = 0x1000000;

                        BinaryReader reader = new BinaryReader(fs);

                        totalChunks = (long)(source.Length - chunkLength) / (long)chunkLength;
                        var identifiedMix = IdentifyMix(fs);

                        if (totalChunks >= 1)
                        {
                            byte[] rawData = new byte[chunkLength];

                            for (int j = 0; j < totalChunks; j++)
                            {
                                fs.Read(rawData, 0, CHUNK_LENGTH);

                                using (MemoryStream ms = new MemoryStream(rawData))
                                {
                                    var chunk = DJMainChunk.Read(ms, chartOffsets, sampleMapOffsets, soundOffset);
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
                                        ConvertHelper.StereoCombiner.Process(chunk.Sounds, chunk.Charts, stereoAmp);
                                        Console.WriteLine("Writing set " + j.ToString());
                                        ConvertHelper.BemaniToBMS.ConvertSounds(chunk.Sounds, soundPath, 0.6f);
                                    }
                                    else
                                    {
                                        bool chunkEmpty = true;
                                        byte byteZero = rawData[0];
                                        for (int k = 0; k < chunkLength; k++)
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
}
