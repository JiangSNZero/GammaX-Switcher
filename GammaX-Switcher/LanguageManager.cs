using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace GammaX_Switcher
{
    // 语言类型枚举
    public enum LanguageType
    {
        English,
        Chinese
    }

    public static class LanguageManager
    {
        // 当前语言
        public static LanguageType CurrentLanguage { get; private set; } = LanguageType.English;

        // 切换语言并刷新指定窗体
        public static void SwitchLanguage(LanguageType lang, Form targetForm)
        {
            CurrentLanguage = lang;

            // 设置线程语言
            CultureInfo culture = lang == LanguageType.English 
                ? new CultureInfo("en-US") 
                : new CultureInfo("zh-CN");
    
            // 强制UI线程执行
            targetForm.Invoke(new Action(() => 
            {
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                RefreshFormText(targetForm);
            }));
    
            // 调用公开方法刷新预设/托盘菜单
            if (targetForm is Window window)
            {
                window.RefreshPresets();
                window.RefreshTrayMenu();
            }
        }

        // 递归刷新控件文本（适配嵌套控件）
        public static void RefreshFormText(Control parentCtrl)
        {
            foreach (Control ctrl in parentCtrl.Controls)
            {
                // 1. 跳过ComboBox/ListBox等下拉类控件（关键！）
                if (ctrl is ComboBox || ctrl is ListBox || ctrl is ToolStripComboBox)
                {
                    // 递归处理子控件（如ComboBox的子控件）
                    if (ctrl.HasChildren)
                        RefreshFormText(ctrl);
                    continue;
                }

                // 2. 仅刷新文本类控件的Text属性
                if (!string.IsNullOrEmpty(ctrl.Name) &&
                    (ctrl is Label || ctrl is Button || ctrl is LinkLabel ||
                     ctrl is CheckBox || ctrl is RadioButton || ctrl is GroupBox))
                {
                    string text = GetText(ctrl.Name);
                    if (!string.IsNullOrEmpty(text))
                        ctrl.Text = text;
                }

                // 3. 处理菜单控件（如ToolStripMenuItem）
                if (ctrl is ToolStrip)
                {
                    foreach (ToolStripItem item in ((ToolStrip)ctrl).Items)
                    {
                        if (!string.IsNullOrEmpty(item.Name) && item is ToolStripMenuItem)
                        {
                            string text = GetText(item.Name);
                            if (!string.IsNullOrEmpty(text))
                                item.Text = text;
                        }

                        // 递归处理子菜单
                        if (item is ToolStripMenuItem && ((ToolStripMenuItem)item).DropDownItems.Count > 0)
                        {
                            foreach (ToolStripItem subItem in ((ToolStripMenuItem)item).DropDownItems)
                            {
                                if (!string.IsNullOrEmpty(subItem.Name))
                                {
                                    string text = GetText(subItem.Name);
                                    if (!string.IsNullOrEmpty(text))
                                        subItem.Text = text;
                                }
                            }
                        }
                    }
                }

                // 4. 递归处理子控件（如Panel/GroupBox内的控件）
                if (ctrl.HasChildren)
                    RefreshFormText(ctrl);
            }
        }

        // 获取指定键的当前语言文本（用于动态生成的文本，如托盘菜单）
        public static string GetText(string key)
        {
            return Properties.Resources.ResourceManager.GetString(key, Thread.CurrentThread.CurrentUICulture);
        }
    }
}