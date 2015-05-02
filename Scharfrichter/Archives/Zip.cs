using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class Zip : IDisposable
    {
        Stream source;

        public Zip(Stream source)
        {
            this.source = source;
            Initialize();
        }

        public Zip(string source)
        {
            this.source = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
            Initialize();
        }

        public Stream StreamFile(ZipDirectoryEntry zde)
        {
            var reader = new BinaryReader(source);
            
            switch (zde.Compression)
            {
                case ZipCompressionMethod.Stored:
                    source.Position = zde.Offset;
                    return source;
                    break;
                case ZipCompressionMethod.Deflated:
                    source.Position = zde.Offset;
                    return new DeflateStream(source, CompressionMode.Decompress);
                    break;
                default:
                    source.Position = zde.Offset;
                    return source;
                    break;
            }
        }

        public void Dispose()
        {
            if (source != null)
            {
                source.Dispose();
                source = null;
            }
        }

        public IList<ZipDirectoryEntry> Files
        {
            get;
            private set;
        }

        void Initialize()
        {
            Files = new List<ZipDirectoryEntry>();
            var reader = new BinaryReader(source);

            while (source.Position < source.Length)
            {
                int id = reader.ReadInt32();
                switch (id)
                {
                    case 0x4034B50:
                        var file = new ZipDirectoryEntry(source);
                        Files.Add(file);
                        if (file.CompressedSize >= 0 && file.UncompressedSize >= 0)
                        {
                            source.Position += file.CompressedSize;
                            if (file.DataDescriptorFollows)
                            {
                                reader.ReadBytes(12);
                            }
                        }
                        break;
                    default:
                        return;
                }
            }
        }
    }

    public enum ZipCompressionMethod : int
    {
        Stored = 0,
        Shrunk = 1,
        Reduced1 = 2,
        Reduced2 = 3,
        Reduced3 = 4,
        Reduced4 = 5,
        Imploded = 6,
        Tokenized = 7,
        Deflated = 8,
        EnhancedDeflate = 9,
        IBMTerseOld = 10,
        Reserved11 = 11,
        BZip2 = 12,
        Reserved13 = 13,
        LZMA = 14,
        Reserved15 = 15,
        Reserved16 = 16,
        Reserved17 = 17,
        IBMTerseNew = 18,
        IBMLZ77 = 19,
        WavPack = 97,
        PPMd = 98
    }

    public class ZipDirectoryEntry
    {
        protected static Encoding encoding437 = Encoding.GetEncoding(437);
        protected static Encoding encodingUTF8 = Encoding.UTF8;

        public int Version;
        public int Flags;
        public ZipCompressionMethod Compression;
        public int LastModifiedTime;
        public int LastModifiedDate;
        public int Crc32;
        public long CompressedSize;
        public long UncompressedSize;
        public string Filename;
        public List<ZipExtraField> ExtraData;
        public long Offset;

        public bool CompressedPatch { get { return (Flags & 32) != 0; } }
        public bool Compression1 { get { return (Flags & 2) != 0; } }
        public bool Compression2 { get { return (Flags & 4) != 0; } }
        public bool DataDescriptorFollows { get { return (Flags & 8) != 0; } }
        public bool Encrypted { get { return (Flags & 1) != 0; } }
        public bool EncryptedDirectory { get { return (Flags & 8192) != 0; } }
        public bool EnhancedCompression { get { return (Flags & 4096) != 0; } }
        public bool EnhancedDeflate { get { return (Flags & 16) != 0; } }
        public bool StrongEncryption { get { return (Flags & 64) != 0; } }
        public bool UseUTF8 { get { return (Flags & 2048) != 0; } }

        public ZipDirectoryEntry(Stream source)
        {
            ReadHeader(source);
        }

        static public ZipDirectoryEntry Read(Stream source)
        {
            return new ZipDirectoryEntry(source);
        }

        virtual protected void ReadHeader(Stream source)
        {
            var reader = new BinaryReader(source);
            int fileNameLength;
            int extraFieldLength;
            var enc = encoding437;

            Version = reader.ReadInt16();
            Flags = reader.ReadInt16();
            Compression = (ZipCompressionMethod)reader.ReadInt16();
            LastModifiedTime = reader.ReadInt16();
            LastModifiedDate = reader.ReadInt16();
            Crc32 = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            fileNameLength = reader.ReadInt16();
            extraFieldLength = reader.ReadInt16();
            ExtraData = new List<ZipExtraField>();

            if (UseUTF8)
            {
                enc = encodingUTF8;
            }

            if (fileNameLength > 0)
            {
                Filename = enc.GetString(reader.ReadBytes(fileNameLength));
            }
            else
            {
                Filename = "";
            }

            while (extraFieldLength > 0)
            {
                // load extra data
                int id = reader.ReadInt16();
                int fieldLength = reader.ReadInt16();
                ZipExtraField zef = new ZipExtraField(id, reader.ReadBytes(fieldLength));
                extraFieldLength -= fieldLength + 4;
                ExtraData.Add(zef);
            }

            foreach (var data in ExtraData)
            {
                using (MemoryStream dataMem = new MemoryStream(data.Data))
                {
                    BinaryReader dataReader = new BinaryReader(dataMem);
                    switch (data.Id)
                    {
                        case 1: // ZIP64
                            UncompressedSize = dataReader.ReadInt64();
                            CompressedSize = dataReader.ReadInt64();
                            break;
                    }
                }
            }

            Offset = source.Position;
        }
    }

    public class ZipExtraField
    {
        public byte[] Data;
        public int Id;

        public ZipExtraField(int id, byte[] data)
        {
            this.Id = id;
            this.Data = data;
        }
    }

}
