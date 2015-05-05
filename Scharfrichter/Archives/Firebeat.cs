using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Sounds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class FirebeatChunk : Archive
    {
        private List<Chart> charts = new List<Chart>();
        private List<Sound> sounds = new List<Sound>();

        public List<int[]> SampleMaps = new List<int[]>();

        public override int ChartCount
        {
            get
            {
                return charts.Count;
            }
        }

        public override Chart[] Charts
        {
            get
            {
                return charts.ToArray();
            }
            set
            {
                charts.Clear();
                charts.AddRange(value);
            }
        }

        static public FirebeatChunk Read(Stream source, int[] chartOffsets, int[] sampleInfoOffsets, int sampleDataOffset)
        {
            bool foundChart = false;
            FirebeatChunk result = new FirebeatChunk();
            byte[] rawData = new byte[0x2000000];
            source.Read(rawData, 0, rawData.Length);

            using (MemoryStream mem = new MemoryStream(rawData))
            {
                // detect charts
                if (chartOffsets != null)
                {
                    for (int i = 0; i < chartOffsets.Length; i++)
                    {
                        int offset = chartOffsets[i];
                        for (int j = 0; j < 0x20; j++)
                        {
                            if (rawData[j + offset] != rawData[offset])
                            {
                                foundChart = true;
                                break;
                            }
                        }

                        if (foundChart && rawData[offset + 0x20] == 0 && rawData[offset + 0x21] == 0 && rawData[offset + 0x22] != 0)
                        {
                            mem.Position = offset;
                            Chart chart = BeatmaniaIII.Read(mem);
                            if (chart != null)
                                result.charts.Add(chart);
                            else if (i == 0)
                                break; //no main chart found, don't bother ripping this
                            foundChart = true;
                        }
                    }
                }
            }

            if (foundChart)
            {
                // swap the bytes for the rest of the data
                //Util.ByteSwapInPlace16(rawData);
                using (MemoryStream mem = new MemoryStream(rawData))
                {
                    if (sampleInfoOffsets != null)
                    {
                        // read sample info
                        List<YMZ280B.Properties> samples = new List<YMZ280B.Properties>();

                        for (int i = 0; i < sampleInfoOffsets.Length; i++)
                        {
                            mem.Position = sampleInfoOffsets[i];
                            List<YMZ280B.Properties> rawSamples = YMZ280B.ReadSampleBlock(mem);
                            List<int> sampleMap = new List<int>();
                            int sampleIndex = 0;

                            // determine actual length of BGM
                            int bgmLength = 0;
                            for (int j = 0x2000000 - 4; j >= 0x0840000 + 4; j -= 4)
                            {
                                if (rawData[j] == 0x00 && rawData[j + 1] == 0x80 && rawData[j + 2] == 0x00 && rawData[j + 3] == 0x80)
                                {
                                    continue;
                                }
                                bgmLength = (j - 0x0840000) + 4;
                                break;
                            }
                            YMZ280B.Properties bgmProperty = new YMZ280B.Properties();
                            bgmProperty.Channel = 0xFF;
                            bgmProperty.Flag01 = 0x00;
                            bgmProperty.Flag0E = 0x07;
                            bgmProperty.Flag0F = 0x80;
                            bgmProperty.Frequency = 0xAC44;
                            bgmProperty.Panning = 0x40;
                            bgmProperty.SampleLength = bgmLength;
                            bgmProperty.SampleOffset = 0x0840000;
                            bgmProperty.Value0C = 0x0000;
                            bgmProperty.Volume = 0x01;
                            samples.Add(bgmProperty);
                            sampleMap.Add(1);

                            while (true)
                            {
                                YMZ280B.Properties prop = rawSamples[sampleIndex++];

                                if (prop.Frequency <= 0 || prop.SampleLength <= 0)
                                    break;

                                if (!samples.Contains(prop))
                                {
                                    samples.Add(prop);
                                    sampleMap.Add(samples.Count);
                                }
                                else
                                {
                                    sampleMap.Add(samples.IndexOf(prop) + 1);
                                }
                            }
                            result.SampleMaps.Add(sampleMap.ToArray());
                        }

                        int sampleCount = samples.Count;
                        for (int i = 0; i < sampleCount; i++)
                        {
                            mem.Position = sampleDataOffset + samples[i].SampleOffset;
                            result.sounds.Add(YMZ280B.Read(mem, samples[i]));
                        }
                    }
                }
            }

            return result;
        }

        public override int SoundCount
        {
            get
            {
                return sounds.Count;
            }
        }

        public override Sound[] Sounds
        {
            get
            {
                return sounds.ToArray();
            }
            set
            {
                sounds.Clear();
                sounds.AddRange(value);
            }
        }
    }
}
