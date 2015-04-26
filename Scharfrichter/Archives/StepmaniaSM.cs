using Scharfrichter.Codec.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class StepmaniaSM : Archive
    {
        public Dictionary<string, string> Tags = new Dictionary<string, string>();

        private class SMNoteEntry
        {
            public int Column;
            public int Measure;
            public string NoteChar;
            public int Offset;

            public SMNoteEntry()
            {
                Measure = 0;
                NoteChar = "0";
                Offset = 0;
            }

            public SMNoteEntry(Entry source, int quantize)
            {
                if (source.MetricOffsetInitialized)
                {
                    Column = source.Column;
                    Measure = source.MetricMeasure;
                    Offset = (int)Math.Round((double)(source.MetricOffset * new Fraction(quantize, 1)));
                    while (Offset >= quantize)
                    {
                        Offset -= quantize;
                        Measure++;
                    }
                    if (source.Type == EntryType.Mine)
                        NoteChar = "M";
                    else if (source.Type == EntryType.Marker)
                        NoteChar = "1";
                    else
                        NoteChar = "0";
                }
                else
                {
                    throw new Exception("Cannot create SM Note entry without metric offset");
                }
            }
        }

        public void CreateStepTag(Entry[] entries, string gameType, string description, string difficulty, string playLevel, string grooveRadar, int panelCount, int quantize)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("NOTES:");
            builder.AppendLine(gameType + ":");
            builder.AppendLine(description + ":");
            builder.AppendLine(difficulty + ":");
            builder.AppendLine(playLevel + ":");
            builder.Append(grooveRadar);

            string tagName = builder.ToString();
            int count = entries.Length;
            int highestMeasure = entries[count - 1].MetricMeasure + 2;

            builder.Clear();

            List<SMNoteEntry> noteEntries = new List<SMNoteEntry>();
            Dictionary<int, SMNoteEntry> previousEntries = new Dictionary<int,SMNoteEntry>();

            foreach (Entry entry in entries)
            {
                SMNoteEntry noteEntry = new SMNoteEntry(entry, quantize);
                if (noteEntry.NoteChar != "0")
                {
                    if (entry.Freeze)
                    {
                        if (previousEntries.ContainsKey(entry.Column))
                        {
                            previousEntries[entry.Column].NoteChar = "2";
                            previousEntries.Remove(entry.Column);
                            noteEntry.NoteChar = "3";
                        }
                    }
                    else
                    {
                        previousEntries[entry.Column] = noteEntry;
                    }
                    noteEntries.Add(noteEntry);
                }
            }

            bool firstMeasure = true;

            for (int measure = 0; measure < highestMeasure; measure++)
            {
                List<SMNoteEntry> measureEntries = new List<SMNoteEntry>();
                List<int> offsets = new List<int>();

                foreach (SMNoteEntry entry in noteEntries)
                {
                    if (entry.Measure == measure)
                    {
                        measureEntries.Add(entry);
                        offsets.Add(entry.Offset);
                    }
                }
                offsets.Add(quantize);

                if (!firstMeasure)
                    builder.Append(",");
                builder.AppendLine("   // measure " + (measure + 1).ToString());

                firstMeasure = false;

                if (measureEntries.Count > 0)
                {
                    int reduction = Util.GetLineReductionDivisor(offsets.ToArray());
                    int subdivisions = quantize / reduction;
                    if (subdivisions < 1)
                        subdivisions = 1;
                    while (subdivisions < 4)
                    {
                        subdivisions *= 2;
                        reduction /= 2;
                    }

                    string[,] measureChars = new string[subdivisions, panelCount];
                    for (int i = 0; i < subdivisions; i++)
                        for (int j = 0; j < panelCount; j++)
                            measureChars[i, j] = "0";

                    foreach (SMNoteEntry entry in measureEntries)
                        measureChars[entry.Offset / reduction, entry.Column] = entry.NoteChar;

                    for (int i = 0; i < subdivisions; i++)
                    {
                        for (int j = 0; j < panelCount; j++)
                            builder.Append(measureChars[i, j]);
                        builder.AppendLine();
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < panelCount; j++)
                            builder.Append("0");
                        builder.AppendLine();
                    }
                }
            }

            Tags[tagName] = builder.ToString();
        }

        public void CreateTempoTags(Entry[] entries)
        {
            // build the DISPLAYBPM, BPMS and STOPS tags
            string bpmTag = "";
            string stopTag = "";
            int bpmCount = entries.Length;
            double lowBPM = double.MaxValue;
            double highBPM = double.MinValue;

            for (int i = 0; i < bpmCount; i++)
            {
                Entry entry = entries[i];
                double offset = Math.Round(((double)entry.MetricOffset + (double)entry.MetricMeasure) * 4f, 3);
                double value = Math.Round((double)entry.Value, 3);

                if (value > 0)
                {
                    if (bpmTag.Length > 0)
                    {
                        bpmTag += ",";
                        if (value < lowBPM)
                            lowBPM = Math.Round(value);
                        if (value > highBPM)
                            highBPM = Math.Round(value);
                    }
                    else
                    {
                        Tags["DisplayBPM"] = Math.Round(value).ToString();
                    }

                    bpmTag += offset.ToString();
                    bpmTag += "=";
                    bpmTag += value.ToString();
                }
                else if (i < (bpmCount - 1))
                {
                    double stopLength = Math.Abs(Math.Round((double)(entries[i + 1].LinearOffset - entries[i].LinearOffset), 3));
                    if (stopTag.Length > 0)
                        stopTag += ",";
                    stopTag += offset.ToString();
                    stopTag += "=";
                    stopTag += stopLength.ToString();
                }
            }

            if (lowBPM < highBPM)
            {
                string bpmResult;
                if (lowBPM != highBPM)
                    bpmResult = lowBPM.ToString() + ":" + highBPM.ToString();
                else
                    bpmResult = lowBPM.ToString();
                Tags["DisplayBPM"] = bpmResult;
            }

            Tags["BPMs"] = bpmTag;
            Tags["Stops"] = stopTag;
        }

        public void Write(Stream target)
        {
            StreamWriter writer = new StreamWriter(target);
            foreach (KeyValuePair<string, string> tag in Tags)
            {
                string val = "#" + tag.Key + ":" + tag.Value + ";";
                writer.WriteLine(val);
            }
            writer.Flush();
        }

        public void WriteFile(string filename)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                Write(mem);
                File.WriteAllBytes(filename, mem.ToArray());
            }
        }
    }
}
