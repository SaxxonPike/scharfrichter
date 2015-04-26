using Scharfrichter.Codec.Sounds;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class BemaniSSP : Archive
    {
        static public BemaniSSP Read(Stream source)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);
            BemaniSSP result = new BemaniSSP();

            reader.ReadBytes(16); // name of archive

            int length = reader.ReadInt32();
            int count = (length - 18) / 4;

            for (int i = 0; i < count; i++)
            {
                int offset = reader.ReadInt32();
                if (offset >= length && offset < reader.BaseStream.Length)
                {
                    long currentOffset = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                    result.sounds.Add(BemaniSD9.Read(reader.BaseStream));
                    reader.BaseStream.Position = currentOffset;
                }
            }

            return result;
        }

        private List<Sound> sounds = new List<Sound>();

        public override int SoundCount
        {
            get
            {
                return sounds.Count;
            }
        }

        public override Sounds.Sound[] Sounds
        {
            get
            {
                return sounds.ToArray();
            }
            set
            {
                sounds.Clear();
                sounds.AddRange(value);
            }
        }
    }
}
