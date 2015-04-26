using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Heuristics
{
    static public partial class Heuristics
    {
        static public bool DetectBemani1(byte[] data)
        {
            if (data.Length < 0x68)
                return false;

            using (MemoryStream mem = new MemoryStream(data))
            {
                BinaryReaderEx reader = new BinaryReaderEx(mem);
                mem.Position = mem.Length - 8;

                int endMarkerOffset = reader.ReadInt32();
                int endMarkerData = reader.ReadInt32();

                if (endMarkerData != 0x00000000 || endMarkerOffset != 0x7FFFFFFF)
                    return false;
            }

            return true;
        }
    }
}
