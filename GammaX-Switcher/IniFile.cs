using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace GammaX_Switcher
{
    internal class IniFile
    {
        private readonly string _iniPath;
        private readonly Encoding _encoding = Encoding.UTF8; // 强制UTF-8编码

        public IniFile(string iniPath = null)
        {
            string exeName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            _iniPath = new FileInfo(iniPath ?? $"{exeName}.ini").FullName;
            
            // 若文件不存在则创建
            if (!File.Exists(_iniPath))
                File.Create(_iniPath).Close();
        }

        // 读取指定节-键的值
        public string Read(string key, string section = null)
        {
            section = section ?? Path.GetFileNameWithoutExtension(_iniPath);
            var iniData = ReadAllIniData();
            
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
                return iniData[section][key];
            return "";
        }

        // 写入指定节-键的值
        public void Write(string key, string value, string section = null)
        {
            section = section ?? Path.GetFileNameWithoutExtension(_iniPath);
            var iniData = ReadAllIniData();
            
            if (!iniData.ContainsKey(section))
                iniData[section] = new Dictionary<string, string>();
            iniData[section][key] = value;
            
            WriteAllIniData(iniData);
        }

        // 删除指定节
        public void DeleteSection(string section = null)
        {
            section = section ?? Path.GetFileNameWithoutExtension(_iniPath);
            var iniData = ReadAllIniData();
            
            if (iniData.ContainsKey(section))
            {
                iniData.Remove(section);
                WriteAllIniData(iniData);
            }
        }

        // 删除指定节的指定键
        public void DeleteKey(string key, string section = null)
        {
            section = section ?? Path.GetFileNameWithoutExtension(_iniPath);
            var iniData = ReadAllIniData();
            
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
            {
                iniData[section].Remove(key);
                WriteAllIniData(iniData);
            }
        }

        // 判断键是否存在
        public bool KeyExists(string key, string section = null)
        {
            section = section ?? Path.GetFileNameWithoutExtension(_iniPath);
            var iniData = ReadAllIniData();
            
            return iniData.ContainsKey(section) && iniData[section].ContainsKey(key);
        }

        // 获取所有节名
        public string[] GetSections()
        {
            var iniData = ReadAllIniData();
            var sections = new List<string>(iniData.Keys);
            return sections.ToArray();
        }

        // 读取所有INI数据到字典
        private Dictionary<string, Dictionary<string, string>> ReadAllIniData()
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string currentSection = "";

            foreach (var line in File.ReadAllLines(_iniPath, _encoding))
            {
                string trimmedLine = line.Trim();
                
                // 忽略空行和注释
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                    continue;
                
                // 匹配节名 [Section]
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    continue;
                }
                
                // 匹配键值 Key=Value
                int equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex > 0)
                {
                    string key = trimmedLine.Substring(0, equalIndex).Trim();
                    string value = trimmedLine.Substring(equalIndex + 1).Trim();
                    
                    if (!iniData.ContainsKey(currentSection))
                        iniData[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    iniData[currentSection][key] = value;
                }
            }

            return iniData;
        }

        // 将字典写入INI文件
        private void WriteAllIniData(Dictionary<string, Dictionary<string, string>> iniData)
        {
            var sb = new StringBuilder();
            
            foreach (var section in iniData)
            {
                sb.AppendLine($"[{section.Key}]");
                foreach (var keyValue in section.Value)
                {
                    sb.AppendLine($"{keyValue.Key}={keyValue.Value}");
                }
                sb.AppendLine(); // 节之间空行分隔
            }

            File.WriteAllText(_iniPath, sb.ToString().TrimEnd(), _encoding);
        }

        public void SaveLanguage(LanguageType lang)
        {
            try
            {
                // 对应LoadLanguage的Read("Language", "Settings")，写入时保持key/section一致
                Write("Language", lang.ToString(), "Settings");
            }
            catch (Exception ex)
            {
                throw new Exception("保存语言配置失败", ex);
            }
        }

        public LanguageType LoadLanguage()
        {
            try
            {
                string langStr = Read("Language", "Settings");
                return string.IsNullOrEmpty(langStr) ? LanguageType.English : (LanguageType)Enum.Parse(typeof(LanguageType), langStr);
            }
            catch
            {
                return LanguageType.English;
            }
        }

        public void SavePresetHotkey(string presetName, HotkeyConfig hotkey)
        {
            Write("Hotkey", hotkey.ToStringForIni(), presetName);
        }

        public HotkeyConfig LoadPresetHotkey(string presetName)
        {
            string hotkeyStr = Read("Hotkey", presetName);
            return HotkeyConfig.FromString(hotkeyStr);
        }
    }
}