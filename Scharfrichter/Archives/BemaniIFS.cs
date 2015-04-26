using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class BemaniIFS : Archive
    {
        static public readonly string PropBinaryNameChars =
            "0123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

        public struct Header
        {
            public const int Size = 0x24;

            public int Unk00;
            public int Unk04;
            public int Unk08;
            public int Unk0C;
            public int BodyStart;
            public int Unk14;
            public int Unk18;
            public int Unk1C;
            public int Unk20;

            static public Header Read(Stream source)
            {
                BinaryReaderEx reader = new BinaryReaderEx(source);
                Header result = new Header();
                result.Unk00 = reader.ReadInt32S();
                result.Unk04 = reader.ReadInt32S();
                result.Unk08 = reader.ReadInt32S();
                result.Unk0C = reader.ReadInt32S();
                result.BodyStart = reader.ReadInt32S();
                result.Unk14 = reader.ReadInt32S();
                result.Unk18 = reader.ReadInt32S();
                result.Unk1C = reader.ReadInt32S();
                result.Unk20 = reader.ReadInt32S();
                return result;
            }
        }

        public struct Stat
        {
            public int Offset;
            public int Length;
            public int TimeStamp;

            static public Stat Read(Stream source)
            {
                BinaryReaderEx reader = new BinaryReaderEx(source);
                Stat result = new Stat();
                result.Offset = reader.ReadInt32S();
                result.Length = reader.ReadInt32S();
                result.TimeStamp = reader.ReadInt32S();
                return result;
            }
        }

        private List<byte[]> files = new List<byte[]>();
        private List<string> properties = new List<string>();

        public string[] Properties
        {
            get
            {
                return properties.ToArray();
            }
            set
            {
                properties.Clear();
                properties.AddRange(value);
            }
        }

        public override byte[][] RawData
        {
            get
            {
                return files.ToArray();
            }
            set
            {
                files.Clear();
                files.AddRange(value);
            }
        }

        public override int RawDataCount
        {
            get
            {
                return files.Count;
            }
        }

        static public BemaniIFS Read(Stream source)
        {
            BemaniIFS result = new BemaniIFS();

            // read header
            Header header = Header.Read(source);
            byte[] propertyPageData = new byte[header.BodyStart - Header.Size];
            source.Read(propertyPageData, 0, propertyPageData.Length);

            // read property page






#if (false)
            List<byte[]> dataList = new List<byte[]>();
            BinaryReaderEx reader = new BinaryReaderEx(source);
            BemaniIFS result = new BemaniIFS();

            // header length is 0x28 bytes
            reader.ReadInt32S(); // identifier
            Int16 headerMetaLength = reader.ReadInt16S(); // header meta amount?
            reader.ReadInt16S(); // bitwise xor 0xFFFF of previously read value
            reader.ReadInt32S();
            reader.ReadInt32S();
            Int32 headerLength = reader.ReadInt32S();
            reader.ReadInt32S();

            for (int i = 1; i < headerMetaLength; i++)
            {
                reader.ReadInt32S();
                reader.ReadInt32S();
            }

            Console.WriteLine("Header length: " + headerLength.ToString());

            // read table A
            Int32 tableALength = reader.ReadInt32S();
            Console.WriteLine("Table A length: " + tableALength.ToString());
            MemoryStream tableAMem = new MemoryStream(reader.ReadBytes(tableALength));

            // read table B
            Int32 tableBLength = reader.ReadInt32S();
            Console.WriteLine("Table B length: " + tableBLength.ToString());
            MemoryStream tableBMem = new MemoryStream(reader.ReadBytes(tableBLength));

            // read padding
            int headerPadding = headerLength - (0x10 + (headerMetaLength * 8) + 4 + tableALength + 4 + tableBLength);
            if (headerPadding > 0)
                reader.ReadBytes(headerPadding);

            // a bit of a hack to get the info we need (it's probably not accurate)
            BinaryReaderEx tableAReader = new BinaryReaderEx(tableAMem);
            BinaryReaderEx tableBReader = new BinaryReaderEx(tableBMem);

            tableAReader.BaseStream.Position = 0x18;
            tableBReader.BaseStream.Position = 0x14;
            int dataLength = tableBReader.ReadInt32S();
            MemoryStream dataChunk = new MemoryStream(reader.ReadBytes(dataLength));
            BinaryReaderEx dataReader = new BinaryReaderEx(dataChunk);

            // process tables
            int chunkIndex = 0;
            bool processTable = true;
            while (processTable)
            {
                Console.Write("A:" + Util.ConvertToHexString((int)tableAReader.BaseStream.Position, 8) + " B:" + Util.ConvertToHexString((int)tableBReader.BaseStream.Position, 8) + " ");
                byte chunkType = tableAReader.ReadByte();
                Console.Write("Op:" + Util.ConvertToHexString(chunkType, 2) + "  ");
                switch (chunkType)
                {
                    case 0x06: // directory
                        {
                            byte subType = tableAReader.ReadByte();
                            switch (subType)
                            {
                                case 0x03:
                                    tableAReader.ReadBytes(3);
                                    break;
                                case 0x06:
                                    break;
                                default:
                                    break;
                            }
                            Int32 fileModified = tableBReader.ReadInt32S(); // modified date?
                            Console.WriteLine("*" + Util.ConvertToHexString(fileModified, 8));
                        }
                        continue;
                    case 0x1E: // file
                        tableAReader.ReadByte();
                        {
                            Int32 fileOffset = tableBReader.ReadInt32S(); // offset
                            Int32 fileLength = tableBReader.ReadInt32S(); // length
                            Int32 fileModified = tableBReader.ReadInt32S(); // modified date?
                            Console.WriteLine(Util.ConvertToHexString(fileOffset, 8) + ":" + Util.ConvertToHexString(fileLength, 8) + ", *" + Util.ConvertToHexString(fileModified, 8));
                            dataReader.BaseStream.Position = fileOffset;
                            dataList.Add(dataReader.ReadBytes(fileLength));
                        }
                        break;
                    case 0x94: // filename
                        Console.WriteLine("FileID: " + Util.ConvertToHexString(tableAReader.ReadInt32S(), 8));
                        continue;
                    case 0xFE: // end of entry
                        Console.WriteLine("End of entry.");
                        break;
                    case 0xFF: // end of list
                        processTable = false;
                        Console.WriteLine("End of list.");
                        continue;
                    default:
                        // for types we don't know, skip the whole line for now
                        Console.WriteLine("UNKNOWN.");
                        break;
                }
                while (chunkType != 0xFE)
                {
                    chunkType = tableAReader.ReadByte();
                }
                chunkIndex++;
            }

            result.files = dataList;
#endif

            return result;
        }
    }
}
