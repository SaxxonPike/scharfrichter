using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Media
{
    public partial class CHD : Stream
    {
        private struct CachedHunk
        {
            public int index;
            public byte[] data;
        }

        private const int hunkCacheMaxSize = 256;

        private CachedHunk currentHunk = new CachedHunk();
        private List<CachedHunk> hunkCache = new List<CachedHunk>();

        private Stream baseStream;
        private long hunkOffset;
        private long hunkSize;
        private UInt64 dataLength = 0;
        private long position;

        private CachedHunk CacheHunk(int index)
        {
            CachedHunk hunk = new CachedHunk();
            hunk.data = ReadHunk(index);
            hunk.index = index;
            hunkCache.Add(hunk);
            if (hunkCache.Count >= hunkCacheMaxSize)
                hunkCache.RemoveAt(0);
            return hunk;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            // do nothing
        }

        private void GetHunk(int index)
        {
            hunkOffset = (long)index * hunkSize;
            int count = hunkCache.Count;
            for (int i = 0; i < count; i++)
            {
                CachedHunk hunk = hunkCache[i];
                if (hunk.index == index)
                {
                    currentHunk = hunk;
                    return;
                }
            }
            currentHunk = CacheHunk(index);
        }

        public override long Length
        {
            get { return (long)dataLength; }
        }

        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int total = count;

            if (currentHunk.data == null)
                GetHunk((int)(position / hunkSize));

            long hunkPosition = position - hunkOffset;
            while (count > 0)
            {
                if (hunkPosition >= hunkSize || hunkPosition < 0)
                {
                    GetHunk((int)(position / hunkSize));
                    hunkPosition = position - hunkOffset;
                }
                buffer[offset] = currentHunk.data[hunkPosition];
                hunkPosition++;
                offset++;
                position++;
                count--;
            }

            return total;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Current:
                    Position = (position + offset);
                    break;
                case SeekOrigin.End:
                    Position = (position - offset);
                    break;
                default:
                    Position = offset;
                    break;
            }
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
