using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Sounds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class TwinkleChunk : Archive
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

        static public TwinkleChunk Read(Stream source, int[] chartOffsets, int[] sampleInfoOffsets, int sampleDataOffset)
        {
            bool foundChart = false;
            TwinkleChunk result = new TwinkleChunk();
            byte[] rawData = new byte[0x1A00000];
            source.Read(rawData, 0, rawData.Length);

            using (MemoryStream mem = new MemoryStream(rawData))
            {
                // detect charts
                if (chartOffsets != null)
                {
                    for (int i = 0; i < chartOffsets.Length; i++)
                    {
                        int offset = chartOffsets[i];
                        if (rawData[offset] == 0 && rawData[offset + 1] == 0 && rawData[offset + 2] == 0 && rawData[offset + 3] > 0 && rawData[offset + 3] <= 250)
                        {
                            mem.Position = offset;
                            Chart chart = Beatmania7Key.Read(mem);
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
                Util.ByteSwapInPlace16(rawData);
                using (MemoryStream mem = new MemoryStream(rawData))
                {
                    if (sampleInfoOffsets != null)
                    {
                        // read sample info
                        List<RF5C400.Properties> samples = new List<RF5C400.Properties>();

                        for (int i = 0; i < sampleInfoOffsets.Length; i++)
                        {
                            mem.Position = sampleInfoOffsets[i];
                            List<RF5C400.Properties> rawSamples = RF5C400.ReadSampleBlock(mem);
                            List<int> sampleMap = new List<int>();
                            int sampleIndex = 0;
                            while (true)
                            {
                                RF5C400.Properties prop = rawSamples[sampleIndex++];
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
                            result.sounds.Add(RF5C400.Read(mem, samples[i]));
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
