/*
 * ConfigManager.cs by ThunderWire Studio
*/

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ThunderWire.Helpers;
using ThunderWire.Utility;

namespace ThunderWire.Configuration {
    /// <summary>
    /// Provides methods for Serialize and Deserialize config files.
    /// </summary>
    public static class ConfigManager {

        private static string folderPath;
        private static string fullPath;

        private static bool enableDebug = false;
        private static bool pathSet = false;

        public static Dictionary<string, Dictionary<string, string>> ConfigDictionary = new Dictionary<string, Dictionary<string, string>>();
		public static List<string> ConfigKeysCache = new List<string>();

        public static void EnableDebugging(bool Enabled)
        {
            enableDebug = Enabled;
        }

        public static void SetFilename(string Filename)
        {
            if (!pathSet)
            {
                folderPath = Tools.GetFolderPath(FilePath.GameDataPath);
            }

            if (Filename.Contains('.'))
            {
                fullPath = folderPath + Filename;
            }
            else
            {
                fullPath = folderPath + Filename + ".cfg";
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            ReadFromConfig();
        }

        public static void SetFilePath(FilePath Filepath)
        {
            folderPath = Tools.GetFolderPath(Filepath);
            pathSet = true;
        }

        public static async void Serialize(string Section, string Key, string Value)
        {
            UpdateDictionary(Section, Key, Value);
            await WriteConfig();
        }

        public static async void Serialize(string Section, Dictionary<string, string> keyValuePairs)
        {
            if (keyValuePairs.Count > 0)
            {
                foreach (var i in keyValuePairs)
                {
                    UpdateDictionary(Section, i.Key, i.Value);
                }

                await WriteConfig();
            }
            else if (enableDebug) { Debug.Log("<color=yellow>Dictionary is Empty!</color>"); }
        }

        public static void CreateSection(string Section)
        {
            ConfigDictionary.Add(Section, new Dictionary<string, string>() { {" ", " " } });
        }

        public static string Deserialize(string Section, string Key)
        {
            if (ConfigDictionary.ContainsKey(Section))
            {
                if (ConfigDictionary[Section].ContainsKey(Key))
                {
                    return ConfigDictionary[Section][Key];
                }
                else
                {
                    if (enableDebug)
                    {
                        Debug.Log("<color=yellow>No key in this section found</color>");
                    }
                    return null;
                }
            }
            else
            {
                if (enableDebug) { Debug.Log("<color=yellow>No section with this name found</color>"); }
                return null;
            }
        }

        /// <summary>
        /// This method works only with system types.
        /// </summary>
        public static T Deserialize<T>(string Section, string Key)
        {
            if (ConfigDictionary.ContainsKey(Section))
            {
                if (ConfigDictionary[Section].ContainsKey(Key))
                {
                    return Parser.Convert<T>(ConfigDictionary[Section][Key]);
                }
                else
                {
                    if (enableDebug)
                    {
                        Debug.Log("<color=yellow>No key in this section found</color>");
                    }
                    return default(T);
                }
            }
            else
            {
                if (enableDebug) { Debug.Log("<color=yellow>No section with this name found</color>"); }
                return default(T);
            }
        }

        public static Dictionary<string, string> DeserializeSection(string Section)
        {
            if (ConfigDictionary.ContainsKey(Section))
            {
                return ConfigDictionary[Section];
            }
            else
            {
                if (enableDebug) { Debug.Log("<color=yellow>No section with this name found</color>"); }
                return null;
            }
        }

        public static bool ContainsSection(string Section) {
            return ConfigDictionary.ContainsKey (Section);
		}

        public static bool ContainsSectionKey(string Section, string Key)
        {
            if (ConfigDictionary.ContainsKey(Section))
            {
                return ConfigDictionary[Section].ContainsKey(Key);
            }
            else
            {
                return false;
            }
        }

        public static void RemoveSectionKey(string Section, string Key)
        {
            if (ConfigDictionary[Section].ContainsKey(Key))
            {
                Debug.Log(ConfigDictionary[Section].Count);
                if (ConfigDictionary[Section].Count == 1)
                {
                    ConfigDictionary.Remove(Section);
                    ReplaceValues();
                }
                else
                {
                    ConfigDictionary[Section].Remove(Key);
                    ReplaceValues();
                }
            }
        }
		
		public static void RemoveSection(string Section) {
			if (ConfigDictionary.ContainsKey(Section)) 
			{
				ConfigDictionary.Remove(Section);
				ReplaceValues();
			}
		}

		public static int GetSectionKeys(string Section)
		{
			return ConfigDictionary[Section].Count;
		}

        public static bool ExistFile(string file)
        {
            if (enableDebug)
            {
                string color = "";
                if (File.Exists(folderPath + CheckExtension(file)))
                {
                    color = "green";
                }
                else
                {
                    color = "red";
                }

                Debug.Log("<color=" + color + ">" + folderPath + CheckExtension(file) + "</color> ");
            }

            return File.Exists(folderPath + CheckExtension(file));
        }

        public static bool ExistFileWithPath(string path, string file)
        {
            string defaultPath = path + CheckExtension(file);
            return File.Exists(defaultPath);
        }

        public static void RemoveFile(FilePath filePath, string file)
        {
            string pathfile = Tools.GetFolderPath(filePath) + file + ".cfg";
            if (File.Exists(pathfile))
            {
                File.Delete(pathfile);
                if (enableDebug) { Debug.Log("<color=red>Config File Removed:</color> " + pathfile); }
            }
            else
            {
                if (enableDebug) { Debug.Log("<color=yellow>Config File Not Found:</color> " + pathfile); }
            }
        }

        public static void DuplicateFile(FilePath filePath, string file, string duplicate)
        {
            string pathfile = Tools.GetFolderPath(filePath) + CheckExtension(file);
            if (File.Exists(pathfile))
            {
                File.Copy(pathfile, Tools.GetFolderPath(filePath) + duplicate + ".cfg");
            }
            else
            {
                if (enableDebug) { Debug.Log("<color=yellow>Config File Not Found:</color> " + pathfile); }
            }
        }

        public static string GetFileAndPath(FilePath filePath, string file)
		{
            return Tools.GetFolderPath(filePath) + CheckExtension(file);
        }

        public static string GetFileAndPathFolder(FilePath filePath, string folder, string file)
		{
            return Tools.GetFolderPath(filePath, true) + "/" + folder + "/" + CheckExtension(file);
        }

        private static string CheckExtension(string file)
        {
            if (file.Contains('.'))
            {
                return file;
            }
            else
            {
                return file + ".cfg";
            }
        }

        private static void ReplaceValues()
        {
            using (StreamWriter sw = new StreamWriter(fullPath))
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> Section in ConfigDictionary)
                {
                    sw.WriteLine("[" + Section.Key + "]");
                    foreach (KeyValuePair<string, string> InSection in Section.Value)
                    {
                        string key = InSection.Key.ToString();
                        string value = InSection.Value.ToString();
                        sw.WriteLine(key + " \"" + value + "\"");
                    }
                }
            }
        }

