using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Sounds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class DJMainChunk : Archive
    {
        private List<Chart> charts = new List<Chart>();
        private List<byte[]> raws = new List<byte[]>();
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

        public override byte[][] RawData
        {
            get
            {
                return raws.ToArray();
            }
            set
            {
                raws = new List<byte[]>(value);
            }
        }

        public override int RawDataCount
        {
            get
            {
                return raws.Count;
            }
        }

        static public DJMainChunk Read(Stream source, int[] chartOffsets, int[] sampleInfoOffsets, int sampleDataOffset)
        {
            bool foundChart = false;
            DJMainChunk result = new DJMainChunk();
            byte[] rawData = new byte[0x1000000];
            source.Read(rawData, 0, rawData.Length);
            result.raws.Add(rawData);

            using (MemoryStream mem = new MemoryStream(rawData))
            {
                // detect charts
                if (chartOffsets != null)
                {
                    for (int i = 0; i < chartOffsets.Length; i++)
                    {
                        int offset = chartOffsets[i];
                        if (rawData[offset] == 0 && rawData[offset + 1] == 0 && rawData[offset + 2] == 0 &&
                            ((rawData[offset + 3] <= 250 && rawData[offset + 3] > 0) || (rawData[offset + 3] == 0 && rawData[offset + 4] == 0 && rawData[offset + 5] == 0 && rawData[offset + 6] != 0 && rawData[offset + 7] == 0)))
                        {
                            mem.Position = offset;
                            Chart chart = Beatmania5Key.Read(mem);
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
                using (MemoryStream mem = new MemoryStream(Util.ByteSwap(rawData, 2)))
                {
                    if (sampleInfoOffsets != null)
                    {
                        // read sample info
                        List<K054539.Properties> samples = new List<K054539.Properties>();

                        for (int i = 0; i < sampleInfoOffsets.Length; i++)
                        {
                            mem.Position = sampleInfoOffsets[i];
                            List<int> sampleMap = new List<int>();
                            while (true)
                            {
                                K054539.Properties prop = K054539.Properties.Read(mem);
                                if (prop.Panning < 0x81 || prop.Panning > 0x8F || prop.Frequency == 0 || prop.SampleType > 0xF)
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
                            mem.Position = sampleDataOffset + samples[i].Offset;
                            result.sounds.Add(K054539.Read(mem, samples[i]));
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
