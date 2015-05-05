using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Charts
{
    static public class BeatmaniaIII
    {
        static public Chart Read(Stream source)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);

            Chart result = new Chart();
            result.TickRate = new Fraction(1, 1000);

            Dictionary<int, int> lastSample = new Dictionary<int, int>();
            int eventParameter = 0;
            int eventValue = 0;
            int eventType = 0;
            int eventOffset = 0;
            int currentOffset = 0;
            bool defaultBPMSet = false;

            // read chart header
            reader.ReadBytes(0x20);

            while (true)
            {
                currentOffset += (int)reader.ReadUInt16S();
                eventOffset = currentOffset;
                eventType = reader.ReadByte();
                int eventPlayer = reader.ReadByte();
                eventParameter = reader.ReadByte();
                eventValue = reader.ReadByte();
                eventValue |= (eventParameter & 0xF0) << 4;

                // end of chart?
                if (eventType == 0xFF)
                    break;

                Entry entry = new Entry();

                entry.LinearOffset = new Fraction(eventOffset, 1);

                int eventColumn;

                switch (eventType)
                {
                    case 0x0: // marker
                        if (eventValue >= 0x00 && eventValue <= 0x08)
                        {
                            if (eventPlayer <= 1)
                            {
                                if (eventValue == 0x05)
                                {
                                    eventValue = 0x07;
                                }
                                else if (eventValue == 0x06)
                                {
                                    eventValue = 0x05;
                                }
                                else if (eventValue == 0x07)
                                {
                                    eventValue = 0x06;
                                }
                                eventColumn = eventValue + (eventPlayer * 0x10);
                                entry.Type = EntryType.Marker;
                                entry.Column = eventValue;
                                entry.Player = eventPlayer + 1;
                                if (lastSample.ContainsKey(eventColumn))
                                {
                                    entry.Value = new Fraction(lastSample[eventColumn], 1);
                                }
                                else
                                {
                                    entry.Value = new Fraction(0, 1);
                                }
                            }
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case 0x2: // sample
                        if (eventParameter >= 0x00 && eventParameter <= 0x08)
                        {
                            if (eventPlayer <= 1)
                            {
                                if (eventParameter == 0x05)
                                {
                                    eventParameter = 0x07;
                                }
                                else if (eventParameter == 0x06)
                                {
                                    eventParameter = 0x05;
                                }
                                else if (eventParameter == 0x07)
                                {
                                    eventParameter = 0x06;
                                }
                                if (eventValue > 0)
                                {
                                    eventValue++;
                                }
                                eventColumn = eventParameter + (eventPlayer * 0x10);
                                entry.Type = EntryType.Sample;
                                entry.Player = eventPlayer + 1;
                                entry.Column = eventParameter;
                                entry.Value = new Fraction(eventValue, 1);
                                lastSample[eventColumn] = eventValue;
                            }
                            break;
                        }
                        else
                        {
                            break;
                        }
                    case 0x4: // tempo
                        entry.Type = EntryType.Tempo;
                        entry.Value = new Fraction(eventValue, 1);
                        if (!defaultBPMSet)
                        {
                            defaultBPMSet = true;
                            result.DefaultBPM = entry.Value;
                        }
                        break;
                    case 0x6: // end of song
                        entry.Type = EntryType.EndOfSong;
                        break;
                    case 0x7: // bgm
                        if (eventValue > 0)
                        {
                            eventValue++;
                        }
                        entry.Type = EntryType.Marker;
                        entry.Player = 0;
                        entry.Value = new Fraction(eventValue, 1);
                        break;
                    case 0x8: // judgement
                        entry.Type = EntryType.Judgement;
                        entry.Value = new Fraction(eventValue, 1);
                        entry.Parameter = eventParameter;
                        break;
                    case 0xB: // unknown
                        break;
                    case 0xC: // measure
                        entry.Type = EntryType.Measure;
                        entry.Player = entry.Player + 1;
                        break;
                    case 0xD: // unknown
                        break;
                    case 0xE: // bgm track?
                        entry.Type = EntryType.Marker;
                        entry.Player = 0;
                        entry.Value = new Fraction(1, 1);
                        break;
                    default:
                        break;
                }

                if (entry.Type != EntryType.Invalid)
                    result.Entries.Add(entry);
            }

            if (result.Entries.Count > 0)
            {
                result.Entries.Sort();
                result.CalculateMetricOffsets();
            }
            else
            {
                result = null;
            }

            return result;
        }
    }
}
