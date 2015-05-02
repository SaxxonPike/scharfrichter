using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Scharfrichter.Codec.Archives;
using Scharfrichter.Codec.Media;

namespace ConvertHelper
{
    public class StreamAdapterInfo
    {
        public long Length;
        public Stream Stream;

        public StreamAdapterInfo(Stream stream)
        {
            this.Length = stream.Length;
            this.Stream = stream;
        }

        public StreamAdapterInfo(Stream stream, long length)
        {
            this.Length = length;
            this.Stream = stream;
        }
    }

    static public class StreamAdapter
    {
        static public StreamAdapterInfo Open(string filename)
        {
            filename = filename.ToLowerInvariant().Trim();
            if (!File.Exists(filename))
            {
                return null;
            }

            if (filename.EndsWith(@".chd"))
            {
                return new StreamAdapterInfo(CHD.Load(new FileStream(filename, FileMode.Open, FileAccess.Read)));
            }

            if (filename.EndsWith(@".zip"))
            {
                var zip = new Zip(filename);
                if (zip.Files.Count > 0)
                {
                    var file = zip.Files[0];
                    long length = file.UncompressedSize;
                    if (length == 0 && file.CompressedSize != 0)
                    {
                        length = file.CompressedSize;
                    }
                    return new StreamAdapterInfo(zip.StreamFile(file), length);
                }
            }

            return new StreamAdapterInfo(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        static public StreamAdapterInfo Open(Stream source)
        {
            return new StreamAdapterInfo(source, source.Length);
        }
    }
}
