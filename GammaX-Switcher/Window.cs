using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GammaX_Switcher
{
    public partial class Window : Form
    {
        private readonly CultureInfo _customCulture;
        private readonly IniFile _iniFile;

        private readonly List<Display.DisplayInfo> _displays;
        private int _numDisplay;
        private Display.DisplayInfo _currDisplay;

        private readonly List<ToolStripComboBox> _toolMonitors = new List<ToolStripComboBox>();
        private ToolStripComboBox _toolMonitor;

        private bool _disableChangeFunc;

        private bool _allColors = true;
        private bool _redColor;
        private bool _greenColor;

        private bool _blueColor;

        private readonly bool _isHotkeyTriggered = false; // 新增：标记快捷键触发

        // 存储当前捕获的快捷键（修饰符+按键）
        private KeyModifiers _currentHotkeyModifier = KeyModifiers.None;

        private Keys _currentHotkeyKey = Keys.None;

        // 全局热键核心API（Windows系统提供，需引入user32.dll）
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 存储「预设节名 → 热键ID」的映射（避免热键ID冲突）
        private readonly Dictionary<string, int> _hotkeyIdMap = new Dictionary<string, int>();

        // 热键ID自增器（起始值建议>1000，避开系统预留ID）
        private int _nextHotkeyId = 1000;

        // Window类的公开方法（修复私有方法访问报错）
        public void RefreshPresets()
        {
            SafeInvoke(InitPresets);
        }

        public void RefreshTrayMenu()
        {
            SafeInvoke(InitTrayMenu);
        }

        private void ClearColors()
        {
            buttonAllColors.Font = new Font(buttonAllColors.Font.Name, buttonAllColors.Font.Size, FontStyle.Regular);
            buttonRed.Font = new Font(buttonRed.Font.Name, buttonRed.Font.Size, FontStyle.Regular);
            buttonGreen.Font = new Font(buttonGreen.Font.Name, buttonGreen.Font.Size, FontStyle.Regular);
            buttonBlue.Font = new Font(buttonBlue.Font.Name, buttonBlue.Font.Size, FontStyle.Regular);

            _allColors = false;
            _redColor = false;
            _greenColor = false;
            _blueColor = false;
        }

        private void InitPresets()
        {
            SafeInvoke(() => // 强制UI线程执行
            {
                comboBoxPresets.Items.Clear();
                string[] presets = _iniFile.GetSections();
                if (presets != null)
                {
                    foreach (string preset in presets)
                    {
                        if (_iniFile.Read("monitor", preset).Equals(_currDisplay.displayName))
                        {
                            string purePresetName = GetPurePresetName(preset);
                            comboBoxPresets.Items.Add(purePresetName);
                            Debug.WriteLine($"initPresets: 加载预设[{purePresetName}]到下拉框");
                        }
                    }
                }

                // 重新绑定事件（确保在UI线程）
                comboBoxPresets.SelectedIndexChanged -= comboBoxPresets_SelectedIndexChanged;
                comboBoxPresets.SelectedIndexChanged += comboBoxPresets_SelectedIndexChanged;
            });
        }

        private void InitTrayMenu()
        {
            contextMenu.Items.Clear();
            _toolMonitors.Clear();

            // 1. 替换硬编码的"Settings"为多语言资源文本
            ToolStripMenuItem toolSetting = new ToolStripMenuItem(
                LanguageManager.GetText("txtSettings"), // 读取资源文件中的"设置/Settings"
                null,
                toolSettings_Click
            );
            contextMenu.Items.Add(toolSetting);

            ToolStripSeparator toolStripSeparator1 = new ToolStripSeparator();
            contextMenu.Items.Add(toolStripSeparator1);

            foreach (var t in _displays)
            {
                _toolMonitor = new ToolStripComboBox(t.displayName);
                _toolMonitor.DropDownStyle = ComboBoxStyle.DropDownList;

                // 显示器名称后缀保持原逻辑（无需翻译，是系统显示器名）
                _toolMonitor.Items.Add(t.displayName + ":");
                _toolMonitor.Text = t.displayName + @":";

                _toolMonitor.SelectedIndexChanged += comboBoxToolMonitor_IndexChanged;

                string[] presets = _iniFile.GetSections();
                if (presets != null)
                {
                    foreach (var t1 in presets)
                    {
                        if (_iniFile.Read("monitor", t1).Equals(t.displayName))
                        {
                            // 预设名称保留用户自定义内容（无需翻译）
                            _toolMonitor.Items.Add(t1);
                        }
                    }
                }

                _toolMonitors.Add(_toolMonitor);
                contextMenu.Items.Add(_toolMonitor);
            }

            ToolStripSeparator toolStripSeparator2 = new ToolStripSeparator();
            contextMenu.Items.Add(toolStripSeparator2);

            // 2. 替换硬编码的"Exit"为多语言资源文本
            ToolStripMenuItem toolExit = new ToolStripMenuItem(
                LanguageManager.GetText("txtExit"), // 读取资源文件中的"退出/Exit"
                null,
                toolExit_Click
            );
            contextMenu.Items.Add(toolExit);

            // 3. 同步更新托盘图标提示文本（可选，提升体验）
            notifyIcon.Text = LanguageManager.GetText("notifyIcon.Text");
        }

        private void FillInfo(Display.DisplayInfo currDisplay)
        {
            _disableChangeFunc = true;

            textBoxGamma.Text = ((currDisplay.rGamma + currDisplay.gGamma + currDisplay.bGamma) / 3f).ToString("0.00");
            textBoxContrast.Text =
                ((currDisplay.rContrast + currDisplay.gContrast + currDisplay.bContrast) / 3f).ToString("0.00");
            textBoxBrightness.Text =
                ((currDisplay.rBright + currDisplay.gBright + currDisplay.bBright) / 3f).ToString("0.00");

            trackBarGamma.Value = (int)((currDisplay.rGamma + currDisplay.gGamma + currDisplay.bGamma) / 3f * 100f);
            trackBarContrast.Value =
                (int)((currDisplay.rContrast + currDisplay.gContrast + currDisplay.bContrast) / 3f * 100f);
            trackBarBrightness.Value =
                (int)((currDisplay.rBright + currDisplay.gBright + currDisplay.bBright) / 3f * 100f);

            if (currDisplay.isExternal)
            {
                labelMonitorContrastUp.Visible = true;
                labelMonitorContrastDown.Visible = true;
                trackBarMonitorContrast.Visible = true;
                textBoxMonitorContrast.Visible = true;

                textBoxMonitorBrightness.Text = ExternalMonitor.GetBrightness(currDisplay.PhysicalHandle).ToString();
                trackBarMonitorBrightness.Value = ExternalMonitor.GetBrightness(currDisplay.PhysicalHandle);

                textBoxMonitorContrast.Text = ExternalMonitor.GetContrast(currDisplay.PhysicalHandle).ToString();
                trackBarMonitorContrast.Value = ExternalMonitor.GetContrast(currDisplay.PhysicalHandle);
            }
            else
            {
                labelMonitorContrastUp.Visible = false;
                labelMonitorContrastDown.Visible = false;
                trackBarMonitorContrast.Visible = false;
                textBoxMonitorContrast.Visible = false;

                textBoxMonitorBrightness.Text = InternalMonitor.GetBrightness().ToString();
                trackBarMonitorBrightness.Value = InternalMonitor.GetBrightness();
            }

            _disableChangeFunc = false;
        }

        private void Window_Load(object sender, EventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Size.Width;
            int windowWidth = Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Size.Height;
            int windowHeight = Height;
            int tmp = Screen.PrimaryScreen.Bounds.Height;
            int taskBarHeight = tmp - Screen.PrimaryScreen.WorkingArea.Height;

            //dpi
            /*int PSH = SystemParameters.PrimaryScreenHeight;
            int PSBH = Screen.PrimaryScreen.Bounds.Height;
            double ratio = PSH / PSBH;
            int TaskBarHeight = PSBH - Screen.PrimaryScreen.WorkingArea.Height;
            TaskBarHeight *= ratio;*/
            // ========== 新增：读取并应用语言配置 ==========

            // 1. 从INI读取Language字段（默认English）
            string langStr = _iniFile.Read("Language", "Settings");
            if (!Enum.TryParse(langStr, out LanguageType currentLang))
            {
                currentLang = LanguageType.English; // 解析失败时默认英文
            }

            // 2. 应用语言配置到界面（替换为你实际的语言切换方法）
            LanguageManager.SwitchLanguage(currentLang, this);

            Location = new Point(screenWidth - windowWidth, screenHeight - (windowHeight + taskBarHeight));
            // 初始化预设 + 注册热键
            InitTrayMenu();
            InitPresets();
            RegisterAllPresetHotkeys();
        }

        public Window()
        {
            InitializeComponent();
            KeyPreview = true;

            // 绑定快捷键文本框的按键监听
            txtHotkey.KeyDown += TxtHotkey_KeyDown;
            txtHotkey.KeyUp += TxtHotkey_KeyUp;

            // 初始化时清空文本框
            txtHotkey.Text = "";
            _customCulture =
                (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            _customCulture.NumberFormat.NumberDecimalSeparator = ",";

            _iniFile = new IniFile("GammaManager.ini");

            buttonAllColors.Font = new Font(buttonAllColors.Font.Name, buttonAllColors.Font.Size, FontStyle.Bold);


            _displays = Display.QueryDisplayDevices();
            _displays.Reverse();
            for (int i = 0; i < _displays.Count; i++)
            {
                _displays[i].numDisplay = i;
                comboBoxMonitors.Items.Add(i + 1 + ") " + _displays[i].displayName);
            }

            _currDisplay = _displays[_numDisplay];
            comboBoxMonitors.SelectedIndex = _numDisplay;

            FillInfo(_currDisplay);

            InitPresets();

            InitTrayMenu();
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void trackBarGamma_ValueChanged(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            if (!_disableChangeFunc)
            {
                textBoxGamma.Text = (trackBarGamma.Value / 100f).ToString("0.00");

                if (_allColors)
                {
                    _currDisplay.rGamma = trackBarGamma.Value / 100f;
                    _currDisplay.gGamma = trackBarGamma.Value / 100f;
                    _currDisplay.bGamma = trackBarGamma.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_redColor)
                {
                    _currDisplay.rGamma = trackBarGamma.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_greenColor)
                {
                    _currDisplay.gGamma = trackBarGamma.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_blueColor)
                {
                    _currDisplay.bGamma = trackBarGamma.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                }

                EndColors: ;
            }
        }


        private void trackBarContrast_ValueChanged(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            if (!_disableChangeFunc)
            {
                textBoxContrast.Text = (trackBarContrast.Value / 100f).ToString("0.00");

                if (_allColors)
                {
                    _currDisplay.rContrast = trackBarContrast.Value / 100f;
                    _currDisplay.gContrast = trackBarContrast.Value / 100f;
                    _currDisplay.bContrast = trackBarContrast.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_redColor)
                {
                    _currDisplay.rContrast = trackBarContrast.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_greenColor)
                {
                    _currDisplay.gContrast = trackBarContrast.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_blueColor)
                {
                    _currDisplay.bContrast = trackBarContrast.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                }

                EndColors: ;
            }
        }

        private void trackBarBrightness_ValueChanged(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            if (!_disableChangeFunc)
            {
                textBoxBrightness.Text = (trackBarBrightness.Value / 100f).ToString("0.00");

                if (_allColors)
                {
                    _currDisplay.rBright = trackBarBrightness.Value / 100f;
                    _currDisplay.gBright = trackBarBrightness.Value / 100f;
                    _currDisplay.bBright = trackBarBrightness.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_redColor)
                {
                    _currDisplay.rBright = trackBarBrightness.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_greenColor)
                {
                    _currDisplay.gBright = trackBarBrightness.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                    goto EndColors;
                }

                if (_blueColor)
                {
                    _currDisplay.bBright = trackBarBrightness.Value / 100f;
                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast,
                            _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright, _currDisplay.gBright,
                            _currDisplay.bBright));
                }

                EndColors: ;
            }
        }

        private void trackBarMonitorBrightness_ValueChanged(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            if (!_disableChangeFunc)
            {
                textBoxMonitorBrightness.Text = trackBarMonitorBrightness.Value.ToString();

                _currDisplay.monitorBrightness = trackBarMonitorBrightness.Value;

                if (_currDisplay.isExternal)
                {
                    ExternalMonitor.SetBrightness(_currDisplay.PhysicalHandle, (uint)trackBarMonitorBrightness.Value);
                }
                else
                {
                    InternalMonitor.SetBrightness((byte)trackBarMonitorBrightness.Value);
                }
            }
        }

        private void trackBarMonitorContrast_ValueChanged(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            if (!_disableChangeFunc)
            {
                textBoxMonitorContrast.Text = trackBarMonitorContrast.Value.ToString();

                _currDisplay.monitorContrast = trackBarMonitorContrast.Value;

                ExternalMonitor.SetContrast(_currDisplay.PhysicalHandle, (uint)trackBarMonitorContrast.Value);
            }
        }

        private void buttonAllColors_Click(object sender, EventArgs e)
        {
            _disableChangeFunc = true;
            ClearColors();
            _allColors = true;

            textBoxGamma.Text =
                ((_currDisplay.rGamma + _currDisplay.gGamma + _currDisplay.bGamma) / 3f).ToString("0.00");
            textBoxContrast.Text =
                ((_currDisplay.rContrast + _currDisplay.gContrast + _currDisplay.bContrast) / 3f).ToString("0.00");
            textBoxBrightness.Text =
                ((_currDisplay.rBright + _currDisplay.gBright + _currDisplay.bBright) / 3f).ToString("0.00");

            trackBarGamma.Value =
                (int)((_currDisplay.rGamma + _currDisplay.gGamma + _currDisplay.bGamma) / 3f * 100f);
            trackBarContrast.Value =
                (int)((_currDisplay.rContrast + _currDisplay.gContrast + _currDisplay.bContrast) / 3f * 100f);
            trackBarBrightness.Value =
                (int)((_currDisplay.rBright + _currDisplay.gBright + _currDisplay.bBright) / 3f * 100f);

            buttonAllColors.Font = new Font(buttonAllColors.Font.Name, buttonAllColors.Font.Size, FontStyle.Bold);
            _disableChangeFunc = false;
        }

        private void buttonRed_Click(object sender, EventArgs e)
        {
            _disableChangeFunc = true;
            ClearColors();
            _redColor = true;

            textBoxGamma.Text = _currDisplay.rGamma.ToString("0.00");
            textBoxContrast.Text = _currDisplay.rContrast.ToString("0.00");
            textBoxBrightness.Text = _currDisplay.rBright.ToString("0.00");

            trackBarGamma.Value = (int)(_currDisplay.rGamma * 100f);
            trackBarContrast.Value = (int)(_currDisplay.rContrast * 100f);
            trackBarBrightness.Value = (int)(_currDisplay.rBright * 100f);

            buttonRed.Font = new Font(buttonRed.Font.Name, buttonRed.Font.Size, FontStyle.Bold);
            _disableChangeFunc = false;
        }

        private void buttonGreen_Click(object sender, EventArgs e)
        {
            _disableChangeFunc = true;
            ClearColors();
            _greenColor = true;

            textBoxGamma.Text = _currDisplay.gGamma.ToString("0.00");
            textBoxContrast.Text = _currDisplay.gContrast.ToString("0.00");
            textBoxBrightness.Text = _currDisplay.gBright.ToString("0.00");

            trackBarGamma.Value = (int)(_currDisplay.gGamma * 100f);
            trackBarContrast.Value = (int)(_currDisplay.gContrast * 100f);
            trackBarBrightness.Value = (int)(_currDisplay.gBright * 100f);

            buttonGreen.Font = new Font(buttonGreen.Font.Name, buttonGreen.Font.Size, FontStyle.Bold);
            _disableChangeFunc = false;
        }

        private void buttonBlue_Click(object sender, EventArgs e)
        {
            _disableChangeFunc = true;
            ClearColors();
            _blueColor = true;

            textBoxGamma.Text = _currDisplay.bGamma.ToString("0.00");
            textBoxContrast.Text = _currDisplay.bContrast.ToString("0.00");
            textBoxBrightness.Text = _currDisplay.bBright.ToString("0.00");

            trackBarGamma.Value = (int)(_currDisplay.bGamma * 100f);
            trackBarContrast.Value = (int)(_currDisplay.bContrast * 100f);
            trackBarBrightness.Value = (int)(_currDisplay.bBright * 100f);

            buttonBlue.Font = new Font(buttonBlue.Font.Name, buttonBlue.Font.Size, FontStyle.Bold);
            _disableChangeFunc = false;
        }

        private void checkBoxExContrast_CheckedChanged(object sender, EventArgs e)
        {
            trackBarContrast.Maximum = checkBoxExContrast.Checked ? 10000 : 300;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            // 1. 过滤/转义预设名中的特殊字符，避免INI节名乱码
            string tmp = comboBoxPresets.Text.Trim();
            // 移除/替换INI不支持的特殊字符（如中文乱码根源是编码，这里先清理非法字符）
            string safePresetName = System.Text.RegularExpressions.Regex.Replace(tmp, @"[\\/:*?""<>|]", "_");
            // 拼接最终预设节名（显示器名 + 安全预设名）
            string presetName = $"{_currDisplay.displayName}: {safePresetName}";

            // ========== 原有保存逻辑（优化数值格式） ==========
            _iniFile.Write("monitor", _currDisplay.displayName, presetName);
            // 强制使用点（.）作为小数分隔符，避免逗号（,）导致解析失败
            _iniFile.Write("rGamma", _currDisplay.rGamma.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            _iniFile.Write("gGamma", _currDisplay.gGamma.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            _iniFile.Write("bGamma", _currDisplay.bGamma.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            _iniFile.Write("rContrast", _currDisplay.rContrast.ToString("0.00", CultureInfo.InvariantCulture),
                presetName);
            _iniFile.Write("gContrast", _currDisplay.gContrast.ToString("0.00", CultureInfo.InvariantCulture),
                presetName);
            _iniFile.Write("bContrast", _currDisplay.bContrast.ToString("0.00", CultureInfo.InvariantCulture),
                presetName);
            _iniFile.Write("rBright", _currDisplay.rBright.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            _iniFile.Write("gBright", _currDisplay.gBright.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            _iniFile.Write("bBright", _currDisplay.bBright.ToString("0.00", CultureInfo.InvariantCulture), presetName);
            // 整数直接保存，无需格式
            _iniFile.Write("monitorBrightness", _currDisplay.monitorBrightness.ToString(), presetName);
            _iniFile.Write("monitorContrast", _currDisplay.monitorContrast.ToString(), presetName);

            // ========== 新增：保存快捷键（适配INI中None格式） ==========
            string hotkeyModifier =
                _currentHotkeyModifier == KeyModifiers.None ? "None" : _currentHotkeyModifier.ToString();
            string hotkeyKey = _currentHotkeyKey == Keys.None ? "None" : _currentHotkeyKey.ToString();
            string hotkeyStr = $"{hotkeyModifier}|{hotkeyKey}";
            _iniFile.Write("Hotkey", hotkeyStr, presetName);

            // ========== 原有后续逻辑（修正下拉框显示） ==========
            InitPresets();
            // 下拉框显示安全预设名（而非完整节名），避免重复显示"显示器名: "
            comboBoxPresets.Text = safePresetName;
            InitTrayMenu();

            RegisterAllPresetHotkeys();

            // 保存后清空快捷键文本框
            txtHotkey.Text = "";
            _currentHotkeyModifier = KeyModifiers.None;
            _currentHotkeyKey = Keys.None;

            // 提示保存成功
            MessageBox.Show($@"预设「{safePresetName}」保存成功！", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboBoxPresets.Text))
            {
                MessageBox.Show(@"请先选中要删除的预设！", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 拼接安全节名（和保存时一致）
            string safePresetName =
                System.Text.RegularExpressions.Regex.Replace(comboBoxPresets.Text.Trim(), @"[\\/:*?""<>|]", "_");
            string presetName = $"{_currDisplay.displayName}: {safePresetName}";

            if (_iniFile.KeyExists("monitor", presetName))
            {
                _iniFile.DeleteSection(presetName);
                MessageBox.Show($@"预设「{safePresetName}」已删除！", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(@"未找到该预设，删除失败！", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            InitPresets();
            InitTrayMenu();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            comboBoxPresets.Text = string.Empty;

            buttonAllColors.PerformClick();

            trackBarGamma.Value = 100;
            trackBarContrast.Value = 100;
            trackBarBrightness.Value = 0;

            _currDisplay.rGamma = 1;
            _currDisplay.gGamma = 1;
            _currDisplay.bGamma = 1;
            _currDisplay.rContrast = 1;
            _currDisplay.gContrast = 1;
            _currDisplay.bContrast = 1;
            _currDisplay.rBright = 0;
            _currDisplay.gBright = 0;
            _currDisplay.bBright = 0;


            if (_currDisplay.isExternal)
            {
                trackBarMonitorBrightness.Value = 100;

                trackBarMonitorContrast.Value = 50;
            }
            else
            {
                trackBarMonitorBrightness.Value = 100;
            }


            Gamma.SetGammaRamp(_displays[_numDisplay].displayLink, Gamma.CreateGammaRamp(1, 1, 1, 1, 1, 1, 0, 0, 0));

            InitPresets();
            InitTrayMenu();
        }

        private void buttonHide_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void comboBoxMonitors_SelectedIndexChanged(object sender, EventArgs e)
        {
            string num = comboBoxMonitors.SelectedItem.ToString();

            num = num.Substring(0, num.IndexOf(")", StringComparison.Ordinal));
            _numDisplay = Int32.Parse(num) - 1;

            _currDisplay = _displays[_numDisplay];
            FillInfo(_currDisplay);

            InitPresets();
        }

        private void buttonForward_Click(object sender, EventArgs e)
        {
            if (_numDisplay + 1 <= _displays.Count - 1)
            {
                comboBoxMonitors.SelectedIndex = _numDisplay + 1;
            }
            else
            {
                comboBoxMonitors.SelectedIndex = 0;
            }
        }

        // 独立的预设选中事件（避免重复绑定）
        private void comboBoxPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_disableChangeFunc || comboBoxPresets.SelectedIndex == -1)
                return;

            // 拼接完整节名
            string purePresetName = comboBoxPresets.Text.Trim();
            string presetName = $"{_currDisplay.displayName}: {purePresetName}";

            // ========== 读取数值（适配UTF-8 INI） ==========
            try
            {
                _currDisplay.rGamma = float.Parse(_iniFile.Read("rGamma", presetName), CultureInfo.InvariantCulture);
                _currDisplay.gGamma = float.Parse(_iniFile.Read("gGamma", presetName), CultureInfo.InvariantCulture);
                _currDisplay.bGamma = float.Parse(_iniFile.Read("bGamma", presetName), CultureInfo.InvariantCulture);
                _currDisplay.rContrast =
                    float.Parse(_iniFile.Read("rContrast", presetName), CultureInfo.InvariantCulture);
                _currDisplay.gContrast =
                    float.Parse(_iniFile.Read("gContrast", presetName), CultureInfo.InvariantCulture);
                _currDisplay.bContrast =
                    float.Parse(_iniFile.Read("bContrast", presetName), CultureInfo.InvariantCulture);
                _currDisplay.rBright = float.Parse(_iniFile.Read("rBright", presetName), CultureInfo.InvariantCulture);
                _currDisplay.gBright = float.Parse(_iniFile.Read("gBright", presetName), CultureInfo.InvariantCulture);
                _currDisplay.bBright = float.Parse(_iniFile.Read("bBright", presetName), CultureInfo.InvariantCulture);
                _currDisplay.monitorBrightness = int.Parse(_iniFile.Read("monitorBrightness", presetName));
                _currDisplay.monitorContrast = int.Parse(_iniFile.Read("monitorContrast", presetName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($@"读取预设失败：{ex.Message}", @"错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ========== 读取并回显快捷键 ==========
            string hotkeyStr = _iniFile.Read("Hotkey", presetName);
            if (!string.IsNullOrEmpty(hotkeyStr))
            {
                var hotkeyConfig = HotkeyConfig.FromString(hotkeyStr);
                _currentHotkeyModifier = hotkeyConfig.Modifier;
                _currentHotkeyKey = hotkeyConfig.Key;
                UpdateHotkeyDisplay(); // 刷新输入栏显示
            }
            else
            {
                txtHotkey.Text = "";
                _currentHotkeyModifier = KeyModifiers.None;
                _currentHotkeyKey = Keys.None;
            }

            // ========== 原有逻辑 ==========
            FillInfo(_currDisplay);
            ClearColors();
            buttonAllColors.PerformClick();
            InitTrayMenu();

            Gamma.SetGammaRamp(_currDisplay.displayLink,
                Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                    _currDisplay.rContrast, _currDisplay.gContrast, _currDisplay.bContrast, _currDisplay.rBright,
                    _currDisplay.gBright,
                    _currDisplay.bBright));

            if (_currDisplay.isExternal)
            {
                trackBarMonitorBrightness.Value = _currDisplay.monitorBrightness;
                trackBarMonitorContrast.Value = _currDisplay.monitorContrast;
            }
            else
            {
                trackBarMonitorBrightness.Value = _currDisplay.monitorBrightness;
            }
        }

        //tray
        private void Window_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void toolSettings_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void toolExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBoxToolMonitor_IndexChanged(object sender, EventArgs e)
        {
            if (!_disableChangeFunc)
            {
                string monitor = sender.ToString()
                    .Substring(0, sender.ToString().IndexOf(":", StringComparison.Ordinal));
                /*string comName = toolMonitor.Items[i].ToString().Substring
                        (0, toolMonitor.Items[i].ToString().IndexOf(":"));*/

                int tmp = 0;

                _disableChangeFunc = true;

                for (int i = 0; i < _displays.Count; i++)
                {
                    if (monitor.Equals(_displays[i].displayName))
                    {
                        tmp = i;
                    }
                    else
                    {
                        _toolMonitor = _toolMonitors[i];
                        _toolMonitor.SelectedIndex = 0;
                    }
                }

                _disableChangeFunc = false;

                _toolMonitor = _toolMonitors[tmp];

                if (_toolMonitor.SelectedIndex != 0)
                {
                    for (int i = 0; i < _displays.Count; i++)
                    {
                        if (_displays[i].displayName.Equals(_toolMonitor.Items[0].ToString()
                                .Substring(0, _toolMonitor.Items[0].ToString().IndexOf(":", StringComparison.Ordinal))))
                        {
                            comboBoxMonitors.Text = i + 1 + @") " + _displays[i].displayName;

                            _numDisplay = i;
                            _currDisplay.numDisplay = _numDisplay;
                            _currDisplay.displayLink = _displays[i].displayLink;
                            _currDisplay.isExternal = _displays[i].isExternal;
                            break;
                        }
                    }

                    _currDisplay.displayName = _toolMonitor.Items[0].ToString()
                        .Substring(0, _toolMonitor.Items[0].ToString().IndexOf(":", StringComparison.Ordinal));

                    _currDisplay.rGamma = float.Parse(_iniFile.Read("rGamma", _toolMonitor.Text), _customCulture);
                    _currDisplay.gGamma = float.Parse(_iniFile.Read("gGamma", _toolMonitor.Text), _customCulture);
                    _currDisplay.bGamma = float.Parse(_iniFile.Read("bGamma", _toolMonitor.Text), _customCulture);
                    _currDisplay.rContrast = float.Parse(_iniFile.Read("rContrast", _toolMonitor.Text), _customCulture);
                    _currDisplay.gContrast = float.Parse(_iniFile.Read("gContrast", _toolMonitor.Text), _customCulture);
                    _currDisplay.bContrast = float.Parse(_iniFile.Read("bContrast", _toolMonitor.Text), _customCulture);
                    _currDisplay.rBright = float.Parse(_iniFile.Read("rBright", _toolMonitor.Text), _customCulture);
                    _currDisplay.gBright = float.Parse(_iniFile.Read("gBright", _toolMonitor.Text), _customCulture);
                    _currDisplay.bBright = float.Parse(_iniFile.Read("bBright", _toolMonitor.Text), _customCulture);
                    _currDisplay.monitorBrightness = int.Parse(_iniFile.Read("monitorBrightness", _toolMonitor.Text));
                    _currDisplay.monitorContrast = int.Parse(_iniFile.Read("monitorContrast", _toolMonitor.Text));

                    FillInfo(_currDisplay);
                    InitPresets();
                    buttonAllColors.PerformClick();

                    Gamma.SetGammaRamp(_currDisplay.displayLink,
                        Gamma.CreateGammaRamp(_currDisplay.rGamma, _currDisplay.gGamma, _currDisplay.bGamma,
                            _currDisplay.rContrast, _currDisplay.gContrast, _currDisplay.bContrast,
                            _currDisplay.rBright,
                            _currDisplay.gBright,
                            _currDisplay.bBright));

                    if (_currDisplay.isExternal)
                    {
                        trackBarMonitorBrightness.Value = _currDisplay.monitorBrightness;
                        trackBarMonitorContrast.Value = _currDisplay.monitorContrast;
                    }
                    else
                    {
                        trackBarMonitorBrightness.Value = _currDisplay.monitorBrightness;
                    }
                }
            }
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 1. 防止下拉框无选中项时触发错误
            if (comboBoxLanguage.SelectedIndex == -1)
                return;

            // 2. 判断选中的语言（0=EN→English，1=zh-CN→Chinese）
            LanguageType newLang = comboBoxLanguage.SelectedIndex == 0
                ? LanguageType.English
                : LanguageType.Chinese;

            // 3. 核心：切换语言并刷新窗体UI
            LanguageManager.SwitchLanguage(newLang, this);

            try
            {
                // 4. 保存语言设置到INI文件（捕获可能的异常，避免保存失败导致功能中断）
                _iniFile.SaveLanguage(newLang);
            }
            catch (Exception ex)
            {
                // 可选：提示保存失败（也可删除，不影响核心功能）
                MessageBox.Show($@"保存语言设置失败：{ex.Message}", @"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 5. 刷新托盘菜单文本（同步切换托盘菜单的中英文）
            InitTrayMenu();

            // 可选：移除设计器自动生成的异常抛出（关键！）
            // throw new System.NotImplementedException(); 
        }

        private void labelHotkeyTip_Click(object sender, EventArgs e)
        {
            // throw new System.NotImplementedException();
        }

        private void txtHotkey_TextChanged(object sender, EventArgs e)
        {
            // throw new System.NotImplementedException();
        }

        // 按键按下时捕获修饰符和按键
        private void TxtHotkey_KeyDown(object sender, KeyEventArgs e)
        {
            // 捕获修饰符（Ctrl/Alt/Shift）
            _currentHotkeyModifier = KeyModifiers.None;
            if (e.Control) _currentHotkeyModifier |= KeyModifiers.Ctrl;
            if (e.Alt) _currentHotkeyModifier |= KeyModifiers.Alt;
            if (e.Shift) _currentHotkeyModifier |= KeyModifiers.Shift;

            // 捕获普通按键（排除修饰符本身）
            if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey)
            {
                _currentHotkeyKey = e.KeyCode;
            }

            // 显示快捷键到文本框
            UpdateHotkeyDisplay();

            // 阻止系统默认行为（如Ctrl+C复制）
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        // 按键松开时重置（可选，仅保留最后一次按下的组合）
        private void TxtHotkey_KeyUp(object sender, KeyEventArgs e)
        {
            // 若仅松开修饰符，保持显示；若松开普通按键，最终确认显示
            if (e.KeyCode != Keys.ControlKey && e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey)
            {
                UpdateHotkeyDisplay();
            }
        }

        // 更新文本框显示的快捷键文本
        private void UpdateHotkeyDisplay()
        {
            if (_currentHotkeyKey == Keys.None && _currentHotkeyModifier == KeyModifiers.None)
            {
                txtHotkey.Text = ""; // 无按键时清空
                return;
            }

            // 拼接修饰符文本
            string modifierText = "";
            if (_currentHotkeyModifier.HasFlag(KeyModifiers.Ctrl)) modifierText += "Ctrl + ";
            if (_currentHotkeyModifier.HasFlag(KeyModifiers.Alt)) modifierText += "Alt + ";
            if (_currentHotkeyModifier.HasFlag(KeyModifiers.Shift)) modifierText += "Shift + ";

            // 拼接最终显示文本（如 "Ctrl + Alt + D1"）
            txtHotkey.Text = $@"{modifierText}{_currentHotkeyKey}".TrimEnd(' ', '+');
        }

        // 注册当前显示器所有预设的全局热键
        private void RegisterAllPresetHotkeys()
        {
            UnregisterAllPresetHotkeys();
            string[] presets = _iniFile.GetSections();
            if (presets == null) return;

            // 新增：统计注册成功/失败数
            int successCount = 0;
            int failCount = 0;

            foreach (string preset in presets)
            {
                if (!_iniFile.Read("monitor", preset).Equals(_currDisplay.displayName))
                    continue;

                string hotkeyStr = _iniFile.Read("Hotkey", preset);
                HotkeyConfig hotkeyConfig = HotkeyConfig.FromString(hotkeyStr);
                if (hotkeyConfig.Modifier == KeyModifiers.None && hotkeyConfig.Key == Keys.None)
                    continue;

                int hotkeyId = _nextHotkeyId++;
                _hotkeyIdMap[preset] = hotkeyId;

                // 注册热键并获取结果
                bool isRegistered = RegisterHotKey(Handle, hotkeyId, (int)hotkeyConfig.Modifier,
                    (int)hotkeyConfig.Key);

                if (isRegistered) successCount++;
                else failCount++;
            }

            // 关键：注册热键后刷新预设列表，确保数据源同步
            InitPresets();

            Debug.WriteLine($"热键注册完成：成功{successCount}个，失败{failCount}个");

            // 最终统计提示（测试用）
            // MessageBox.Show($"热键注册完成：成功{successCount}个，失败{failCount}个", "注册结果");
        }

        // 注销所有已注册的热键（配套方法）
        private void UnregisterAllPresetHotkeys()
        {
            // 遍历所有已注册的热键ID，逐个注销
            foreach (var kv in _hotkeyIdMap)
            {
                UnregisterHotKey(Handle, kv.Value);
            }

            // 清空映射表+重置ID自增器
            _hotkeyIdMap.Clear();
            _nextHotkeyId = 1000;
        }

        // 重写窗口消息处理方法，监听全局热键触发
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m); // 先执行系统默认的消息处理

            // 仅处理「全局热键触发」消息（WM_HOTKEY = 0x0312）
            if (m.Msg == 0x0312)
            {
                try
                {
                    int hotkeyId = m.WParam.ToInt32();
                    var kv = _hotkeyIdMap.FirstOrDefault(k => k.Value == hotkeyId);
                    string targetPreset = kv.Key;
                    if (string.IsNullOrEmpty(targetPreset)) return;

                    string purePresetName = GetPurePresetName(targetPreset);
                    SafeInvoke(() => // 强制UI线程选中
                    {
                        int presetIndex = comboBoxPresets.Items.IndexOf(purePresetName);
                        if (presetIndex != -1)
                        {
                            comboBoxPresets.SelectedIndex = presetIndex;
                            comboBoxPresets.Text = comboBoxPresets.Items[presetIndex].ToString(); // 强制设置文本
                            comboBoxPresets.Refresh(); // 强制UI刷新
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WndProc异常：{ex}");
                }
            }
        }

        // 新增工具方法：统一提取纯预设名（放在Window类中）
        private string GetPurePresetName(string fullPresetName)
        {
            if (string.IsNullOrEmpty(fullPresetName))
            {
                Debug.WriteLine("GetPurePresetName: 完整预设名为空");
                return "";
            }

            // 仅处理「显示器名: 预设名」格式（冒号+空格分隔），不修改原字符
            const string separator = ": ";
            if (fullPresetName.Contains(separator))
            {
                // 提取分隔符后的纯预设名（保留原始字符）
                string pureName = fullPresetName
                    .Substring(fullPresetName.IndexOf(separator, StringComparison.Ordinal) + separator.Length).Trim();
                // MessageBox.Show($"GetPurePresetName: 完整名[{fullPresetName}] → 纯预设名[{pureName}]", "错误",
                //     MessageBoxButtons.OK, MessageBoxIcon.Error);

                // MessageBox.Show($@"GetPurePresetName调用源头：{new StackTrace()}", @"错误",
                //     MessageBoxButtons.OK, MessageBoxIcon.Error);

                return pureName;
            }
            else
            {
                // 无分隔符时直接返回原始名称（去空格）
                string pureName = fullPresetName.Trim();
                // MessageBox.Show($@"GetPurePresetName: 完整名[{fullPresetName}] 无分隔符，返回[{pureName}]", @"错误",
                //     MessageBoxButtons.OK, MessageBoxIcon.Error);

                return pureName;
            }
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action); // 切换到UI线程执行
            }
            else
            {
                action();
            }
        }

        private void comboBoxPresets_TextChanged(object sender, EventArgs e)
        {
            // 如果是快捷键触发的选中，禁止修改Text
            if (_isHotkeyTriggered && !string.IsNullOrEmpty(comboBoxPresets.Text))
            {
                // 锁定文本为选中项的文本
                string selectedText = comboBoxPresets.Items[comboBoxPresets.SelectedIndex].ToString();
                if (comboBoxPresets.Text != selectedText)
                {
                    comboBoxPresets.Text = selectedText;
                }
            }
        }
    }
}