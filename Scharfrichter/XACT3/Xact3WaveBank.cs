using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.XACT3
{
    static public class Constants
    {
        public const int AdpcmMiniwaveformatBlockalignConversionOffset = 22;
        public const int WavebankHeaderSignature = 0x444E4257;
        public const int WavebankHeaderVersion = 44;
        public const int WavebankBanknameLength = 64;
        public const int WavebankEntrynameLength = 64;
        public const int WavebankMaxDataSegmentSize = 0x7FFFFFFF;
        public const int WavebankMaxCompactDataSegmentSize = 0x1FFFFF;
        public const int WavebankTypeBuffer = 0x00000000;
        public const int WavebankTypeStreaming = 0x00000001;
        public const int WavebankTypeMask = 0x00000001;
        public const int WavebankFlagsEntrynames = 0x00010000;
        public const int WavebankFlagsCompact = 0x00020000;
        public const int WavebankFlagsSyncDisabled = 0x00040000;
        public const int WavebankFlagsSeektables = 0x00080000;
        public const int WavebankFlagsMask = 0x000F0000;
        public const int WavebankEntryFlagsReadahead = 0x00000001;
        public const int WavebankEntryFlagsLoopcache = 0x00000002;
        public const int WavebankEntryFlagsRemovelooptail = 0x00000004;
        public const int WavebankEntryFlagsIgnoreloop = 0x00000008;
        public const int WavebankEntryFlagsMask = 0x0000000F;
        public const int WavebankminiformatTagPcm = 0x0;
        public const int WavebankminiformatTagXma = 0x1;
        public const int WavebankminiformatTagAdpcm = 0x2;
        public const int WavebankminiformatTagWma = 0x3;
        public const int WavebankminiformatBitdepth8 = 0x0;
        public const int WavebankminiformatBitdepth16 = 0x0;
        public const int WavebankentryXmastreamsMax = 3;
        public const int WavebankentryXmachannelsMax = 6;
        public const int WavebankDvdSectorSize = 2048;
        public const int WavebankDvdBlockSize = (WavebankDvdSectorSize * 16);
        public const int WavebankAlignmentMin = 4;
        public const int WavebankAlignmentDvd = WavebankDvdSectorSize;
        public const int MaxWmaAvgBytesPerSecEntries = 7;
        public const int MaxWmaBlockAlignEntries = 17;

        static public short[] AdpcmCoefficients = 
        {
            256, 0,
            512, -256,
            0, 0,
            192, 64,
            240, 0,
            460, -208,
            392, -232
        };

        static public readonly int[] WmaAvgBytesPerSec = new int[]
        {
            12000,
            24000,
            4000,
            6000,
            8000,
            20000,
            2500
        };

        static public readonly int[] WmaBlockAlign = new int[]
        {
            929,
            1487,
            1280,
            2230,
            8917,
            8192,
            4459,
            5945,
            2304,
            1536,
            1485,
            1000,
            2731,
            4096,
            6827,
            5462,
            1280
        };
    }

    public enum WaveBankSegIdx
    {
        BankData = 0,
        EntryMetaData,
        SeekTables,
        EntryNames,
        EntryWaveData,
        Count
    }

    public struct WaveBankData
    {
        public int Flags;
        public int EntryCount;
        public string BankName;
        public int EntryMetaDataElementSize;
        public int EntryNameElementSize;
        public int Alignment;
        WaveBankMiniWaveFormat CompactFormat;
        long BuildTime;

        static public WaveBankData Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankData result = new WaveBankData();
            result.Flags = reader.ReadInt32();
            result.EntryCount = reader.ReadInt32();
            result.BankName = Util.TrimNulls(new string(reader.ReadChars(Constants.WavebankBanknameLength)));
            result.EntryMetaDataElementSize = reader.ReadInt32();
            result.EntryNameElementSize = reader.ReadInt32();
            result.Alignment = reader.ReadInt32();
            result.CompactFormat = new WaveBankMiniWaveFormat();
            result.CompactFormat.Value = reader.ReadInt32();
            result.BuildTime = reader.ReadInt64();
            return result;
        }
    }

    public struct WaveBankEntry
    {
        public int Value;

        private int Flags
        {
            get { return Value & 0xF; }
        }

        private int Duration
        {
            get { return (Value >> 4); }
        }

        public WaveBankMiniWaveFormat Format;
        public WaveBankRegion PlayRegion;
        public WaveBankSampleRegion LoopRegion;

        static public WaveBankEntry Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankEntry result = new WaveBankEntry();
            result.Value = reader.ReadInt32();
            result.Format = WaveBankMiniWaveFormat.Read(source);
            result.PlayRegion = WaveBankRegion.Read(source);
            result.LoopRegion = WaveBankSampleRegion.Read(source);
            return result;
        }
    }

    public struct WaveBankHeader
    {
        public int Signature;
        public int Version;
        public int HeaderVersion;
        public WaveBankRegion[] Segments;

        static public WaveBankHeader Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankHeader result = new WaveBankHeader();
            result.Signature = reader.ReadInt32();
            result.Version = reader.ReadInt32();
            result.HeaderVersion = reader.ReadInt32();
            result.Segments = new WaveBankRegion[(int)WaveBankSegIdx.Count];
            for (int i = 0; i < result.Segments.Length; i++)
                result.Segments[i] = WaveBankRegion.Read(source);
            return result;
        }
    }

    public struct WaveBankMiniWaveFormat
    {
        public int Value;

        public int BitsPerSample
        {
                
            get
            {
                if (wFormatTag == Constants.WavebankminiformatTagXma)
                    return 2 * 8;
                if (wFormatTag == Constants.WavebankminiformatTagWma)
                    return 16;
                if (wFormatTag == Constants.WavebankminiformatTagAdpcm)
                    return 4;
                return ((wBitsPerSample == Constants.WavebankminiformatBitdepth16) ? 16 : 8);
            }
        }

        public int BlockAlign
        {
            get
            {
                switch (wFormatTag)
                {
                    case Constants.WavebankminiformatTagPcm:
                        return wBlockAlign;
                    case Constants.WavebankminiformatTagXma:
                        return (nChannels * (8 * 2) / 8);
                    case Constants.WavebankminiformatTagAdpcm:
                        return (wBlockAlign + Constants.AdpcmMiniwaveformatBlockalignConversionOffset) * nChannels;
                    case Constants.WavebankminiformatTagWma:
                        int dwBlockAlignIndex = wBlockAlign & 0x1F;
                        if (dwBlockAlignIndex < Constants.MaxWmaBlockAlignEntries)
                            return Constants.WmaBlockAlign[dwBlockAlignIndex];
                        break;
                }
                return 0;
            }
        }

        public int AvgBytesPerSec
        {
            get
            {
                switch (wFormatTag)
                {
                    case Constants.WavebankminiformatTagPcm:
                    case Constants.WavebankminiformatTagXma:
                        return (nSamplesPerSec * wBlockAlign);
                    case Constants.WavebankminiformatTagAdpcm:
                        return (BlockAlign * nSamplesPerSec / AdpcmSamplesPerBlock);
                    case Constants.WavebankminiformatTagWma:
                        int dwBytesPerSecIndex = wBlockAlign >> 5;
                        if (dwBytesPerSecIndex < Constants.MaxWmaAvgBytesPerSecEntries)
                            return Constants.WmaAvgBytesPerSec[dwBytesPerSecIndex];
                        break;
                }
                return 0;
            }
        }

        public int AdpcmSamplesPerBlock
        {
            get
            {
                int nBlockAlign = (wBlockAlign + Constants.AdpcmMiniwaveformatBlockalignConversionOffset) * nChannels;
                return nBlockAlign * 2 / nChannels - 12;
            }
        }

        public int SamplesPerSec
        {
            get
            {
                return nSamplesPerSec;
            }
        }

        public int Channels
        {
            get
            {
                return nChannels;
            }
        }

        public int FormatTag
        {
            get
            {
                return wFormatTag;
            }
        }

        private int wFormatTag
        {
            get { return (Value >> 0) & 0x3; }
            set { Value &= ~(0x3 << 0); Value |= (value & 0x3) << 0; }
        }

        private int nChannels
        {
            get { return (Value >> 2) & 0x7; }
            set { Value &= ~(0x7 << 2); Value |= (value & 0x7) << 2; }
        }

        private int nSamplesPerSec
        {
            get { return (Value >> 5) & 0x3FFFF; }
            set { Value &= ~(0x3FFFF << 5); Value |= (value & 0x3FFFF) << 5; }
        }

        private int wBlockAlign
        {
            get { return (Value >> 23) & 0xFF; }
            set { Value &= ~(0xFF << 23); Value |= (value & 0xFF) << 23; }
        }

        private int wBitsPerSample
        {
            get { return (Value >> 31) & 0x1; }
            set { Value &= ~(0x1 << 31); Value |= (value & 0x1) << 31; }
        }

        static public WaveBankMiniWaveFormat Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankMiniWaveFormat result = new WaveBankMiniWaveFormat();
            result.Value = reader.ReadInt32();
            return result;
        }
    }

    public struct WaveBankRegion
    {
        public int Offset;
        public int Length;

        static public WaveBankRegion Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankRegion result = new WaveBankRegion();
            result.Offset = reader.ReadInt32();
            result.Length = reader.ReadInt32();
            return result;
        }
    }

    public struct WaveBankSampleRegion
    {
        public int StartSample;
        public int TotalSamples;

        static public WaveBankSampleRegion Read(Stream source)
        {
            BinaryReader reader = new BinaryReader(source);
            WaveBankSampleRegion result = new WaveBankSampleRegion();
            result.StartSample = reader.ReadInt32();
            result.TotalSamples = reader.ReadInt32();
            return result;
        }
    }
}
