using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public class Gzip : IDisposable
    {
        bool closeOnDispose;
        DeflateStream deflateSource;
        Stream source;

        public Gzip(Stream source)
        {
            this.source = source;
            Initialize();
        }

        public Gzip(string filename)
        {
            closeOnDispose = true;
            this.source = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            Initialize();
        }

        string GetNullTerminatedString(BinaryReader reader)
        {
            var result = new StringBuilder();
            var enc = Encoding.GetEncoding(437);
            byte[] namedataarray = new byte[1];
            byte namedata;

            while (true)
            {
                namedata = reader.ReadByte();
                if (namedata == 0)
                {
                    break;
                }
                namedataarray[0] = namedata;
                result.Append(enc.GetChars(namedataarray));
            }

            return result.ToString();
        }

        public Stream GetDeflateStream()
        {
            return deflateSource;
        }

        void Initialize()
        {
            BinaryReader reader = new BinaryReader(source);

            int id = reader.ReadUInt16();
            if (id != 0x8b1f)
            {
                return; // not valid ID
            }

            int cm = reader.ReadByte();
            if (cm != 0x08)
            {
                return; // can't decode anything but deflate
            }

            int flags = reader.ReadByte();
            int mtime = reader.ReadInt32();
            int xfl = reader.ReadByte();
            int os = reader.ReadByte();

            bool ftext = (flags & 1) != 0;
            bool fhcrc = (flags & 2) != 0;
            bool fextra = (flags & 4) != 0;
            bool fname = (flags & 8) != 0;
            bool fcomment = (flags & 16) != 0;

            if (fextra)
            {
                int xlen = reader.ReadUInt16();
                reader.ReadBytes(xlen); // extra data
            }

            if (fname)
            {
                GetNullTerminatedString(reader); // filename
            }

            if (fcomment)
            {
                GetNullTerminatedString(reader); // file comment
            }

            if (fhcrc)
            {
                reader.ReadInt16(); // crc16 (lower 16 bits of crc32)
            }

            deflateSource = new DeflateStream(source, CompressionMode.Decompress);
        }

        public void Dispose()
        {
            if (closeOnDispose && source != null)
            {
                closeOnDispose = false;
                source.Close();
            }
            source = null;
            deflateSource = null;
        }
    }
}
