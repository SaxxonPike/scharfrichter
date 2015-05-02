using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace System.IO.Compression
{
    /// <summary>
    /// 
    /// </summary>
    internal class ZipDeflateStream : DeflateStream
    {
        int bufferSize;
        long position;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public ZipDeflateStream(Stream source, ZipStorer.ZipFileEntry zfe)
            : base(source, CompressionMode.Decompress, true)
        {
            this.Entry = zfe;
        }

        public ZipStorer.ZipFileEntry Entry
        {
            get;
            private set;
        }

        public override long Length
        {
            get
            {
                return Entry.FileSize;
            }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                if (value < position)
                {
                    throw new IOException(@"Can't seek backwards in zip deflate stream");
                }
                else if (value > position)
                {
                    byte[] buffer = new byte[16384];
                    long bytesRemaining = value - position;
                    while (bytesRemaining > 0)
                    {
                        Read(buffer, 0, (int)Math.Max(bytesRemaining, 16384));
                    }
                }
            }
        }

        public override int Read(byte[] array, int offset, int count)
        {
            position += count;
            return base.Read(array, offset, count);
        }
    }
}
