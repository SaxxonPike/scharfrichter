using Scharfrichter.Codec.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class BemaniSSQ : Archive
    {
        private struct Chunk
        {
            public byte[] Data;
            public Int16 Parameter;
            public Int16 Type;

            static public Chunk Read(Stream source, int length)
            {
                Chunk result = new Chunk();
                BinaryReaderEx reader = new BinaryReaderEx(source);

                length -= 8;
                result.Type = reader.ReadInt16();
                result.Parameter = reader.ReadInt16();
                result.Data = reader.ReadBytes(length);

                return result;
            }

            public void Write(Stream target)
            {
                BinaryWriterEx writer = new BinaryWriterEx(target);
                Int32 length = Data.Length + 8;

                writer.Write(length);
                writer.Write(Type);
                writer.Write(Parameter);
                writer.Write(Data);
            }
        }

        private List<Chart> charts = new List<Chart>();
        private List<Chunk> extraChunks = new List<Chunk>();

        public List<Entry> TempoEntries = new List<Entry>();

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

        static private Entry[] DecodeStepChunk(Chunk source, int measureUnit)
        {
            using (BinaryReaderEx reader = new BinaryReaderEx(new MemoryStream(source.Data)))
            {
                int count = reader.ReadInt32();
                int[] metric = new int[count];
                List<Entry> result = new List<Entry>();

                for (int i = 0; i < count; i++)
                    metric[i] = reader.ReadInt32();

                byte[] step = reader.ReadBytes(count);
                byte[] freeze = reader.ReadBytes((int)reader.BaseStream.Length - (4 + (5 * count)));
                int freezeIndex = 0;
                int freezeCount = 0;

                while (freezeIndex < freeze.Length && freeze[freezeIndex] == 0)
                    freezeIndex++;

                for (int i = 0; i < count; i++)
                {
                    if (step[i] == 0)
                        freezeCount++;
                }

                for (int i = 0; i < count; i++)
                {
                    bool isShock = false;
                    bool isFreeze = false;
                    byte stepData = step[i];
                    int column = 0;

                    if (stepData == 0)
                    {
                        isFreeze = true;
                        stepData = freeze[freezeIndex];
                        freezeIndex++;
                        freezeIndex++; // freeze type, ignored for now
                    }
                    else if ((stepData & 0xF) == 0xF)
                    {
                        isShock = true;
                    }

                    while (stepData > 0)
                    {
                        if ((stepData & 1) != 0)
                        {
                            Entry entry = new Entry();
                            entry.MetricMeasure = metric[i] / measureUnit;
                            entry.MetricOffset = new Fraction(metric[i] % measureUnit, measureUnit);
                            entry.Column = column;
                            if (isFreeze)
                                entry.Freeze = true;
                            if (isShock)
                                entry.Type = EntryType.Mine;
                            else
                                entry.Type = EntryType.Marker;
                            result.Add(entry);
                        }
                        stepData >>= 1;
                        column++;
                    }
                }

                return result.ToArray();
            }
        }

        static private Entry[] DecodeTempoChunk(Chunk source, int measureUnit)
        {
            using (BinaryReaderEx reader = new BinaryReaderEx(new MemoryStream(source.Data)))
            {
                int count = reader.ReadInt32();
                int[] metric = new int[count];
                int[] linear = new int[count];

                for (int i = 0; i < count; i++)
                    metric[i] = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    linear[i] = reader.ReadInt32();

                Entry[] result = new Entry[count - 1];

                for (int i = 1; i < count; i++)
                {
                    Entry entry = new Entry();
                    int metricDiff = metric[i] - metric[i - 1];
                    int linearDiff = linear[i] - linear[i - 1];
                    Fraction metricValue = new Fraction(metricDiff * 60, measureUnit / 4);
                    Fraction linearValue = new Fraction(linearDiff, source.Parameter);
                    Fraction bpmValue = metricValue / linearValue;
                    entry.MetricMeasure = metric[i - 1] / measureUnit;
                    entry.MetricOffset = new Fraction(metric[i - 1] % measureUnit, measureUnit);
                    entry.LinearOffset = new Fraction(linear[i - 1], source.Parameter);
                    entry.Type = EntryType.Tempo;
                    entry.Value = bpmValue;
                    result[i - 1] = entry;
                }

                return result;
            }
        }

        static public BemaniSSQ Read(Stream source, int measureUnit)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);
            BemaniSSQ result = new BemaniSSQ();
            Chunk tempoChunk = new Chunk();
            List<Chunk> chartChunks = new List<Chunk>();

            // parse all chunks in the file
            while (true)
            {
                int length = reader.ReadInt32();

                if (length <= 0)
                    break;

                Chunk chunk = Chunk.Read(source, length);

                switch (chunk.Type)
                {
                    case 0x0001: // tempo info
                        tempoChunk = chunk;
                        break;
                    case 0x0003: // chart
                        chartChunks.Add(chunk);
                        break;
                    default:
                        result.extraChunks.Add(chunk);
                        break;
                }
            }

            // assemble tempo information
            result.TempoEntries.AddRange(DecodeTempoChunk(tempoChunk, measureUnit));

            // convert charts
            foreach (Chunk chartChunk in chartChunks)
            {
                Chart chart = new Chart();
                chart.Entries.AddRange(DecodeStepChunk(chartChunk, measureUnit));
                chart.Tags["Panels"] = (chartChunk.Parameter & 0xF).ToString();
                chart.Tags["Difficulty"] = ((chartChunk.Parameter & 0xFF00) >> 8).ToString();
                result.charts.Add(chart);
            }

            return result;
        }
    }
}
