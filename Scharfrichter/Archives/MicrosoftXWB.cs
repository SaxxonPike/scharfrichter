using NAudio.Wave;
using Scharfrichter.Codec.Sounds;
using Scharfrichter.Codec.XACT3;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class MicrosoftXWB : Archive
    {
        public string Name;

        private Sound[] sounds = { };

        public MicrosoftXWB()
        {
        }

        static public MicrosoftXWB Read(Stream source)
        {
            MicrosoftXWB result = new MicrosoftXWB();
            using (BinaryReader reader = new BinaryReader(source))
            {
                int sampleCount = 0;
                WaveBankHeader header = new WaveBankHeader();
                WaveBankData bank = new WaveBankData();
                WaveBankEntry[] entries = { };
                string[] names = { };
                MemoryStream dataChunk = new MemoryStream();

                header = WaveBankHeader.Read(source);

                for (int i = 0; i < (int)WaveBankSegIdx.Count; i++)
                {
                    WaveBankRegion region = header.Segments[i];
                    if (region.Length > 0)
                    {
                        source.Position = region.Offset;
                        MemoryStream mem = new MemoryStream(reader.ReadBytes(region.Length));
                        BinaryReader memReader = new BinaryReader(mem);
                        switch (i)
                        {
                            case (int)WaveBankSegIdx.BankData:
                                bank = WaveBankData.Read(mem);
                                sampleCount = bank.EntryCount;
                                entries = new WaveBankEntry[sampleCount];
                                names = new string[sampleCount];
                                mem.Dispose();
                                break;
                            case (int)WaveBankSegIdx.EntryMetaData:
                                for (int j = 0; j < sampleCount; j++)
                                    entries[j] = WaveBankEntry.Read(mem);
                                mem.Dispose();
                                break;
                            case (int)WaveBankSegIdx.EntryNames:
                                for (int j = 0; j < sampleCount; j++)
                                    names[j] = Util.TrimNulls(new string(memReader.ReadChars(Constants.WavebankEntrynameLength)));
                                mem.Dispose();
                                break;
                            case (int)WaveBankSegIdx.EntryWaveData:
                                dataChunk = mem;
                                break;
                            case (int)WaveBankSegIdx.SeekTables:
                                mem.Dispose();
                                break;
                            default:
                                mem.Dispose();
                                break;
                        }
                    }
                }

                if (sampleCount > 0)
                {
                    List<Sound> sounds = new List<Sound>();
                    for (int i = 0; i < sampleCount; i++)
                    {
                        WaveBankEntry entry = entries[i];
                        byte[] rawData = new byte[entry.PlayRegion.Length];
                        dataChunk.Position = entry.PlayRegion.Offset;
                        dataChunk.Read(rawData, 0, rawData.Length);

                        WaveFormat dataFormat;
                        switch (entry.Format.FormatTag)
                        {
                            case Constants.WavebankminiformatTagPcm:
                                dataFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, entry.Format.SamplesPerSec, entry.Format.Channels, entry.Format.AvgBytesPerSec, entry.Format.BlockAlign, entry.Format.BitsPerSample);
                                break;
                            case Constants.WavebankminiformatTagXma:
                                dataFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.DviAdpcm, entry.Format.SamplesPerSec, entry.Format.Channels, entry.Format.AvgBytesPerSec, entry.Format.BlockAlign, entry.Format.BitsPerSample);
                                break;
                            case Constants.WavebankminiformatTagAdpcm:
                                dataFormat = new AdpcmWaveFormat(entry.Format.SamplesPerSec, entry.Format.Channels, (short)entry.Format.BlockAlign, entry.Format.AvgBytesPerSec, (short)entry.Format.AdpcmSamplesPerBlock);
                                //dataFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Adpcm, entry.Format.SamplesPerSec, entry.Format.Channels, entry.Format.AvgBytesPerSec, entry.Format.BlockAlign, entry.Format.BitsPerSample);
                                break;
                            case Constants.WavebankminiformatTagWma:
                                dataFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.WindowsMediaAudio, entry.Format.SamplesPerSec, entry.Format.Channels, entry.Format.AvgBytesPerSec, entry.Format.BlockAlign, entry.Format.BitsPerSample);
                                break;
                            default:
                                dataFormat = null;
                                break;
                        }

                        if (dataFormat != null)
                        {
                            Sound baseSound = new Sound(rawData, dataFormat);
                            baseSound.Name = names[i];
                            sounds.Add(baseSound);
                        }
                    }
                    result.sounds = sounds.ToArray();
                    result.Name = bank.BankName;
                }
            }
            return result;
        }

        public override int SoundCount
        {
            get
            {
                return sounds.Length;
            }
        }

        public override Sounds.Sound[] Sounds
        {
            get
            {
                return sounds;
            }
            set
            {
                sounds = value;
            }
        }
    }
}
