using NAudio;
using NAudio.Wave;
using NAudio.Utils;

using Scharfrichter.Codec.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Sounds
{
    static public class ChartRenderer
    {
        static private long GetIdentifier(Entry entry)
        {
            long result = (long)entry.Column;
            result <<= 32;
            result |= (long)(entry.Player) & 0xFFFFFFFFL;

            return result;
        }

        static private void Paste(byte[] sourceRendered, ref int[] target, Fraction offset, Fraction cutoffFraction)
        {
            if (sourceRendered == null)
                return;

            int sourceLength = sourceRendered.Length;

            int desiredOffset = (int)(offset * new Fraction(88200, 1));
            int desiredLength = (sourceRendered.Length / 2) + (int)desiredOffset;
            int cutoff = (int)(cutoffFraction * new Fraction(88200, 1));

            if (cutoff >= 0 && desiredOffset + (sourceLength / 4) > cutoff)
            {
                sourceLength = (cutoff - desiredOffset) * 4;
            }

            if (target.Length < desiredLength)
                Array.Resize(ref target, desiredLength);

            Int32 sourceSampleL = 0;
            Int32 sourceSampleR = 0;
            int sourceIndex = 0;
            int targetIndex = desiredOffset;

            while (sourceIndex < sourceLength - 3)
            {
                sourceSampleL = sourceRendered[sourceIndex++];
                sourceSampleL |= (int)(sourceRendered[sourceIndex++]) << 8;
                sourceSampleL <<= 16;
                sourceSampleL >>= 16;
                sourceSampleR = sourceRendered[sourceIndex++];
                sourceSampleR |= (int)(sourceRendered[sourceIndex++]) << 8;
                sourceSampleR <<= 16;
                sourceSampleR >>= 16;
                target[targetIndex++] += sourceSampleL;
                target[targetIndex++] += sourceSampleR;
            }
        }

        static public byte[] Render(Chart chart, Sound[] sounds)
        {
            Dictionary<long, Entry> lastNote = new Dictionary<long, Entry>();
            Dictionary<int, Fraction> noteCutoff = new Dictionary<int, Fraction>();
            Dictionary<int, byte[]> renderedSamples = new Dictionary<int, byte[]>();

            int[] buffer = new int[0];

            chart.Entries.Reverse();

            foreach (Entry entry in chart.Entries)
            {
                if (entry.Type == EntryType.Sample)
                {
                    lastNote[GetIdentifier(entry)] = entry;
                }
                else if (entry.Type == EntryType.Marker)
                {
                    if (entry.Value.Numerator > 0)
                    {
                        byte[] soundData = null;
                        var soundIndex = (int)entry.Value - 1;
                        var sound = soundIndex < sounds.Length ? sounds[soundIndex] : null;

                        if (renderedSamples.ContainsKey(soundIndex))
                        {
                            soundData = renderedSamples[soundIndex];
                        }
                        else if (sound != null)
                        {
                            soundData = sound.Render(1.0f);
                            renderedSamples[soundIndex] = soundData;
                        }

                        var cutoff = new Fraction(-1, 1);
                        if (sound != null && sound.Channel >= 0 && noteCutoff.ContainsKey(sound.Channel))
                        {
                            cutoff = noteCutoff[sound.Channel];
                        }
                        if (soundData != null)
                        {
                            Paste(soundData, ref buffer, entry.LinearOffset * chart.TickRate, cutoff * chart.TickRate);
                        }
                        if (sound != null && sound.Channel >= 0)
                            noteCutoff[sound.Channel] = entry.LinearOffset;
                    }
                }
            }

            chart.Entries.Reverse();

            int length = buffer.Length;
            Int16[] outputSamples = new Int16[length];
            int normalization = 1;

            for (int i = 0; i < length; i++)
            {
                // auto-normalize
                int currentSample = buffer[i] / normalization;
                while (currentSample > 32767 || currentSample < -32768)
                {
                    normalization++;
                    currentSample = buffer[i] / normalization;
                }
            }

            for (int i = 0; i < length; i++)
            {
                outputSamples[i] = (Int16)(buffer[i] / normalization);
            }

            using (MemoryStream mem = new MemoryStream())
            {
                using (WaveFileWriter writer = new WaveFileWriter(new IgnoreDisposeStream(mem), WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 44100, 2, 44100 * 4, 4, 16)))
                {
                    writer.WriteSamples(outputSamples, 0, length);
                }
                mem.Flush();
                return mem.ToArray();
            }
        }
    }
}
