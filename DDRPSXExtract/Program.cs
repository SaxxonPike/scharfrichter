using Scharfrichter.Codec;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DDRPSXExtract
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("This tool is currently for diagnostics only.");
                Console.WriteLine("It has next to no functionality. It can only be run in the IDE.");
                return;
            }


            string basePath;
            string exeFile;
            string dataFile;
            string mix = @"Extra";
            int offsetAdjust;
            int tableOffset;


            switch (mix)
            {
                case @"5th":
                    basePath = @"C:\Users\Tony_2\Desktop\New folder (7)\5th\";
                    exeFile = @"SLPM_868.97";
                    dataFile = @"READ_DT.BIN";
                    offsetAdjust = 0x4E20;
                    tableOffset = 0x929F8;
                    break;
                case @"Extra":
                    basePath = @"C:\Users\Tony_2\Desktop\New folder (7)\Extra\";
                    exeFile = @"SLPM_868.31";
                    dataFile = @"READ_DT.BIN";
                    offsetAdjust = 0xA410;
                    tableOffset = 0x83A7C;
                    break;
                default:
                    return;
            }

            byte[] executableData = File.ReadAllBytes(Path.Combine(basePath, exeFile));
            byte[] fileData = File.ReadAllBytes(Path.Combine(basePath, dataFile));



            using (MemoryStream exe = new MemoryStream(executableData), data = new MemoryStream(fileData))
            {
                BinaryReader exeReader = new BinaryReader(exe);
                BinaryReader dataReader = new BinaryReader(data);

                exe.Position = tableOffset;

                while (true)
                {
                    int length = exeReader.ReadInt32();
                    int offset = exeReader.ReadInt32();
                    if (length == 0)
                        break;

                    offset -= offsetAdjust;
                    offset *= 0x800;
                    data.Position = offset;

                    byte[] extracted = dataReader.ReadBytes(length);
                    File.WriteAllBytes(Path.Combine(basePath, Util.ConvertToDecimalString(offset / 0x800, 6) + ".dat"), extracted);
                }
            }
        }
    }
}
