using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Charts
{
    static public class Beatmania5Key
    {
        static public Chart Read(Stream source)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);

            Chart result = new Chart();
            result.TickRate = new Fraction(1, 58);

            int[] lastSample = new int[16];
            int eventParameter = 0;
            int eventValue = 0;
            int eventType = 0;
            int eventOffset = 0;
            bool notecountMode = true;
            bool defaultBPMSet = false;

            while (true)
            {
                try
                {
                    eventOffset = reader.ReadUInt16();
                    eventType = reader.ReadByte();
                    eventParameter = (eventType >> 4);
                    eventType &= 0xF;
                    eventValue = reader.ReadByte();
                }
                catch (Exception)
                {
                    break;
                }

                // end of chart?
                if (eventOffset == 0x7FFF)
                    break;

                // ignore events in note count mode
                if (notecountMode)
                {
                    if (eventType != 0 || eventOffset > 0)
                        notecountMode = false;
                }

                // process events
                if (!notecountMode)
                {
                    Entry entry = new Entry();

                    entry.LinearOffset = new Fraction(eventOffset, 1);

                    switch (eventType)
                    {
                        case 0x0: // marker
                            switch (eventParameter)
                            {
                                case 0x0: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 0; entry.Value = new Fraction(lastSample[0x0], 1); break; //key0
                                case 0x1: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 0; entry.Value = new Fraction(lastSample[0x1], 1); break;
                                case 0x2: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 1; entry.Value = new Fraction(lastSample[0x2], 1); break; //key1
                                case 0x3: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 1; entry.Value = new Fraction(lastSample[0x3], 1); break;
                                case 0x4: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 2; entry.Value = new Fraction(lastSample[0x4], 1); break; //key2
                                case 0x5: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 2; entry.Value = new Fraction(lastSample[0x5], 1); break;
                                case 0x6: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 3; entry.Value = new Fraction(lastSample[0x6], 1); break; //key3
                                case 0x7: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 3; entry.Value = new Fraction(lastSample[0x7], 1); break;
                                case 0x8: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 4; entry.Value = new Fraction(lastSample[0x8], 1); break; //key4
                                case 0x9: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 4; entry.Value = new Fraction(lastSample[0x9], 1); break;
                                case 0xA: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 7; entry.Value = new Fraction(lastSample[0xA], 1); break; //scratch
                                case 0xB: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 7; entry.Value = new Fraction(lastSample[0xB], 1); break;
                                case 0xC: entry.Type = EntryType.Measure; entry.Player = 1; break;
                                case 0xD: entry.Type = EntryType.Measure; entry.Player = 2; break;
                                case 0xE: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 8; entry.Value = new Fraction(lastSample[0xA], 1); break; //freescratch
                                case 0xF: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 8; entry.Value = new Fraction(lastSample[0xB], 1); break;
                            }
                            break;
                        case 0x1: // sample
                            switch (eventParameter)
                            {
                                case 0x0: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 0; lastSample[0x0] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x1: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 0; lastSample[0x1] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x2: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 1; lastSample[0x2] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x3: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 1; lastSample[0x3] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x4: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 2; lastSample[0x4] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x5: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 2; lastSample[0x5] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x6: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 3; lastSample[0x6] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x7: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 3; lastSample[0x7] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x8: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 4; lastSample[0x8] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x9: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 4; lastSample[0x9] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0xA: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 7; lastSample[0xA] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0xB: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 7; lastSample[0xB] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0xE: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 8; lastSample[0xA] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0xF: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 8; lastSample[0xB] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                            }
                            break;
                        case 0x2: // tempo
                            entry.Type = EntryType.Tempo;
                            entry.Value = new Fraction((eventParameter * 256) + eventValue, 1);
                            if (!defaultBPMSet)
                            {
                                defaultBPMSet = true;
                                result.DefaultBPM = entry.Value;
                            }
                            break;
                        case 0x4: // end of song
                            entry.Type = EntryType.EndOfSong;
                            break;
                        case 0x5: // bgm
                            entry.Type = EntryType.Marker;
                            entry.Player = 0;
                            entry.Value = new Fraction(eventValue, 1);
                            break;
                        case 0x6: // judgement
                            entry.Type = EntryType.Judgement;
                            entry.Value = new Fraction(eventValue, 1);
                            entry.Parameter = eventParameter;
                            break;
                    }

                    if (entry.Type != EntryType.Invalid)
                        result.Entries.Add(entry);
                }
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
