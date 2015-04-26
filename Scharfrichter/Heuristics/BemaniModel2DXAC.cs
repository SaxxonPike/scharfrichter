using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Heuristics
{
    static public partial class Heuristics
    {
        static public bool DetectBemaniModel2DXAC(byte[] data)
        {
            if (data.Length < 4)
                return false;
            if (data[0] == 0xC1 && data[1] == 0xD0 && data[2] == 0xB2 && data[3] == 0x08)
                return true;
            return false;
        }
    }
}
