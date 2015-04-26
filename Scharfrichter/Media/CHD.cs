using Scharfrichter.Codec.Compression;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Scharfrichter.Codec.Media
{
    public partial class CHD
    {
        private struct HeaderInfo
        {
            public UInt32 compression;
            public UInt32[] compressors;
            public UInt32 cylinders;
            public UInt32 flags;
            public UInt32 heads;
            public UInt32 hunkBytes;
            public UInt32 hunkSize;
            public UInt64 logicalBytes;
            public UInt64 mapOffset;
            public byte[] md5;
            public UInt64 metaOffset;
            public byte[] parentmd5;
            public byte[] parentsha1;
            public byte[] rawsha1;
            public UInt32 seclen;
            public UInt32 sectors;
            public byte[] sha1;
            public UInt32 totalHunks;
            public UInt32 unitBytes;
        }

        private struct MapInfo
        {
            public UInt32 compression;
            public UInt32 crc32;
            public byte flags;
            public UInt64 length;
            public UInt64 offset;
        }

        private const int CompressionRLESmall = 7;
        private const int CompressionRLELarge = 8;

        private HeaderInfo header;
        private List<MapInfo> map;
        private CHD parent;
        private BinaryReaderEx reader;

        private Func<int, byte[]> ReadHunk;

        public CHD(Stream baseFile)
        {
            baseStream = baseFile;
            reader = new BinaryReaderEx(baseStream);
        }

        public static CHD Load(Stream source)
        {
            CHD result = new CHD(source);
            BinaryReaderEx reader = result.reader;

            if (new string(reader.ReadChars(8)) != "MComprHD")
                return null;

            UInt32 headerLength = reader.ReadUInt32S();
            UInt32 version = reader.ReadUInt32S();

            switch (version)
            {
                case 1:
                    result.ReadHeaderV1();
                    result.ReadMapV1();
                    result.ReadHunk = result.ReadHunkV1;
                    break;
                case 2:
                    result.ReadHeaderV2();
                    result.ReadMapV1();
                    result.ReadHunk = result.ReadHunkV1;
                    break;
                case 3:
                    result.ReadHeaderV3();
                    result.ReadMapV3();
                    result.ReadHunk = result.ReadHunkV3;
                    break;
                case 4:
                    result.ReadHeaderV4();
                    result.ReadMapV3();
                    result.ReadHunk = result.ReadHunkV3;
                    break;
                case 5:
                    result.ReadHeaderV5();
                    result.ReadMapV5();
                    result.ReadHunk = result.ReadHunkV5;
                    break;
                default:
                    return null;
            }

            return result;
        }

        private void ReadHeaderV1()
        {
            header = new HeaderInfo();
            header.flags = reader.ReadUInt32S();
            header.compression = reader.ReadUInt32S();
            header.hunkSize = reader.ReadUInt32S();
            header.totalHunks = reader.ReadUInt32S();
            header.cylinders = reader.ReadUInt32S();
            header.heads = reader.ReadUInt32S();
            header.sectors = reader.ReadUInt32S();
            header.md5 = reader.ReadMD5S();
            header.parentmd5 = reader.ReadMD5S();
            header.seclen = 512;
            dataLength = header.totalHunks * (header.hunkSize * 512);
            hunkSize = header.hunkSize * 512;
        }

        private void ReadHeaderV2()
        {
            header = new HeaderInfo();
            header.flags = reader.ReadUInt32S();
            header.compression = reader.ReadUInt32S();
            header.hunkSize = reader.ReadUInt32S();
            header.totalHunks = reader.ReadUInt32S();
            header.cylinders = reader.ReadUInt32S();
            header.heads = reader.ReadUInt32S();
            header.sectors = reader.ReadUInt32S();
            header.md5 = reader.ReadMD5S();
            header.parentmd5 = reader.ReadMD5S();
            header.seclen = reader.ReadUInt32S();
            dataLength = header.totalHunks * (header.hunkSize * header.seclen);
            hunkSize = header.hunkSize * header.seclen;
        }

        private void ReadHeaderV3()
        {
            header = new HeaderInfo();
            header.flags = reader.ReadUInt32S();
            header.compression = reader.ReadUInt32S();
            header.totalHunks = reader.ReadUInt32S();
            header.logicalBytes = reader.ReadUInt64S();
            header.metaOffset = reader.ReadUInt64S();
            header.md5 = reader.ReadMD5S();
            header.parentmd5 = reader.ReadMD5S();
            header.hunkBytes = reader.ReadUInt32S();
            header.sha1 = reader.ReadSHA1S();
            header.parentsha1 = reader.ReadSHA1S();
            dataLength = header.logicalBytes;
            hunkSize = header.hunkBytes;
        }

        private void ReadHeaderV4()
        {
            header = new HeaderInfo();
            header.flags = reader.ReadUInt32S();
            header.compression = reader.ReadUInt32S();
            header.totalHunks = reader.ReadUInt32S();
            header.logicalBytes = reader.ReadUInt64S();
            header.metaOffset = reader.ReadUInt64S();
            header.hunkBytes = reader.ReadUInt32S();
            header.sha1 = reader.ReadSHA1S();
            header.parentsha1 = reader.ReadSHA1S();
            header.rawsha1 = reader.ReadSHA1S();
            dataLength = header.logicalBytes;
            hunkSize = header.hunkBytes;
        }

        private void ReadHeaderV5()
        {
            header = new HeaderInfo();
            header.compressors = new UInt32[] { reader.ReadUInt32S(), reader.ReadUInt32S(), reader.ReadUInt32S(), reader.ReadUInt32S() };
            header.logicalBytes = reader.ReadUInt64S();
            header.mapOffset = reader.ReadUInt64S();
            header.metaOffset = reader.ReadUInt64S();
            header.hunkBytes = reader.ReadUInt32S();
            header.unitBytes = reader.ReadUInt32S();
            header.rawsha1 = reader.ReadSHA1S();
            header.sha1 = reader.ReadSHA1S();
            header.parentsha1 = reader.ReadSHA1S();
            dataLength = header.logicalBytes;
            hunkSize = header.hunkBytes;
            header.totalHunks = (uint)((header.logicalBytes + header.hunkBytes - 1) / header.hunkBytes);
        }

        private byte[] ReadHunkV1(int index)
        {
            MapInfo entry = map[index];
            byte[] result;

            baseStream.Position = (long)entry.offset;
            if (entry.length == header.hunkSize)
            {
                result = new byte[header.hunkSize];
                baseStream.Read(result, 0, (int)(header.hunkSize * header.seclen));
            }
            else
            {
                result = DecompressZlib((UInt32)entry.length, header.hunkSize * header.seclen);
            }
            
            return result;
        }

        private byte[] ReadHunkV3(int index)
        {
            MapInfo entry = map[index];
            byte[] result;

            switch (entry.flags & 0xF)
            {
                case 0x1:
                    baseStream.Position = (long)entry.offset;
                    result = DecompressZlib((UInt32)entry.length, header.hunkBytes);
                    break;
                case 0x2:
                    baseStream.Position = (long)entry.offset;
                    result = new byte[header.hunkBytes];
                    baseStream.Read(result, 0, (int)header.hunkBytes);
                    break;
                case 0x3:
                    result = DecompressMini(entry.offset, header.hunkBytes);
                    break;
                case 0x4:
                    result = DecompressSelfHunk(entry.offset);
                    break;
                case 0x5:
                    result = DecompressParentHunk(entry.offset);
                    break;
                case 0x6:
                    throw new Exception("Unsupported V3 hunk type.");
                default:
                    throw new Exception("Invalid V3 hunk type.");
            }

            return result;
        }

        private byte[] ReadHunkV5(int index)
        {
            byte[] result = null;
            return result;
        }

        private void ReadMapV1()
        {
            map = new List<MapInfo>();
            for (UInt32 i = 0; i < header.totalHunks; i++)
            {
                MapInfo entry = new MapInfo();
                UInt64 raw = reader.ReadUInt64S();
                entry.offset = (raw >> 20) & 0xFFFFFFFFFFFul;
                entry.length = (raw & 0xFFFFFul);
                map.Add(entry);
            }
        }

        private void ReadMapV3()
        {
            map = new List<MapInfo>();
            for (UInt32 i = 0; i < header.totalHunks; i++)
            {
                MapInfo entry = new MapInfo();
                entry.offset = reader.ReadUInt64S();
                entry.crc32 = reader.ReadUInt32S();
                entry.length = reader.ReadUInt16S();
                entry.length |= ((UInt64)reader.ReadByte()) << 16;
                entry.flags = reader.ReadByte();
                map.Add(entry);
            }
        }

        private void ReadMapV5()
        {
            map = new List<MapInfo>();
            reader.BaseStream.Position = (long)header.mapOffset;

            bool compressed = (header.compressors[0] != 0);

            if (compressed)
            {
                // compressed map header
                UInt32 mapBytes = reader.ReadUInt32S();
                UInt64 firstOffs = reader.ReadUValueS(6);
                UInt16 mapCrc = reader.ReadUInt16S();
                byte lengthBits = reader.ReadByte();
                byte selfBits = reader.ReadByte();
                byte parentBits = reader.ReadByte();

                // decompress the map
                Huffman huffmanDecoder = new Huffman(16, 8, null, null, null);
                huffmanDecoder.ImportTreeRLE(reader);
                byte lastComp = 0;
                int repCount = 0;

                using (MemoryStream mem = new MemoryStream())
                {
                    for (int hunkNum = 0; hunkNum < header.totalHunks; hunkNum++)
                    {

                    }
                }
            }
        }
    }
}
