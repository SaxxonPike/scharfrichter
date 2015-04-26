using Scharfrichter.Codec.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class Bemani1 : Archive
    {
        private Chart[] charts = new Chart[12];

        public override Chart[] Charts
        {
            get
            {
                return charts;
            }
            set
            {
                if (value.Length == 12)
                    charts = value;
            }
        }

        public override int ChartCount
        {
            get
            {
                return 12;
            }
        }

        public static Bemani1 Read(Stream source, long unitNumerator, long unitDenominator)
        {
            Bemani1 result = new Bemani1();
            long offsetBase = source.Position;
            BinaryReader reader = new BinaryReader(source);

            int[] offset = new int[12];
            int[] length = new int[12];

            for (int i = 0; i < 12; i++)
            {
                offset[i] = reader.ReadInt32();
                length[i] = reader.ReadInt32();
            }

            for (int i = 0; i < 12; i++)
            {
                if (length[i] > 0 && offset[i] >= 0x60)
                {
                    Chart chart;
                    source.Position = offsetBase + offset[i];

                    byte[] chartData = reader.ReadBytes(length[i]);
                    using (MemoryStream mem = new MemoryStream(chartData))
                    {
                        chart = BeatmaniaIIDXPC.Read(mem);
                        chart.TickRate = new Fraction(unitNumerator, unitDenominator);

                        // fill in the metric offsets
                        chart.CalculateMetricOffsets();
                    }

                    if (chart.Entries.Count > 0)
                        result.charts[i] = chart;
                    else
                        result.charts[i] = null;
                }
            }

            return result;
        }

        public void Write(Stream target, long unitNumerator, long unitDenominator)
        {
            long baseOffset = target.Position;
            int[] offset = new int[12];
            int[] length = new int[12];

            using (MemoryStream mem = new MemoryStream())
            {
                // generate the data block
                for (int i = 0; i < 12; i++)
                {
                    if (charts[i] != null)
                    {
                        baseOffset = mem.Position;
                        offset[i] = (int)baseOffset;
                        BeatmaniaIIDXPC.Write(mem, charts[i]);
                        length[i] = (int)(mem.Position - baseOffset);
                    }
                }

                BinaryWriter outputWriter = new BinaryWriter(target);
                // write the offsets and data block
                for (int i = 0; i < 12; i++)
                {
                    if (length[i] > 0)
                    {
                        outputWriter.Write((Int32)(offset[i] + 0x60));
                        outputWriter.Write((Int32)length[i]);
                    }
                    else
                    {
                        outputWriter.Write((Int32)0);
                        outputWriter.Write((Int32)0);
                    }
                }
                outputWriter.Write(mem.ToArray());
                outputWriter.Flush();
            }

            target.Flush();
        }
    }
}
