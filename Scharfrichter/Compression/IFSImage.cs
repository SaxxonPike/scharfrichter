using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Compression
{
    static public class IFSImage
    {
        static public void Decompress(Stream source, Stream target)
        {
            BinaryReaderEx reader = new BinaryReaderEx(source);

            // unsure what this is- probably decompressed size
            int decompressedSize = reader.ReadInt32S();

            // read length
            int length = reader.ReadInt32S();

            // decompress
            BemaniLZSS2.DecompressGCZ(source, target, length, decompressedSize);
        }
    }
}
