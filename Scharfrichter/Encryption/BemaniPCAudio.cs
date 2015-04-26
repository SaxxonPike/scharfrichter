using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Encryption
{
    // most of this was ported from crack2dx.c (thanks Tau)

    public enum Bemani2DXEncryptionType
    {
        Standard,
        Partial
    }

    public static class Bemani2DXEncryptionKeys
    {
        static public byte[] IIDX9 = new byte[]
        {
            0x97, 0x1E, 0x24, 0xA0, 0x9A, 0x00, 0x10, 0x2B,
            0x91, 0xEF, 0xD7, 0x7A, 0xCD, 0x11, 0xAF, 0xAF,
            0x8D, 0x26, 0x5D, 0xBB, 0xE0, 0xC6, 0x1B, 0x2B                
        };

        static public byte[] IIDX10 = new byte[]
        {
            0x2D, 0x86, 0x56, 0x62, 0xD7, 0xFD, 0xCA, 0xA4,
            0xB3, 0x24, 0x60, 0x26, 0x24, 0x81, 0xDB, 0xC2,
            0x57, 0xB1, 0x74, 0x6F, 0xA7, 0x52, 0x99, 0x21
        };

        static public byte[] IIDX11 = new byte[]
        {
            0xED, 0xF0, 0x9C, 0x90, 0x44, 0x1A, 0x5A, 0x03,
            0xAB, 0x07, 0xC1, 0x99, 0x23, 0x24, 0x32, 0xC7,
            0x5F, 0x32, 0xA5, 0x97, 0xAD, 0x98, 0x0F, 0x8F
        };

        static public byte[] IIDX16 = new byte[]
        {
            0x28, 0x22, 0x28, 0x54, 0x63, 0x3F, 0x0E, 0x42,
            0x6F, 0x45, 0x4E, 0x50, 0x67, 0x53, 0x61, 0x7C,
            0x04, 0x46, 0x00, 0x3B, 0x13, 0x2B, 0x45, 0x6A
        };
    }

    public static class Bemani2DXEncryption
    {
        static public void Decrypt(Stream source, Stream target, byte[] key, Bemani2DXEncryptionType type)
        {
            BinaryReader reader = new BinaryReader(source);
            BinaryWriter writer = new BinaryWriter(target);

            byte[] lastBlock = {0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] currentBlock = { 0, 0, 0, 0, 0, 0, 0, 0 };

            while (source.Position < source.Length)
            {
                byte[] block = reader.ReadBytes(8);
                Array.Copy(block, currentBlock, 8);

                // xor with key 0
                for (int i = 0; i < 8; i++)
                    block[i] ^= key[i];

                // manipulation
                DecryptCycle(block);

                // swap first half with second half
                for (int i = 0; i < 4; i++)
                {
                    byte swap = block[i];
                    block[i] = block[i + 4];
                    block[i + 4] = swap;
                }

                if (type == Bemani2DXEncryptionType.Standard)
                {
                    // xor with key 1
                    for (int i = 0; i < 8; i++)
                        block[i] ^= key[8 + i];

                    // manipulation
                    DecryptCycle(block);

                    // xor with key 2
                    for (int i = 0; i < 8; i++)
                        block[i] ^= key[16 + i];
                }

                // xor with previous state
                for (int i = 0; i < 8; i++)
                    block[i] ^= lastBlock[i];

                // output
                writer.Write(block);
                Array.Copy(currentBlock, lastBlock, 8);
            }
        }

        static private void DecryptCycle(byte[] block)
        {
            unchecked
            {
                int a = (block[0] * 63) & 0xFF;
                int b = (a + block[3]) & 0xFF;
                int c = (block[1] * 17) & 0xFF;
                int d = (c + block[2]) & 0xFF;
                int e = (d + b) & 0xFF;
                int f = (e * block[3]) & 0xFF;
                int g = (f + b + 51) & 0xFF;
                int h = (b ^ d) & 0xFF;
                int i = (g ^ e) & 0xFF;

                block[4] ^= (byte)h;
                block[5] ^= (byte)d;
                block[6] ^= (byte)i;
                block[7] ^= (byte)g;
            }
        }

        static public void Encrypt(Stream source, Stream target, byte[] key, Bemani2DXEncryptionType type)
        {
        }
    }
}
