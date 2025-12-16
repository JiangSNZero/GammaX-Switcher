# GammaX-Switcher

## 中文版

### 伽马值切换管理器

无需安装，直接运行 .exe 文件即可使用。程序会在 .exe 同目录下自动生成唯一的 .ini 配置文件，用于保存自定义参数预设。

> 注意：程序运行后会覆盖当前系统所有显示器的伽马值设置。

### 功能介绍

#### 一、基础功能（原生功能）

<img width="585" height="239" alt="image" src="https://github.com/user-attachments/assets/8f85f6a1-2c55-4c46-a9a5-25a80be51ae7" />


1. **伽马值调节**

   - 颜色按钮支持独立调节 RGB 通道伽马值，或同步调节所有通道。

   - 勾选「+++」复选框可解锁极端对比度数值。

   - 操作方式：拖动滑块实现大幅调节；选中滑块后，通过键盘方向键 / 鼠标点击滑块两侧实现微调。

   - 数值范围：

     ```plaintext
     伽马值：0.30 — 4.40（默认 1.00）｜CreateGammaRamp 函数不支持超出该范围的数值
     亮度：-1.00 — 1.00（默认 0.00）｜超出范围无效果
     对比度：0.10 — 3.00/100.00（默认 1.00）｜0.10 为安全下限，3.00 为实用上限，100.00 为娱乐性上限
     ```

     

2. **校准辅助**

   内置小型校准图片，便于调节时参考色彩还原度。

3. **显示器切换**

   左侧下拉列表显示所有已连接显示器，支持手动选择、鼠标滚轮滚动，或点击「Forward」按钮切换。

4. **预设管理**

   中间下拉列表为参数预设库，支持手动选择 / 鼠标滚轮切换：

   - 保存预设：在空列表中输入自定义名称，点击「Save」即可将当前参数写入 .ini 文件；
   - 删除预设：选中目标预设，点击「Delete」即可从 .ini 文件中移除。

5. **硬件亮度 / 对比度调节**

   独立滑块调节显示器硬件亮度 / 对比度（需显示器支持），范围 0-100，操作逻辑与伽马值滑块一致。

6. **快捷操作**

   - 「Reset」：重置所有参数至默认值（不会恢复调节前的设置）；
   - 「Hide」：将程序最小化至系统托盘；
   - 恢复窗口：双击托盘图标，或右键托盘图标 →「设置」；
   - 退出程序：点击窗口右上角红色「×」，或右键托盘图标 →「退出」。

7. **托盘功能**

   托盘菜单中会显示所有显示器的预设下拉列表，便于快速切换参数。

#### 二、新增功能

1. **中英双语切换**
   - 程序支持简体中文 / 英文双语言界面，切换后即时生效；
   - 切换方式：在设置界面找到「语言」下拉选项，选择对应语言即可。
2. **全局快捷键切换**
   - 支持自定义全局快捷键，快速切换预设参数 / 显示器 / 语言；
   - 快捷键配置：在「快捷键」设置面板中绑定按键组合，支持 Ctrl/Shift/Alt 修饰键 + 字母 / 数字 / 功能键；
   - *常用快捷键示例（可自定义，功能待完善）*：
     - 切换语言：Ctrl + Shift + L
     - 切换下一个预设：Ctrl + ↑
     - 切换下一个显示器：Ctrl + →
     - 重置参数：Ctrl + R
     - 隐藏窗口：Ctrl + H

### 使用说明

1. 首次运行自动生成 .ini 配置文件，删除该文件将恢复所有预设为默认状态；
2. 全局快捷键生效期间，需保证程序在后台运行（托盘状态亦可）；
3. 语言切换后，所有菜单、按钮、提示文本将同步更新为所选语言；
4. 若显示器不支持硬件亮度 / 对比度调节，对应滑块将置灰不可操作。

### 版本信息

- 基于 Gamma Manager 原版修改，新增中英双语、全局快捷键功能
- 适配系统：Windows (x86/x64)，依赖 .NET Framework 4.7.2
- 许可证：CC0 1.0 Public Domain（公有领域）

