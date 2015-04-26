using NAudio;
using NAudio.Wave;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Sounds
{
    static public class RF5C400
    {
        static public float[] VolumeTable
        {
            get
            {
                // BemaniPC and Twinkle volume tables are identical
                return Bemani2DXSound.VolumeTable;
            }
        }

        static public Sound Read(Stream source, Properties properties)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);
            Sound result = new Sound();
            bool stereo = (properties.Flag0F & 0x80) != 0;
            byte[] data = reader.ReadBytes(properties.SampleLength);
            int dataLength = data.Length;
            byte[] newData = new byte[dataLength * (stereo ? 1 : 2)];

            // translate the "signed" data
            int newDataPtr = 0;
            for (int i = 0; i < dataLength; i += 2)
            {
                int sample = ((int)data[i] << 8) | data[i + 1];
                if (sample >= 0x8000)
                {
                    sample = -(sample & 0x7FFF);
                }
                newData[newDataPtr] = (byte)(sample & 0xFF);
                newData[newDataPtr + 1] = (byte)((sample >> 8) & 0xFF);
                if (!stereo)
                {
                    newData[newDataPtr + 2] = newData[newDataPtr];
                    newData[newDataPtr + 3] = newData[newDataPtr + 1];
                    newDataPtr += 2;
                }
                newDataPtr += 2;
            }

            // 16-bit stereo format
            result.Format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, properties.Frequency, 2, properties.Frequency * 4, 4, 16);
            result.Data = newData;

            // determine volume
            result.Volume = VolumeTable[properties.Volume];

            // determine panning
            result.Panning = (float)(properties.Panning - 1) / (float)0x7E;

            // return the final result
            return result;
        }

        static public List<Properties> ReadSampleBlock(Stream source)
        {
            BinaryReader sourceReader = new BinaryReader(source);
            byte[] sourceData = sourceReader.ReadBytes(0x2000);
            int count = 256;
            List<Properties> result = new List<Properties>();

            // load sample block
            using (MemoryStream tempMem = new MemoryStream(sourceData))
            {
                BinaryReaderEx dataReader = new BinaryReaderEx(tempMem);
                for (int i = 0; i < count; i++)
                {
                    Properties props = new Properties();
                    props.Channel = dataReader.ReadByte();
                    props.Flag01 = dataReader.ReadByte();
                    props.Frequency = dataReader.ReadUInt16S();
                    props.Volume = dataReader.ReadByte();
                    props.Panning = dataReader.ReadByte();
                    props.SampleOffset = dataReader.ReadInt24S() * 2;
                    props.SampleLength = ((dataReader.ReadInt24S() * 2) - props.SampleOffset);
                    props.Value0C = dataReader.ReadInt16S();
                    props.Flag0E = dataReader.ReadByte();
                    props.Flag0F = dataReader.ReadByte();
                    props.SizeInBlocks = dataReader.ReadInt16S();
                    result.Add(props);
                }
            }

            return result;
        }

        public struct Properties
        {
            public int Channel;
            public int Flag01;
            public int Frequency;
            public int Volume;
            public int Panning;
            public int SampleOffset;
            public int SampleLength;
            public int Value0C;
            public int Flag0E;
            public int Flag0F;
            public int SizeInBlocks;
        }
    }
}
