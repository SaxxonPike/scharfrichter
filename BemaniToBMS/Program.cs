using ConvertHelper;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BemaniToBMS
{
    class Program
    {
        static long unitNumerator = 100;
        static long unitDenominator = 5994;

        static void Main(string[] args)
        {
            ConvertHelper.BemaniToBMS.Convert(args, unitNumerator, unitDenominator);
        }
    }
}