## English Version

### GammaX Switcher

No installation required, just run the .exe file directly. The program automatically generates a unique .ini configuration file in the same directory as the .exe to save custom parameter presets.

> Note: Running the program will override the gamma settings of all monitors in the current system.

### Feature Introduction

#### 1. Basic Features (Native)

<img width="588" height="243" alt="image" src="https://github.com/user-attachments/assets/488c565a-4bba-4351-bcf9-c9e4b17d94a8" />

1. **Gamma Adjustment**

   - Color buttons support independent adjustment of RGB channel gamma values, or synchronous adjustment of all channels.

   - Check the "+++" checkbox to unlock extreme contrast values.

   - Operation methods: Drag the slider for large adjustments; after selecting the slider, use the keyboard arrow keys/mouse clicks on both sides of the slider for fine adjustments.

   - Value range:


     ```plaintext
     Gamma: 0.30 — 4.40 (default 1.00) | The CreateGammaRamp function does not support values outside this range
     Brightness: -1.00 — 1.00 (default 0.00) | Values outside the range have no effect
     Contrast: 0.10 — 3.00/100.00 (default 1.00) | 0.10 is the safety lower limit, 3.00 is the practical upper limit, 100.00 is the entertainment upper limit
     ```

     

2. **Calibration Assistance**

   A built-in small calibration image is provided for reference of color reproduction during adjustment.

3. **Monitor Switching**

   The drop-down list on the left shows all connected monitors, supporting manual selection, mouse wheel scrolling, or switching via the "Forward" button.

4. **Preset Management**

   The drop-down list in the center is the parameter preset library, supporting manual selection/mouse wheel switching:

   - Save preset: Enter a custom name in the empty list and click "Save" to write the current parameters to the .ini file;
   - Delete preset: Select the target preset and click "Delete" to remove it from the .ini file.

5. **Hardware Brightness & Contrast Adjustment**

   Independent sliders to adjust monitor hardware brightness/contrast (if supported by the monitor), range 0-100, with the same operation logic as the gamma slider.

6. **Quick Operations**

   - "Reset": Reset all parameters to default values (will not restore pre-adjustment settings);
   - "Hide": Minimize the program to the system tray;
   - Restore window: Double-click the tray icon, or right-click the tray icon → "Settings";
   - Exit program: Click the red "×" in the upper right corner of the window, or right-click the tray icon → "Exit".

7. **Tray Function**

   The tray menu displays a drop-down list of presets for all monitors, facilitating quick parameter switching.

   #### 2. New Features

   1. **Chinese/English Language Switching**
      - The program supports Simplified Chinese/English bilingual interface, which takes effect immediately after switching;
      - Switching method: Find the "Language" drop-down option in the settings interface and select the corresponding language.
   2. **Global Hotkey Switching**
      - Support custom global hotkeys to quickly switch preset parameters/monitors/languages;
      - Hotkey configuration: Bind key combinations in the "Hotkey" settings panel, supporting Ctrl/Shift/Alt modifier keys + letters/numbers/function keys;
      - *Examples of common hotkeys (customizable, Features pending improvement)*:
        - Switch language: Ctrl + Shift + L
        - Switch to next preset: Ctrl + ↑
        - Switch to next monitor: Ctrl + →
        - Reset parameters: Ctrl + R
        - Hide window: Ctrl + H

   ### Usage Notes

   1. The .ini configuration file is automatically generated on first run; deleting this file will restore all presets to default status;
   2. For global hotkeys to take effect, the program must run in the background (tray status is also acceptable);
   3. After switching the language, all menus, buttons, and prompt texts will be updated to the selected language synchronously;
   4. If the monitor does not support hardware brightness/contrast adjustment, the corresponding sliders will be grayed out and unavailable.

   ### Version Information

   - Modified based on the original Gamma Manager, adding Chinese/English bilingual and global hotkey functions
   - Compatible systems: Windows (x86/x64), dependent on .NET Framework 4.7.2
   - License: CC0 1.0 Public Domain
