using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Charts
{
    static public class Beatmania7Key
    {
        static public Chart Read(Stream source)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);

            Chart result = new Chart();
            result.TickRate = new Fraction(100, 5980);

            int[] lastSample = new int[16];
            int eventParameter = 0;
            int eventValue = 0;
            int eventType = 0;
            int eventOffset = 0;
            bool notecountMode = true;
            bool defaultBPMSet = false;

            while (true)
            {
                eventOffset = reader.ReadUInt16();
                eventType = reader.ReadByte();
                eventParameter = (eventType >> 4);
                eventType &= 0xF;
                eventValue = reader.ReadByte();

                // end of chart?
                if (eventOffset == 0x7FFF)
                    break;

                // ignore events in note count mode
                if (notecountMode)
                {
                    if ((eventType != 0 && eventType != 1) || eventOffset > 0)
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
                                case 0x0: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 0; entry.Value = new Fraction(lastSample[0x0], 1); break;
                                case 0x1: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 1; entry.Value = new Fraction(lastSample[0x1], 1); break;
                                case 0x2: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 2; entry.Value = new Fraction(lastSample[0x2], 1); break;
                                case 0x3: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 3; entry.Value = new Fraction(lastSample[0x3], 1); break;
                                case 0x4: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 4; entry.Value = new Fraction(lastSample[0x4], 1); break;
                                case 0x5: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 5; entry.Value = new Fraction(lastSample[0x5], 1); break;
                                case 0x6: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 6; entry.Value = new Fraction(lastSample[0x6], 1); break;
                                case 0x7: entry.Type = EntryType.Marker; entry.Player = 1; entry.Column = 7; entry.Value = new Fraction(lastSample[0x7], 1); break;
                            }
                            break;
                        case 0x1: // marker
                            switch (eventParameter)
                            {
                                case 0x0: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 0; entry.Value = new Fraction(lastSample[0x8], 1); break;
                                case 0x1: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 1; entry.Value = new Fraction(lastSample[0x9], 1); break;
                                case 0x2: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 2; entry.Value = new Fraction(lastSample[0xA], 1); break;
                                case 0x3: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 3; entry.Value = new Fraction(lastSample[0xB], 1); break;
                                case 0x4: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 4; entry.Value = new Fraction(lastSample[0xC], 1); break;
                                case 0x5: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 5; entry.Value = new Fraction(lastSample[0xD], 1); break;
                                case 0x6: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 6; entry.Value = new Fraction(lastSample[0xE], 1); break;
                                case 0x7: entry.Type = EntryType.Marker; entry.Player = 2; entry.Column = 7; entry.Value = new Fraction(lastSample[0xF], 1); break;
                            }
                            break;
                        case 0x2: // sample
                            switch (eventParameter)
                            {
                                case 0x0: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 0; lastSample[0x0] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x1: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 1; lastSample[0x1] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x2: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 2; lastSample[0x2] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x3: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 3; lastSample[0x3] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x4: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 4; lastSample[0x4] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x5: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 5; lastSample[0x5] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x6: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 6; lastSample[0x6] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x7: entry.Type = EntryType.Sample; entry.Player = 1; entry.Column = 7; lastSample[0x7] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                            }
                            break;
                        case 0x3: // sample
                            switch (eventParameter)
                            {
                                case 0x0: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 0; lastSample[0x8] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x1: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 1; lastSample[0x9] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x2: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 2; lastSample[0xA] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x3: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 3; lastSample[0xB] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x4: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 4; lastSample[0xC] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x5: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 5; lastSample[0xD] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x6: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 6; lastSample[0xE] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                                case 0x7: entry.Type = EntryType.Sample; entry.Player = 2; entry.Column = 7; lastSample[0xF] = eventValue; entry.Value = new Fraction(eventValue, 1); break;
                            }
                            break;
                        case 0x4: // tempo
                            entry.Type = EntryType.Tempo;
                            entry.Value = new Fraction((eventParameter * 256) + eventValue, 1);
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
                            entry.Type = EntryType.Marker;
                            entry.Player = 0;
                            entry.Value = new Fraction(eventValue, 1);
                            break;
                        case 0xC: // measure
                            entry.Type = EntryType.Measure; entry.Player = entry.Parameter + 1; 
                            break;
                        case 0xE: // judgement
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
