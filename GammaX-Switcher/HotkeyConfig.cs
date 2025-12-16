using System;
using System.Windows.Forms;

namespace GammaX_Switcher
{
    public class HotkeyConfig
    {
        public KeyModifiers Modifier { get; set; } = KeyModifiers.None;
        public Keys Key { get; set; } = Keys.None;

        public string ToStringForIni()
        {
            if (Modifier == KeyModifiers.None && Key == Keys.None)
                return "None|None";
            return $"{Modifier}|{Key}";
        }

        public static HotkeyConfig FromString(string iniStr)
        {
            var config = new HotkeyConfig();
            if (string.IsNullOrEmpty(iniStr) || iniStr == "None|None")
                return config;

            var parts = iniStr.Split('|');
            if (parts.Length == 2)
            {
                KeyModifiers parsedModifier = KeyModifiers.None;
                Keys parsedKey = Keys.None;

                // 解析修饰符（失败时保留默认None）
                if (parts[0] != "None" && !Enum.TryParse(parts[0], out parsedModifier))
                    parsedModifier = KeyModifiers.None;

                // 解析按键（失败时保留默认None）
                if (parts[1] != "None" && !Enum.TryParse(parts[1], out parsedKey))
                    parsedKey = Keys.None;

                config.Modifier = parsedModifier;
                config.Key = parsedKey;
            }
            return config;
        }
    }
}