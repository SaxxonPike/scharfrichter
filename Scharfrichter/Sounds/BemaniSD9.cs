using NAudio;
using NAudio.Wave;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Sounds
{
    static public class BemaniSD9
    {
        static public Sound Read(Stream source)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);
            Sound result = new Sound();

            int id = reader.ReadInt32();
            if (id == 0x00394453)
            {
                int headerLength = reader.ReadInt32();
                int sampleLength = reader.ReadInt32();

                headerLength -= 12;
                if (headerLength > 0)
                    reader.ReadBytes(headerLength);

                byte[] wavData = reader.ReadBytes(sampleLength);
                using (MemoryStream wavDataMem = new MemoryStream(wavData))
                {
                    using (WaveStream wavStream = new WaveFileReader(wavDataMem))
                    {
                        byte[] rawWaveData = new byte[wavStream.Length];
                        wavStream.Read(rawWaveData, 0, (int)wavStream.Length);
                        result.SetSound(rawWaveData, wavStream.WaveFormat);
                    }
                }
            }

            return result;
        }
    }
}
