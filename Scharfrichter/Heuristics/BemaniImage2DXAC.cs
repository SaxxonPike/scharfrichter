using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Heuristics
{
    static public partial class Heuristics
    {
        static public bool DetectBemaniImage2DXAC(byte[] data)
        {
            if (data.Length < 8)
                return false;

            using (MemoryStream mem = new MemoryStream(data))
            {
                BinaryReaderEx reader = new BinaryReaderEx(mem);

                mem.Position = 4;

                int compressedSize = reader.ReadInt32S();
                if (compressedSize != (mem.Length - 8))
                    return false;
            }

            return true;
        }
    }
}
