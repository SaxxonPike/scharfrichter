using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Heuristics
{
    static public partial class Heuristics
    {
        static public bool DetectBemani2DXArchive(byte[] data)
        {
            if (data.Length < 0x4C)
                return false;

            using (MemoryStream mem = new MemoryStream(data))
            {
                BinaryReaderEx reader = new BinaryReaderEx(mem);
                mem.Position = 0x48;

                int offset = reader.ReadInt32();
                if ((offset < 0) || ((offset + 4) > data.Length))
                    return false;

                mem.Position = offset;
                if (new string(reader.ReadChars(4)) != "2DX9")
                    return false;

                return true;
            }
        }
    }
}
