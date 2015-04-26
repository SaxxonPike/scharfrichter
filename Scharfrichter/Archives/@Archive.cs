using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Graphics;
using Scharfrichter.Codec.Sounds;
using Scharfrichter.Codec.Videos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Archives
{
    public abstract class Archive
    {
        public virtual int ArchiveCount
        {
            get
            {
                return 0;
            }
        }

        public virtual Archive[] Archives
        {
            get
            {
                return new Archive[] { };
            }
            set
            {
            }
        }

        public virtual int ChartCount
        {
            get
            {
                return 0;
            }
        }

        public virtual Chart[] Charts
        {
            get
            {
                return new Chart[] { };
            }
            set
            {
            }
        }

        public virtual int GraphicCount
        {
            get
            {
                return 0;
            }
        }

        public virtual Graphic[] Graphics
        {
            get
            {
                return new Graphic[] { };
            }
            set
            {
            }
        }

        public virtual byte[][] RawData
        {
            get
            {
                return new byte[][] { };
            }
            set
            {
            }
        }

        public virtual int RawDataCount
        {
            get
            {
                return 0;
            }
        }

        public virtual int SoundCount
        {
            get
            {
                return 0;
            }
        }

        public virtual Sound[] Sounds
        {
            get
            {
                return new Sound[] { };
            }
            set
            {
            }
        }

        public virtual int VideoCount
        {
            get
            {
                return 0;
            }
        }

        public virtual Video[] Videos
        {
            get
            {
                return new Video[] { };
            }
            set
            {
            }
        }
    }
}
