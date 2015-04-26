using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Charts
{
    public static class BeatmaniaIIDXPC
    {
        public static Chart Read(Stream source)
        {
            Chart chart = new Chart();
            BinaryReader memReader = new BinaryReader(source);
            Fraction[,] lastSample = new Fraction[9, 2];

            while (true)
            {
                Entry entry = new Entry();
                long eventOffset = memReader.ReadInt32();

                if (eventOffset >= 0x7FFFFFFF)
                    break;

                entry.LinearOffset = new Fraction(eventOffset, 1);
                entry.Value = new Fraction(0, 1);

                int eventType = memReader.ReadByte();
                int eventParameter = memReader.ReadByte();
                int eventValue = memReader.ReadUInt16();

                // unhandled parameter types:
                //  0x05: measure length
                //  0x08: judgement
                //  0x10: note count
                switch (eventType)
                {
                    case 0x00: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = eventParameter; entry.Value = lastSample[entry.Column, entry.Player - 1]; break;
                    case 0x01: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = eventParameter; entry.Value = lastSample[entry.Column, entry.Player - 1]; break;
                    case 0x02: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = eventParameter; entry.Value = new Fraction(eventValue, 1); lastSample[entry.Column, entry.Player - 1] = entry.Value; break;
                    case 0x03: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = eventParameter; entry.Value = new Fraction(eventValue, 1); lastSample[entry.Column, entry.Player - 1] = entry.Value; break;
                    case 0x04: entry.Type = EntryType.Tempo; entry.Value = new Fraction(eventValue, eventParameter); break;
                    case 0x06: entry.Type = EntryType.EndOfSong; entry.Player = eventParameter + 1; break;
                    case 0x07: entry.Type = EntryType.Marker; entry.Player = 0; entry.Value = new Fraction(eventValue, 1); entry.Parameter = eventParameter; entry.Column = 0; break;
                    case 0x08: entry.Type = EntryType.Judgement; entry.Player = 0; entry.Value = new Fraction(eventValue, 1); entry.Parameter = eventParameter; break;
                    case 0x0C: entry.Type = (eventParameter == 0 ? EntryType.Measure : EntryType.Invalid); entry.Player = eventParameter + 1; break;
                    default: entry.Type = EntryType.Invalid; break;
                }

                if (entry.Type != EntryType.Invalid)
                    chart.Entries.Add(entry);

                // if there is a value in a marker, it is a freeze
                if (entry.Type == EntryType.Marker && entry.Player > 0 && eventValue > 0)
                {
                    Entry freezeEntry = new Entry();
                    freezeEntry.Type = EntryType.Marker;
                    freezeEntry.Freeze = true;
                    freezeEntry.Player = entry.Player;
                    freezeEntry.LinearOffset = entry.LinearOffset + new Fraction(eventValue, 1);
                    freezeEntry.Column = entry.Column;
                    freezeEntry.Value = new Fraction(0, 1);
                    chart.Entries.Add(freezeEntry);
                }
            }
            
            // sort entries
            chart.Entries.Sort();

            // find the default bpm
            foreach (Entry entry in chart.Entries)
            {
                if (entry.Type == EntryType.Tempo)
                {
                    chart.DefaultBPM = entry.Value;
                    break;
                }
            }
            return chart;
        }

        public static void Write(Stream target, Chart chart)
        {
            // I don't know if these are needed, but they are
            // parameters for note count you would typically find
            // in such a chart, so we will include them

            BinaryWriter writer = new BinaryWriter(target);

            writer.Write((Int32)0);
            writer.Write((byte)0x10);    // notecount ID
            writer.Write((byte)0x00);    // player#
            writer.Write((Int16)chart.NoteCount(1));
            writer.Write((Int32)0);
            writer.Write((byte)0x10);    // notecount ID
            writer.Write((byte)0x01);    // player#
            writer.Write((Int16)chart.NoteCount(2));

            foreach (Entry entry in chart.Entries)
            {
                long num;
                long den;
                Int32 entryOffset = (Int32)(entry.LinearOffset);
                byte entryType = 0xFF;
                byte entryParameter = (byte)(entry.Parameter & 0xFF);
                Int16 entryValue = 0;

                switch (entry.Type)
                {
                    case EntryType.EndOfSong:
                        entryType = 0x06;
                        entryParameter = 0;
                        entryValue = 0;
                        break;
                    case EntryType.Judgement:
                        entryType = 0x08;
                        entryParameter = (byte)entry.Parameter;
                        entryValue = (Int16)entry.Value;
                        break;
                    case EntryType.Marker:
                        if (entry.Player < 1)
                        {
                            entryType = 0x07;
                            entryValue = (Int16)entry.Value;
                        }
                        else
                        {
                            entryType = (byte)(entry.Player - 1);
                            entryValue = 0;
                            entryParameter = (byte)entry.Column;
                        }
                        break;
                    case EntryType.Measure:
                        entryType = 0x0C;
                        entryParameter = (byte)(entry.Player - 1);
                        break;
                    case EntryType.Sample:
                        if (entry.Player > 0)
                        {
                            entryType = (byte)(entry.Player + 1);
                            entryValue = (Int16)entry.Value;
                            entryParameter = (byte)entry.Column;
                        }
                        break;
                    case EntryType.Tempo:
                        num = entry.Value.Numerator;
                        den = entry.Value.Denominator;
                        while ((num > 32767) || (den > 255))
                        {
                            num /= 2;
                            den /= 2;
                        }
                        entryValue = (Int16)num;
                        entryParameter = (byte)den;
                        entryType = 0x04;
                        break;
                    default:
                        continue;
                }
                if (entryType == 0xFF)
                    continue;

                writer.Write(entryOffset);
                writer.Write(entryType);
                writer.Write(entryParameter);
                writer.Write(entryValue);
            }
            writer.Write((Int32)0x7FFFFFFF);
            writer.Write((Int32)0);
        }
    }
}
