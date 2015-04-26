using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Media
{
    public partial class CHD
    {
        private byte[] DecompressMini(UInt64 data, UInt32 decompressedLength)
        {
            byte[] result = new byte[decompressedLength];
            byte[] buffer = new byte[8];

            buffer[0] = (byte)((data >> 56) & 0xFF);
            buffer[1] = (byte)((data >> 48) & 0xFF);
            buffer[2] = (byte)((data >> 40) & 0xFF);
            buffer[3] = (byte)((data >> 32) & 0xFF);
            buffer[4] = (byte)((data >> 24) & 0xFF);
            buffer[5] = (byte)((data >> 16) & 0xFF);
            buffer[6] = (byte)((data >> 8) & 0xFF);
            buffer[7] = (byte)(data & 0xFF);

            int j = 0;
            for (int i = 0; i < decompressedLength; i++)
            {
                result[i] = buffer[j];
                j++;
                if (j == 8)
                    j = 0;
            }

            return result;
        }

        private byte[] DecompressParentHunk(UInt64 offs)
        {
            return parent.ReadHunk((int)(offs & 0x7FFFFFFFul));
        }

        private byte[] DecompressSelfHunk(UInt64 offs)
        {
            return ReadHunk((int)(offs & 0x7FFFFFFFul));
        }

        private byte[] DecompressZlib(UInt32 compressedLength, UInt32 decompressedLength)
        {
            byte[] buffer = new byte[decompressedLength];
            using (DeflateStream ds = new DeflateStream(baseStream, CompressionMode.Decompress, true))
            {
                ds.Read(buffer, 0, (int)decompressedLength);
            }
            return buffer;
        }
    }
}
