# RetroTVFX 插件安装问题诊断

## 🔴 问题确认

插件虽然在 `Packages/manifest.json` 中被引用，但**实际并未下载到本地**。

检查结果：
- ✅ manifest.json 中有引用：`"com.glairedaggers.retrotvfx": "https://github.com/GlaireDaggers/RetroTVFX.git"`
- ❌ Library/PackageCache 中找不到插件文件
- ❌ 无法使用 RetroTV 相关组件

## 🔧 解决方案

### 方案 1：在 Unity 编辑器中重新导入（推荐）

1. **打开 Unity 编辑器**
2. **打开 Package Manager**
   - 菜单：`Window > Package Manager`
3. **检查插件状态**
   - 左上角下拉菜单选择 `Packages: In Project`
   - 查找 `RetroTVFX` 或 `com.glairedaggers.retrotvfx`
4. **查看错误信息**
   - 如果显示错误图标，点击查看详细错误
   - 常见错误：
     - Git 未安装或未配置
     - 网络连接问题
     - GitHub 访问受限

### 方案 2：检查 Git 配置

RetroTVFX 是从 GitHub 安装的，需要 Git 支持。

**检查 Git 是否安装：**
1. 打开命令行（CMD 或 PowerShell）
2. 运行：`git --version`
3. 如果显示版本号 → Git 已安装
4. 如果提示找不到命令 → 需要安装 Git

**安装 Git：**
- 下载：https://git-scm.com/download/win
- 安装后重启 Unity 编辑器

### 方案 3：手动下载插件

如果 Git 方式不行，可以手动下载：

1. **下载插件**
   - 访问：https://github.com/GlaireDaggers/RetroTVFX
   - 点击绿色 `Code` 按钮 > `Download ZIP`
   - 解压到临时文件夹

2. **复制到项目**
   - 将解压后的文件夹复制到：`Assets/Plugins/RetroTVFX`
   - Unity 会自动识别

3. **修改 manifest.json**
   - 删除这一行：`"com.glairedaggers.retrotvfx": "https://github.com/GlaireDaggers/RetroTVFX.git",`
   - 因为已经手动放入 Assets 文件夹

### 方案 4：使用替代的 CRT 效果插件

如果 RetroTVFX 无法安装，可以使用 URP 内置的后处理效果组合：

**使用 URP 内置效果模拟 CRT：**
1. Chromatic Aberration（色差）
2. Vignette（暗角）
3. Film Grain（胶片颗粒，模拟噪点）
4. Lens Distortion（镜头扭曲）

这些效果组合起来也能达到类似的复古电视效果。

## 🧪 验证插件是否安装成功

### 方法 1：检查 Package Manager
- `Window > Package Manager`
- 左上角选择 `Packages: In Project`
- 应该能看到 `RetroTVFX` 且没有错误图标

### 方法 2：检查文件系统
- 查看 `Library/PackageCache` 文件夹
- 应该有 `com.glairedaggers.retrotvfx@xxxxx` 文件夹

### 方法 3：测试组件
- 创建 Volume
- 点击 `Add Override`
- 搜索 `Retro` 或 `CRT`
- 应该能找到相关效果

## 📝 当前推荐操作步骤

### 立即可行的方案：使用 URP 内置效果

由于 RetroTVFX 安装存在问题，我建议先使用 URP 内置效果：

1. **创建 Global Volume**（如果还没创建）
   ```
   - 场景中创建空物体 "Retro Effect Volume"
   - 添加 Volume 组件
   - Is Global = ✓
   - Weight = 0
   - Priority = 1
   ```

2. **添加内置后处理效果**
   - 创建新 Profile
   - 添加以下 Override：
     - ✅ Chromatic Aberration（色差）
       - Intensity: 0.3-0.5
     - ✅ Vignette（暗角）
       - Intensity: 0.3-0.4
       - Smoothness: 0.3
     - ✅ Film Grain（胶片颗粒）
       - Intensity: 0.3-0.5
       - Response: 0.8
     - ✅ Lens Distortion（镜头扭曲）
       - Intensity: -0.1 到 -0.2（负值产生桶形畸变）

3. **使用现有的 RetroTVEffectController**
   - 脚本已经写好，可以直接使用
   - 只需将 Volume 拖入 `Retro TV Volume` 字段
   - 效果会在进入回溯时自动淡入

## 🎨 URP 内置效果参数推荐

### 赛博朋克回溯风格
```yaml
Chromatic Aberration:
  Intensity: 0.4

Vignette:
  Intensity: 0.35
  Smoothness: 0.3
  Rounded: true

Film Grain:
  Type: Thin1
  Intensity: 0.4
  Response: 0.8

Lens Distortion:
  Intensity: -0.15
  X Multiplier: 1.0
  Y Multiplier: 1.0
  Center: (0.5, 0.5)
  Scale: 1.0
```

### 轻微怀旧风格
```yaml
Chromatic Aberration:
  Intensity: 0.2

Vignette:
  Intensity: 0.25
  Smoothness: 0.4

Film Grain:
  Type: Thin1
  Intensity: 0.2
  Response: 0.8

Lens Distortion:
  Intensity: -0.08
```

## ✅ 下一步行动

1. **先使用 URP 内置效果**
   - 立即可用，无需额外安装
   - 效果也很好，符合赛博朋克风格
   - 性能更好（URP 原生支持）

2. **稍后解决 RetroTVFX 安装问题**
   - 检查 Git 是否安装
   - 查看 Unity Console 中的错误信息
   - 或使用手动下载方式

3. **测试效果**
   - 使用 RetroTVEffectController 的测试功能
   - 调整参数直到满意

---

**需要我帮你创建使用 URP 内置效果的配置吗？**
