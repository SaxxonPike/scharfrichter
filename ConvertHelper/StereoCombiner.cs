using NAudio.Wave;
using Scharfrichter.Codec;
using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConvertHelper
{
    static public class StereoCombiner
    {
        static public void Process(Sound[] sounds, Chart[] charts, float amplification = 1.0f)
        {
            List<int> keysoundsUsed = new List<int>();
            List<int> bgmKeysounds = new List<int>();
            Dictionary<int, int> keysoundOccurrences = new Dictionary<int, int>();
            var firstKeysoundOccurrence = new Dictionary<int, Scharfrichter.Codec.Fraction>();

            foreach (Chart chart in charts)
            {
                foreach (Entry entry in chart.Entries)
                {
                    if (entry.Type == EntryType.Sample || entry.Type == EntryType.Marker)
                    {
                        if (entry.Value.Denominator == 1 && entry.Value.Numerator > 0)
                        {
                            int noteValue = (int)entry.Value.Numerator;

                            // determine keysounds used
                            if (!keysoundsUsed.Contains(noteValue))
                            {
                                keysoundsUsed.Add(noteValue);
                                if (!bgmKeysounds.Contains(noteValue))
                                {
                                    bgmKeysounds.Add(noteValue);
                                }
                            }

                            // count occurrences of a keysound
                            if (!keysoundOccurrences.ContainsKey(noteValue))
                            {
                                keysoundOccurrences[noteValue] = 0;
                            }
                            keysoundOccurrences[noteValue]++;

                            // determine first occurrence of a keysound
                            if (!firstKeysoundOccurrence.ContainsKey(noteValue))
                            {
                                firstKeysoundOccurrence[noteValue] = entry.Offset;
                            }

                            // sounds outside of BGM are not eligible to be combined
                            if (entry.Player != 0)
                            {
                                bgmKeysounds.Remove(noteValue);
                            }
                        }
                    }
                }
            }

            int count = sounds.Length;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (bgmKeysounds.Contains(i + 1) && bgmKeysounds.Contains(j + 1))
                    {
                        if (
                            Math.Abs(sounds[i].Data.Length - sounds[j].Data.Length) <= (sounds[i].Data.Length / 100) && // length difference within tolerance
                            Math.Abs(sounds[i].Panning - sounds[j].Panning) == 1 &&                                     // panning is opposite
                            keysoundOccurrences[i + 1] == keysoundOccurrences[j + 1] &&                                 // occurrence count matches
                            firstKeysoundOccurrence[i + 1] == firstKeysoundOccurrence[j + 1]                            // first occurrence matches
                            )
                        {
                            byte[] render0 = sounds[i].Render(1.0f);
                            byte[] render1 = sounds[j].Render(1.0f);
                            byte[] output = Util.Sum16(render0, render1);
                            sounds[i].SetSound(output, sounds[i].Format);
                            sounds[j].SetSound(new byte[] { }, sounds[j].Format);
                            sounds[i].Panning = 0.5f;
                            sounds[i].Volume = amplification;
                        }
                    }
                }
            }
        }
    }
}
