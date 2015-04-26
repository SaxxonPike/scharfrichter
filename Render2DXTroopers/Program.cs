using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Render2DXTroopers
{
    class Program
    {
        static void Main(string[] args)
        {
            // debug args (if applicable)
            if (System.Diagnostics.Debugger.IsAttached && args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Debugger attached. Input file name:");
                string baseName = Console.ReadLine();
                args = new string[] { baseName + ".1", baseName + ".2dx" };
            }

            ConvertHelper.Render.RenderWAV(args, 1, 1000);
        }
    }
}
