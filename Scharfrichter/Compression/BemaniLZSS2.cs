using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// the decompression function is ported from unz.c (thanks Tau)

namespace Scharfrichter.Codec.Compression
{
    public enum BemaniLZSS2Type
    {
        GCZ,
        Firebeat
    }

    public struct BemaniLZSS2Properties
    {
        public int ringBufferOffset;
        public int ringBufferSize;
        public BemaniLZSS2Type type;
    }

    static public class BemaniLZSS2
    {
        static public void Compress(Stream source, Stream target, int length, BemaniLZSS2Properties props)
        {
        }

        static public void Decompress(Stream source, Stream target, int length, int decompLength, BemaniLZSS2Properties props)
        {
            byte[] ring = new byte[props.ringBufferSize];
            int ring_pos = props.ringBufferOffset; 
            int chunk_offset; 
            int chunk_length; 
            int control_word = 1;
            int controlBitsLeft = 0;
            int controlBitMask = 0x1;
            byte cmd1; 
            byte cmd2;
            byte data;

            if (decompLength <= 0)
                decompLength = int.MaxValue;

            BinaryReaderEx sourceReader = new BinaryReaderEx(source);
            BinaryWriterEx writer = new BinaryWriterEx(target);

            using (MemoryStream mem = new MemoryStream(sourceReader.ReadBytes(length)))
            {
                BinaryReaderEx reader = new BinaryReaderEx(mem);

                while (decompLength > 0 && length > 0) 
                {
                    if (controlBitsLeft == 0)
                    {
                        /* Read a control byte */
                        control_word = reader.ReadByte();
                        length--;
                        controlBitsLeft = 8;
                    }

                    /* Decode a byte according to the current control byte bit */
                    if ((control_word & controlBitMask) != 0)
                    {
                        /* Straight copy, store into history ring */
                        data = reader.ReadByte();
                        length--;

                        writer.Write(data);
                        ring[ring_pos] = data;

                        ring_pos = (ring_pos + 1) % props.ringBufferSize;
                        decompLength--;
                    } 
                    else 
                    {
                        /* Reference to data in ring buffer */

                        switch (props.type)
                        {
                            case BemaniLZSS2Type.Firebeat:
                                cmd1 = reader.ReadByte();
                                cmd2 = reader.ReadByte();
                                length -= 2;
                                chunk_length = (cmd1 & 0x0F) + 3;
                                chunk_offset = (((int)cmd1 & 0xF0) << 4) + (int)cmd2;
                                chunk_offset = ring_pos - chunk_offset;
                                while (chunk_offset < 0)
                                    chunk_offset += props.ringBufferSize;
                                break;
                            case BemaniLZSS2Type.GCZ:
                                cmd1 = reader.ReadByte();
                                cmd2 = reader.ReadByte();
                                length -= 2;
                                chunk_length = (cmd2 & 0x0F) + 3;
                                chunk_offset = (((int)cmd2 & 0xF0) << 4) | cmd1;
                                break;
                            default:
                                return;
                        }

                        for ( ; chunk_length > 0 && length > 0 ; chunk_length--) 
                        {
                            /* Copy historical data to output AND current ring pos */
                            writer.Write(ring[chunk_offset]);
                            ring[ring_pos] = ring[chunk_offset];

                            /* Update counters */
                            chunk_offset = (chunk_offset + 1) % props.ringBufferSize;
                            ring_pos = (ring_pos + 1) % props.ringBufferSize;
                            decompLength--;
                        }
                    }

                    /* Get next control bit */
                    control_word >>= 1;
                    controlBitsLeft--;
                }
            }
        }

        static public void DecompressFirebeat(Stream source, Stream target, int length, int decompLength)
        {
            BemaniLZSS2Properties props = new BemaniLZSS2Properties();
            props.ringBufferOffset = 0xFFE;
            props.ringBufferSize = 0x1000;
            props.type = BemaniLZSS2Type.Firebeat;
            Decompress(source, target, length, decompLength, props);
        }

        static public void DecompressGCZ(Stream source, Stream target, int length, int decompLength)
        {
            BemaniLZSS2Properties props = new BemaniLZSS2Properties();
            props.ringBufferOffset = 0xFFE;
            props.ringBufferSize = 0x1000;
            props.type = BemaniLZSS2Type.GCZ;
            Decompress(source, target, length, decompLength, props);
        }
    }
}
