using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scharfrichter.Common
{
    public partial class Configuration
    {
        static public Configuration LoadIIDXConfig(string configFileName)
        {
            Configuration config = Configuration.ReadFile(configFileName);
            config["BMS"].SetDefaultValue("QuantizeMeasure", 0);
            config["BMS"].SetDefaultValue("QuantizeNotes", 192);
            config["BMS"].SetDefaultString("Difficulty1", "Beginner");
            config["BMS"].SetDefaultString("Difficulty2", "Normal");
            config["BMS"].SetDefaultString("Difficulty3", "Hyper");
            config["BMS"].SetDefaultString("Difficulty4", "Another");
            config["BMS"].SetDefaultString("Players1", "1P");
            config["BMS"].SetDefaultString("Players2", "2P");
            config["BMS"].SetDefaultString("Players3", "DP");
            config["IIDX"].SetDefaultString("Difficulty0", "3");
            config["IIDX"].SetDefaultString("Difficulty1", "2");
            config["IIDX"].SetDefaultString("Difficulty2", "4");
            config["IIDX"].SetDefaultString("Difficulty3", "1");
            config["IIDX"].SetDefaultString("Difficulty6", "3");
            config["IIDX"].SetDefaultString("Difficulty7", "2");
            config["IIDX"].SetDefaultString("Difficulty8", "4");
            config["IIDX"].SetDefaultString("Difficulty9", "1");
            config["IIDX"].SetDefaultString("Players0", "1");
            config["IIDX"].SetDefaultString("Players1", "1");
            config["IIDX"].SetDefaultString("Players2", "1");
            config["IIDX"].SetDefaultString("Players3", "1");
            config["IIDX"].SetDefaultString("Players6", "3");
            config["IIDX"].SetDefaultString("Players7", "3");
            config["IIDX"].SetDefaultString("Players8", "3");
            config["IIDX"].SetDefaultString("Players9", "3");
            return config;
        }

    }
}
