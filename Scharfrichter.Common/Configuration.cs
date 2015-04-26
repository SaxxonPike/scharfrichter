using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Scharfrichter.Common
{
    public partial class Configuration
    {
        public Dictionary<string, InfoCollection> DB = new Dictionary<string,InfoCollection>();

        private Encoding enc;

        public Configuration()
        {
            enc = Encoding.Unicode;
        }

        public Configuration(Encoding encoding)
        {
            enc = encoding;
        }

        public InfoCollection this[string key]
        {
            get 
            {
                if (!DB.ContainsKey(key))
                    DB[key] = new InfoCollection();
                return DB[key];
            }
            set 
            { 
                DB[key] = value; 
            }
        }

        static public string ConfigPath(string configName)
        {
            string result = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            result = Path.Combine(result, @"Config");
            result = Path.Combine(result, configName + ".txt");
            return result;
        }

        static public Configuration Read(Stream source)
        {
            try
            {
                StreamReader reader = new StreamReader(source);
                Encoding enc = Encoding.Unicode;
                Configuration result = new Configuration(enc);
                InfoCollection currentKey = null;
                string currentKeyName = "";

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (line.Length > 2)
                        {
                            currentKey = new InfoCollection();
                            currentKeyName = line.Substring(1, line.Length - 2);
                            result[currentKeyName] = currentKey;
                        }
                        else
                        {
                            currentKey = null;
                            currentKeyName = "";
                        }
                    }
                    else if (currentKey != null && line.Contains("="))
                    {
                        string keyTag = line.Substring(0, line.IndexOf("=")).Trim().ToUpper();
                        string keyValue = line.Substring(line.IndexOf("=") + 1).Trim();
                        currentKey[keyTag] = keyValue;
                    }
                    else if (line.Length > 0)
                    {
                        currentKey[line] = "";
                    }
                }

                return result;
            }
            catch
            {
                return new Configuration(Encoding.Unicode);
            }
        }

        static public Configuration ReadFile(string configName)
        {
            string configFile = Configuration.ConfigPath(configName);
            if (File.Exists(configFile))
            {
                using (FileStream fs = new FileStream(configFile, FileMode.Open, FileAccess.Read))
                    return Read(fs);
            }
            else
            {
                return new Configuration();
            }
        }

        public void Write(Stream target)
        {
            StreamWriter writer = new StreamWriter(target);
            foreach (KeyValuePair<string, InfoCollection> entry in DB)
            {
                if (entry.Key.Length > 0)
                {
                    writer.WriteLine("[" + entry.Key + "]");
                    foreach (KeyValuePair<string, string> item in entry.Value.Items)
                    {
                        writer.WriteLine(item.Key + "=" + item.Value);
                    }
                }
            }
            writer.Flush();
        }

        public void WriteFile(string configName)
        {
            string configFile = Configuration.ConfigPath(configName);
            Directory.CreateDirectory(Path.GetDirectoryName(configFile));
            using (FileStream fs = new FileStream(configFile, FileMode.Create, FileAccess.Write))
                Write(fs);
        }
    }

    public class InfoCollection
    {
        public Dictionary<string, string> Items = new Dictionary<string,string>();

        public string this[string key]
        {
            get
            {
                key = key.ToUpper();
                if (Items.ContainsKey(key))
                    return Items[key];
                else
                    return "";
            }
            set
            {
                Items[key.ToUpper()] = value;
            }
        }

        public string GetString(string key)
        {
            key = key.ToUpper();
            if (Items.ContainsKey(key))
                return Items[key];
            return "";
        }

        public string GetString(string key, string defaultValue)
        {
            key = key.ToUpper();
            if (!Items.ContainsKey(key))
                Items[key] = defaultValue;
            return Items[key];
        }

        public int GetValue(string key)
        {
            key = key.ToUpper();
            if (Items.ContainsKey(key))
                return Convert.ToInt32(Items[key]);
            return 0;
        }

        public int GetValue(string key, int defaultValue)
        {
            string stringValue = defaultValue.ToString();
            key = key.ToUpper();
            if (!Items.ContainsKey(key))
                Items[key] = stringValue;
            return Convert.ToInt32(Items[key]);
        }

        public void SetDefaultString(string key, string defaultValue)
        {
            key = key.ToUpper();
            if (!Items.ContainsKey(key))
                Items[key] = defaultValue;
        }

        public void SetDefaultValue(string key, int defaultValue)
        {
            key = key.ToUpper();
            if (!Items.ContainsKey(key))
                Items[key] = defaultValue.ToString();
        }

        public void SetString(string key, string newValue)
        {
            Items[key] = newValue;
        }

        public void SetValue(string key, int newValue)
        {
            Items[key] = newValue.ToString();
        }
    }
}