        private static void ReadFromConfig()
        {
            if (File.Exists(fullPath))
            {
                if (string.IsNullOrEmpty(fullPath))
                {
                    return;
                }

                using (StreamReader reader = new StreamReader(fullPath))
                {
                    string line;
                    string m_Section = "";
                    string m_Key = "";
                    string m_Value = "";

                    while (reader.Peek() != -1)
                    {
                        line = reader.ReadLine();

                        if (!string.IsNullOrEmpty(line))
                        {
                            line.Trim();

                            if (line.StartsWith("[") && line.EndsWith("]"))
                            {
                                m_Section = line.Substring(1, line.Length - 2);
                                m_Key = "";
                                m_Value = "";
                            }
                            else
                            {
                                string[] ln_Input = line.Split(new char[] { '"' });
                                m_Key = ln_Input[0].Trim();
                                m_Value = ln_Input[1].Trim();
                            }
                            if (m_Section == "" || m_Key == "" || m_Value == "")
                                continue;

                            UpdateDictionary(m_Section, m_Key, m_Value);
                            ConfigKeysCache.Add(m_Key);
                        }
                    }
                }
            }
        }

        private static void UpdateDictionary(string Section, string Key, string Value)
        {
            if (ConfigDictionary.ContainsKey(Section))
            {
                if (ConfigDictionary[Section].Count > 0 && ConfigDictionary[Section].ContainsKey(Key))
                {
                    ConfigDictionary[Section][Key] = Value;
                }
                else
                {
                    ConfigDictionary[Section].Add(Key, Value);
                }
            }
            else
            {
                Dictionary<string, string> KeyValueDict = new Dictionary<string, string>
                {
                    { Key, Value }
                };
                ConfigDictionary.Add(Section, KeyValueDict);
            }
        }

        private static async Task WriteConfig()
        {
            bool divideSections = false;

            if (File.Exists(fullPath))
            {
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    string line = reader.ReadLine();

                    if (!string.IsNullOrEmpty(line) || line == " ")
                    {
                        divideSections = true;
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(fullPath))
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> Section in ConfigDictionary)
                {
                    string section = Section.Key.ToString();

                    if (divideSections)
                    {
                        await sw.WriteLineAsync("");
                    }
                    else
                    {
                        divideSections = true;
                    }

                    await sw.WriteLineAsync("[" + section + "]");

                    foreach (KeyValuePair<string, string> InSection in Section.Value)
                    {
                        string key = InSection.Key.ToString();
                        string value = InSection.Value.ToString();
                        value = value.Replace(Environment.NewLine, " ");
                        value = value.Replace("\r\n", " ");
                        await sw.WriteLineAsync(key + " \"" + value + "\"");
                    }
                }
            }
        }
	}
}
