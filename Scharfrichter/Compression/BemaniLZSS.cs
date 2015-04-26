using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Compression
{
    // this LZSS variant is used by beatmania 5-key
    // for compression of append data

    static public class BemaniLZSS
    {
        private static int bufferMask = 0x3FFF; // 14 bits window
        private static int bufferSize = 0x4000;

        static public void Compress(Stream source, Stream target, int length)
        {
        }

        static public void Decompress(Stream source, Stream target)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(mem))
                {
                    using (BinaryReader reader = new BinaryReader(source))
                    {
                        byte[] buffer = new byte[bufferSize];
                        int bufferOffset = 0;

                        byte data = 0;
                        int control = 0;
                        int length = 0;
                        int offset = 0;

                        while (true)
                        {
                            control >>= 1;
                            if (control < 0x100)
                                control = reader.ReadByte() | 0xFF00;

                            data = reader.ReadByte();
                            if ((control & 0x1) != 0)
                            {
                                if ((data & 0x80) != 0)
                                {
                                    // 1DDDDDDD
                                    writer.Write(data);
                                    buffer[bufferOffset] = data;
                                    bufferOffset = (bufferOffset + 1) & bufferMask;
                                    continue;
                                }
                                else if ((data & 0x40) != 0)
                                {
                                    // 01FFFFFF FFFFFFFF
                                    offset = reader.ReadByte() | ((data & 0x3F) << 8);
                                    length = 4;
                                }
                                else
                                {
                                    // 00FFFFFF FFFFFFFF LLLLLLLL
                                    offset = reader.ReadByte() | ((data & 0x3F) << 8);
                                    length = 5 + reader.ReadByte();
                                }
                            }
                            else
                            {
                                if ((data & 0x80) != 0)
                                {
                                    if ((data & 0x40) != 0)
                                    {
                                        if (data == 0xFF)
                                        {
                                            // 11111111
                                            break;
                                        }
                                        else
                                        {
                                            // 11FFFLLL FFFFFFFF
                                            offset = ((data & 0x38) << 5) | reader.ReadByte();
                                            length = (data & 0x07) + 5;
                                        }
                                    }
                                    else
                                    {
                                        // 10FFFFFF FFFFFFFF
                                        offset = reader.ReadByte() | ((data & 0x3F) << 8);
                                        length = 3;
                                    }
                                }
                                else
                                {
                                    // 0DDDDDDD
                                    writer.Write(data);
                                    buffer[bufferOffset] = data;
                                    bufferOffset = (bufferOffset + 1) & bufferMask;
                                    continue;
                                }
                            }

                            offset++;
                            while (length > 0)
                            {
                                data = reader.ReadByte();
                                writer.Write(data);
                                buffer[bufferOffset] = data;
                                bufferOffset = (bufferOffset + 1) & bufferMask;
                                length--;
                            }
                        }
                    }
                }

                using (BinaryWriter writer = new BinaryWriter(target))
                {
                    writer.Write(mem.ToArray());
                    writer.Flush();
                }
            }
        }
    }
}
