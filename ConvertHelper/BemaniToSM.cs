using Scharfrichter.Codec;
using Scharfrichter.Codec.Archives;
using Scharfrichter.Codec.Charts;
using Scharfrichter.Codec.Sounds;
using Scharfrichter.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConvertHelper
{
    static public class BemaniToSM
    {
        private const string configFileName = "Convert";
        private const string databaseFileName = "DDRDB";

        static public void Convert(string[] inArgs)
        {
            // configuration
            Configuration config = LoadConfig();

            // splash
            Splash.Show("Bemani To Stepmania");

            // parse args
            string[] args;
            if (inArgs.Length > 0)
                args = Subfolder.Parse(inArgs);
            else
                args = inArgs;

            // usage if no args present
            if (args.Length == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: BemaniToSM <input file>");
                Console.WriteLine();
                Console.WriteLine("Drag and drop with files and folders is fully supported for this application.");
                Console.WriteLine();
                Console.WriteLine("Supported formats:");
                Console.WriteLine("SSQ, XWB");
            }

            // process
            foreach (string filename in args)
            {
                if (File.Exists(filename))
                {
                    Console.WriteLine();
                    Console.WriteLine("Processing File: " + filename);
                    switch (Path.GetExtension(filename).ToUpper())
                    {
                        case @".XWB":
                            {
                                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    Console.WriteLine("Reading XWB bank");
                                    MicrosoftXWB bank = MicrosoftXWB.Read(fs);
                                    string outPath = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));

                                    Directory.CreateDirectory(outPath);

                                    int count = bank.SoundCount;

                                    for (int i = 0; i < count; i++)
                                    {
                                        string outFileName;

                                        if ((bank.Sounds[i].Name == null) || (bank.Sounds[i].Name == ""))
                                            outFileName = Util.ConvertToHexString(i, 4);
                                        else
                                            outFileName = bank.Sounds[i].Name;

                                        string outFile = Path.Combine(outPath, outFileName + ".wav");
                                        Console.WriteLine("Writing " + outFile);
                                        bank.Sounds[i].WriteFile(outFile, 1.0f);
                                    }

                                    bank = null;
                                }
                            }
                            break;
                        case @".SSQ":
                            {
                                string iTitle = "";
                                string iArtist = "";
                                string iTitleTranslit = "";
                                string iArtistTranslit = "";
                                string iCDTitle = "";
                                string iBGChanges = "";

                                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    BemaniSSQ ssq = BemaniSSQ.Read(fs, 0x1000);
                                    StepmaniaSM sm = new StepmaniaSM();

                                    Console.WriteLine();
                                    Console.Write("TITLE: ");
                                    iTitle = Console.ReadLine();

                                    Console.Write("ARTIST: ");
                                    iArtist = Console.ReadLine();

                                    Console.Write("TITLETRANSLIT: ");
                                    iTitleTranslit = Console.ReadLine();

                                    Console.Write("ARTISTTRANSLIT: ");
                                    iArtistTranslit = Console.ReadLine();

                                    Console.Write("Origin (for CDTitle): ");
                                    iCDTitle = Console.ReadLine();

                                    Console.Write("Include BGCHANGES tag? y for Yes, ENTER for None. --> ");
                                    iBGChanges = Console.ReadLine();

                                    sm.Tags["SongID"] = Path.GetFileNameWithoutExtension(@filename);
                                    sm.Tags["TITLE"] = iTitle;
                                    sm.Tags["ARTIST"] = iArtist;
                                    sm.Tags["TITLETRANSLIT"] = iTitleTranslit;
                                    sm.Tags["ARTISTTRANSLIT"] = iArtistTranslit;

                                    if (iTitleTranslit == "")
                                        sm.Tags["BANNER"] = iTitle + ".png";
                                    else
                                        sm.Tags["BANNER"] = iTitleTranslit + ".png";

                                    if (iTitleTranslit == "")
                                        sm.Tags["BACKGROUND"] = iTitle + "-bg.png";
                                    else
                                        sm.Tags["BACKGROUND"] = iTitleTranslit + "-bg.png";

                                    if (iCDTitle != "")
                                        sm.Tags["CDTITLE"] = "./CDTitles/" + iCDTitle + ".png";

                                    if (iTitleTranslit == "")
                                        sm.Tags["MUSIC"] = iTitle + ".ogg";
                                    else
                                        sm.Tags["MUSIC"] = iTitleTranslit + ".ogg";

                                    sm.Tags["SAMPLESTART"] = "20";
                                    sm.Tags["SAMPLELENGTH"] = "15";

                                    sm.CreateTempoTags(ssq.TempoEntries.ToArray());

                                    if (iBGChanges == "y")
                                    {
                                         if (iTitleTranslit == "")
                                             sm.Tags["BGCHANGES"] = "0=" + iTitle + ".avi=1=0=0=0";
                                         else
                                             sm.Tags["BGCHANGES"] = "0=" + iTitleTranslit + ".avi=1=0=0=0";
                                    }

                                    Console.WriteLine();
                                    Console.WriteLine("Input difficulty ratings for song " + iTitle + " below.");
                                    Console.WriteLine();

                                    string[] gType = { "dance-single", "dance-double", "dance-couple", "dance-solo" };

                                    foreach (string gName in gType)
                                    {
                                        string[] dType = { "Beginner", "Easy", "Medium", "Hard", "Challenge", "" };

                                        foreach (string dName in dType)
                                        {
                                            foreach (Chart chart in ssq.Charts)
                                            {
                                                string gameType = config["SM"]["DanceMode" + chart.Tags["Panels"]];
                                                
                                                if (gName == gameType)
                                                {
                                                    string difficulty = config["SM"]["Difficulty" + config["DDR"]["Difficulty" + chart.Tags["Difficulty"]]];
                                                    chart.Entries.Sort();

                                                    if ((gameType == config["SM"]["DanceMode8"]) && (difficulty == ""))
                                                        break;

                                                    if (dName == difficulty)
                                                    {
                                                        // solo chart check
                                                        if (gameType == config["SM"]["DanceMode6"])
                                                        {
                                                            foreach (Entry entry in chart.Entries)
                                                            {
                                                                if (entry.Type == EntryType.Marker)
                                                                {
                                                                    switch (entry.Column)
                                                                    {
                                                                        case 0: entry.Column = 0; break;
                                                                        case 1: entry.Column = 2; break;
                                                                        case 2: entry.Column = 3; break;
                                                                        case 3: entry.Column = 5; break;
                                                                        case 4: entry.Column = 1; break;
                                                                        case 6: entry.Column = 4; break;
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        // couples chart check
                                                        else if (gameType == config["SM"]["DanceMode4"])
                                                        {
                                                            foreach (Entry entry in chart.Entries)
                                                            {
                                                                if (entry.Type == EntryType.Marker && entry.Column >= 4)
                                                                {
                                                                    gameType = config["SM"]["DanceModeCouple"];
                                                                    chart.Tags["Panels"] = "8";
                                                                    break;
                                                                }
                                                            }
                                                        }

                                                        string difText = difficulty;

                                                        switch (difficulty)
                                                        {
                                                            case "Easy": difText = "Basic"; break;
                                                            case "Medium": difText = "Difficult"; break;
                                                            case "Hard": difText = "Expert"; break;
                                                            case "": difText = "Difficult"; break;
                                                        }
                                                        
                                                        Console.Write(ToUpperFirstLetter(gameType.Replace("dance-", "")) + "-" + difText + ": ");

                                                        string meter = Console.ReadLine();

                                                        if (meter == "")
                                                            meter = "0";

                                                        string dif = difficulty;

                                                        if (difficulty == "")
                                                            dif = "Medium";

                                                        sm.CreateStepTag(chart.Entries.ToArray(), gameType, "", dif, meter, "", System.Convert.ToInt32(chart.Tags["Panels"]), config["SM"].GetValue("QuantizeNotes"));
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    string outTitle = iTitle;
                                    if (iTitleTranslit != "")
                                        outTitle = iTitleTranslit;

                                    sm.WriteFile(Path.Combine(Path.GetDirectoryName(filename), outTitle + ".sm"));
                                }
                            }
                            break;
                    }
                }
            }
        }

        static private string ToUpperFirstLetter(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            // convert to char array of the string
            char[] letters = source.ToCharArray();
            // upper case the first char
            letters[0] = char.ToUpper(letters[0]);
            // return the array made of the new char array
            return new string(letters);
        }

        static private Configuration LoadConfig()
        {
            Configuration config = Configuration.ReadFile(configFileName);
            config["SM"].SetDefaultValue("QuantizeNotes", 192);
            config["SM"].SetDefaultString("DanceMode4", "dance-single");
            config["SM"].SetDefaultString("DanceMode6", "dance-solo");
            config["SM"].SetDefaultString("DanceMode8", "dance-double");
            config["SM"].SetDefaultString("DanceModeCouple", "dance-couple");
            config["SM"].SetDefaultString("Difficulty0", "Challenge");
            config["SM"].SetDefaultString("Difficulty1", "Easy");
            config["SM"].SetDefaultString("Difficulty2", "Medium");
            config["SM"].SetDefaultString("Difficulty3", "Hard");
            config["SM"].SetDefaultString("Difficulty4", "Beginner");
            config["SM"].SetDefaultString("Difficulty5", "Edit");
            config["DDR"].SetDefaultString("Difficulty1", "1");
            config["DDR"].SetDefaultString("Difficulty2", "2");
            config["DDR"].SetDefaultString("Difficulty3", "3");
            config["DDR"].SetDefaultString("Difficulty4", "4");
            config["DDR"].SetDefaultString("Difficulty6", "0");
            return config;
        }

        static private Configuration LoadDB()
        {
            Configuration config = Configuration.ReadFile(databaseFileName);
            return config;
        }
    }
}
