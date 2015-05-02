using NAudio;
using NAudio.Codecs;
using NAudio.Wave;
using NAudio.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Sounds
{
    public class Sound
    {
        public int Channel = -1;
        public byte[] Data;
        public WaveFormat Format;
        public string Name = "";
        public float Panning = 0.5f;
        public bool PanningIsLinear = false;
        public float Volume = 1.0f;
        public bool VolumeIsLinear = false;

        public Sound()
        {
            Data = new byte[] { };
            Format = null;
        }

        public Sound(byte[] newData, WaveFormat newFormat)
        {
            SetSound(newData, newFormat);
        }

        static public Sound Read(Stream source)
        {
            Sound result = new Sound();
            WaveFileReader reader = new WaveFileReader(source);
            if (reader.Length > 0)
            {
                result.Data = new byte[reader.Length];
                reader.Read(result.Data, 0, result.Data.Length);
                result.Format = reader.WaveFormat;
            }
            else
            {
                result.Data = new byte[] { };
                result.Format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 44100, 2, 44100 * 4, 4, 16);
            }
            result.Panning = 0.5f;
            result.Volume = 1.0f;
            return result;
        }

        public byte[] Render(float masterVolume)
        {
            // due to the way NAudio works, the source files must be provided twice.
            // this is because all channels are kept in sync by the mux, and the unused
            // channel data is discarded. If we tried to use the same source for both
            // muxes, it would try to read 2x the data present in the buffer!
            // If only we had a way to create separate WaveProviders from within the
            // MultiplexingWaveProvider..

            try
            {
                using (MemoryStream sourceLeft = new MemoryStream(Data), sourceRight = new MemoryStream(Data))
                {
                    using (RawSourceWaveStream waveLeft = new RawSourceWaveStream(new IgnoreDisposeStream(sourceLeft), Format), waveRight = new RawSourceWaveStream(new IgnoreDisposeStream(sourceRight), Format))
                    {
                        // step 1: separate the stereo stream
                        MultiplexingWaveProvider demuxLeft = new MultiplexingWaveProvider(new IWaveProvider[] { waveLeft }, 1);
                        MultiplexingWaveProvider demuxRight = new MultiplexingWaveProvider(new IWaveProvider[] { waveRight }, 1);
                        demuxLeft.ConnectInputToOutput(0, 0);
                        demuxRight.ConnectInputToOutput(1, 0);

                        // step 2: adjust the volume of a stereo stream
                        VolumeWaveProvider16 volLeft = new VolumeWaveProvider16(demuxLeft);
                        VolumeWaveProvider16 volRight = new VolumeWaveProvider16(demuxRight);

                        float volumeValueLeft;
                        float volumeValueRight;

                        if (!PanningIsLinear)
                        {
                            // log scale is applied to each operation
                            volumeValueLeft = (float)Math.Pow(1.0f - Panning, 0.5f);
                            volumeValueRight = (float)Math.Pow(Panning, 0.5f);
                        }
                        else
                        {
                            float panValue = Panning;
                            volumeValueLeft = (float)(1.0f - Panning);
                            volumeValueRight = (float)(Panning);
                        }

                        if (!VolumeIsLinear)
                        {
                            // ensure 1:1 conversion
                            volumeValueLeft /= (float)Math.Sqrt(0.5);
                            volumeValueRight /= (float)Math.Sqrt(0.5);
                            // apply volume
                            volumeValueLeft *= (float)Math.Pow(Volume, 0.5f);
                            volumeValueRight *= (float)Math.Pow(Volume, 0.5f);
                        }
                        else
                        {
                            volumeValueLeft *= Volume;
                            volumeValueRight *= Volume;
                        }

                        // use linear scale for master volume
                        volumeValueLeft = volumeValueLeft * masterVolume;
                        volumeValueRight = volumeValueRight * masterVolume;

                        // clamp
                        volumeValueLeft = Math.Max(volumeValueLeft, 0.0f);
                        volumeValueRight = Math.Max(volumeValueRight, 0.0f);

                        // assign final volume values
                        volLeft.Volume = volumeValueLeft;
                        volRight.Volume = volumeValueRight;

                        // step 3: combine them again
                        IWaveProvider[] tracks = new IWaveProvider[] { volLeft, volRight };
                        MultiplexingWaveProvider mux = new MultiplexingWaveProvider(tracks, 2);

                        // step 4: export them to a byte array
                        byte[] finalData = new byte[Data.Length];
                        mux.Read(finalData, 0, finalData.Length);

                        // cleanup
                        demuxLeft = null;
                        demuxRight = null;
                        volLeft = null;
                        volRight = null;
                        mux = null;

                        return finalData;
                    }
                }
            }
            catch
            {
                return Data;
            }
        }

        public void SetSound(byte[] data, WaveFormat sourceFormat)
        {
            MemoryStream dataStream = new MemoryStream(data);
            RawSourceWaveStream wavStream = new RawSourceWaveStream(dataStream, sourceFormat);
            WaveStream wavConvertStream = null;

            try
            {
                wavConvertStream = WaveFormatConversionStream.CreatePcmStream(wavStream);

                // using a mux, we force all sounds to be 2 channels
                MultiplexingWaveProvider sourceProvider = new MultiplexingWaveProvider(new IWaveProvider[] { wavConvertStream }, 2);
                int bytesToRead = (int)((wavConvertStream.Length * 2) / wavConvertStream.WaveFormat.Channels);
                byte[] rawWaveData = new byte[bytesToRead];
                int bytesRead = sourceProvider.Read(rawWaveData, 0, bytesToRead);

                Data = rawWaveData;
                Format = sourceProvider.WaveFormat;

                // clean up
                sourceProvider = null;
            }
            catch
            {
                Data = data;
                Format = sourceFormat;
            }
            finally
            {
                if (wavConvertStream != null)
                    wavConvertStream.Dispose();
                wavConvertStream = null;
                wavStream.Dispose();
                wavStream = null;
                dataStream.Dispose();
                dataStream = null;
            }
        }

        public void Write(Stream target, float masterVolume)
        {
            if (Data != null && Data.Length > 0)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    using (WaveFileWriter writer = new WaveFileWriter(new IgnoreDisposeStream(mem), Format))
                    {
                        byte[] finalData = Render(masterVolume);
                        writer.Write(finalData, 0, finalData.Length);
                    }
                    target.Write(mem.ToArray(), 0, (int)mem.Length);
                }
            }
        }

        public void WriteFile(string targetFile, float masterVolume)
        {
            using (MemoryStream target = new MemoryStream())
            {
                Write(target, masterVolume);
                target.Flush();
                if (target.Length > 0)
                {
                    File.WriteAllBytes(targetFile, target.ToArray());
                }
            }
        }
    }
}
