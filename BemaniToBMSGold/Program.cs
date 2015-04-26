using ConvertHelper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BemaniToBMSGold
{
    class Program
    {
        static long unitNumerator = 100;
        static long unitDenominator = 6004;

        static void Main(string[] args)
        {
            BemaniToBMS.Convert(args, unitNumerator, unitDenominator);
        }
    }
}
