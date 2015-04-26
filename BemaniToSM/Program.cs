using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BemaniToSM
{
    class Program
    {
        static void Main(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                args = Directory.GetFiles(@"C:\Users\Tony\Desktop\ex\ssq");
                //args = new string[] { @"C:\Users\Tony\Desktop\ex\ssq\Card00024576.ssq" };
            }

            ConvertHelper.BemaniToSM.Convert(args);
        }
    }
}
