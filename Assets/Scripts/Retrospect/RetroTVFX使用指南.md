# RetroTVFX 使用指南

## 概述
RetroTVFX 是一个复古 CRT 电视效果插件，用于在回溯模式中营造赛博朋克氛围。

## ⚠️ Cinemachine Virtual Camera 兼容性说明

**完全兼容！** 使用 Virtual Camera 不影响 RetroTVFX 的使用。

### 工作原理
- **Virtual Camera** 只控制相机的位置、旋转、跟随等参数
- **Main Camera** 才是真正执行渲染的相机
- **后处理效果**（包括 RetroTVFX）作用在 Main Camera 上
- Cinemachine Brain 会将 Virtual Camera 的参数应用到 Main Camera

### 关键配置点
✅ **Main Camera** 需要：
- 挂载 `Cinemachine Brain` 组件
- 挂载 `Universal Additional Camera Data` 组件（URP 自动添加）
- 在 `Universal Additional Camera Data` 中启用 `Post Processing = true`

❌ **Virtual Camera** 不需要：
- 不需要添加任何后处理组件
- 不需要添加 Volume 组件

## 安装步骤

### 1. 确认插件已安装
插件已通过 Package Manager 从 GitHub 导入：
- 包名：`com.glairedaggers.retrotvfx`
- 来源：https://github.com/GlaireDaggers/RetroTVFX.git

### 2. 配置 URP 渲染管线

#### 方法 A：使用 Volume 组件（推荐）

1. **创建 Global Volume**
   - 在场景中创建空物体，命名为 `RetroTV Volume`
   - 添加组件：`Volume`（UnityEngine.Rendering）
   - 设置 `Is Global` = true
   - 设置 `Weight` = 0（初始禁用）
   - 设置 `Priority` = 1（确保优先级高于其他 Volume）

2. **添加 RetroTV Profile**
   - 在 Volume 组件中，点击 `Profile` 的 `New` 按钮创建新配置
   - 点击 `Add Override` 按钮
   - 搜索并添加 RetroTV 相关的效果（可能叫 `RetroTV Effect` 或 `CRT Effect`）

3. **配置效果参数**（常见参数）
   - **Scanline Intensity**：扫描线强度（0-1）
   - **Noise Intensity**：噪点强度（0-1）
   - **Distortion**：画面扭曲程度
   - **Vignette**：暗角效果
   - **Color Bleeding**：颜色溢出（模拟 CRT 色彩）
   - **Chromatic Aberration**：色差效果

#### 方法 B：使用 Renderer Feature

1. **打开 URP Renderer Asset**
   - 路径：`Assets/Settings/UniversalRenderPipelineAsset_Renderer`
   - 或在 Project Settings > Graphics > Scriptable Render Pipeline Settings 中找到

2. **添加 Renderer Feature**
   - 在 Inspector 中找到 `Renderer Features` 列表
   - 点击 `Add Renderer Feature`
   - 选择 `RetroTV Renderer Feature`（如果有的话）

### 3. 设置控制脚本

1. **添加 RetroTVEffectController 到场景**
   - 在场景中创建空物体，命名为 `RetroTV Controller`
   - 添加组件：`RetroTVEffectController`

2. **配置 Inspector 参数**
   ```
   效果设置：
   - Retro TV Volume: 拖入刚创建的 Volume 物体
   - Fade In Duration: 0.5（淡入时长，秒）
   - Fade Out Duration: 0.3（淡出时长，秒）

   效果强度：
   - Target Weight: 0.8（目标强度，0-1）
   ```

### 4. 检查 Main Camera 配置（重要！）

**Unity 2022.3 重要提示：** Post Processing 选项在 **Camera 组件**里，不是在 Universal Additional Camera Data 里！

**如果你使用 Cinemachine Virtual Camera：**

1. **选中 Main Camera**，确认以下组件存在：
   - ✅ `Camera` 组件
   - ✅ `Cinemachine Brain` 组件
   - ✅ `Universal Additional Camera Data` 组件（自动添加，无 Inspector 面板）

2. **在 Camera 组件中**（向下滚动）：
   - 找到 `Rendering` 部分（可能需要展开 ▼）
   - 确保 `Post Processing` = **✓ 勾选**
   - `Anti-aliasing` 可选（推荐 FXAA 或 SMAA）

3. **Virtual Camera 不需要任何配置**
   - 保持原有的跟随、构图等设置即可
   - 不要在 Virtual Camera 上添加 Volume 或后处理组件

**如果不使用 Virtual Camera：**
- 直接在 Main Camera 的 Camera 组件中确认 `Post Processing` 已启用即可

**找不到 Post Processing 选项？**
- 查看详细指南：`Unity2022_URP相机配置指南.md`

## 使用方式

### 自动触发
脚本已自动订阅 `RetrospectManager` 的事件：
- 进入回溯时：自动淡入 RetroTV 效果
- 退出回溯时：自动淡出 RetroTV 效果

### 手动控制
在代码中调用：
```csharp
// 获取控制器
RetroTVEffectController controller = FindObjectOfType<RetroTVEffectController>();

// 启用效果（带淡入动画）
controller.EnableEffect();

// 禁用效果（带淡出动画）
controller.DisableEffect();

// 立即设置（无动画）
controller.SetEffectImmediate(true);  // 启用
controller.SetEffectImmediate(false); // 禁用
```

### 测试功能
在 Inspector 中右键点击 `RetroTVEffectController` 组件：
- **测试：启用效果** - 立即测试淡入效果
- **测试：禁用效果** - 立即测试淡出效果

## 推荐配置

### 赛博朋克回溯风格
```
Scanline Intensity: 0.3-0.5
Noise Intensity: 0.1-0.2
Distortion: 0.05-0.1
Vignette: 0.3-0.4
Color Bleeding: 0.2-0.3
Chromatic Aberration: 0.1-0.15
```

### 轻微怀旧风格
```
Scanline Intensity: 0.2
Noise Intensity: 0.05
Distortion: 0.02
Vignette: 0.2
Color Bleeding: 0.1
Chromatic Aberration: 0.05
```

### 强烈故障风格
```
Scanline Intensity: 0.6-0.8
Noise Intensity: 0.3-0.5
Distortion: 0.2-0.3
Vignette: 0.5
Color Bleeding: 0.4
Chromatic Aberration: 0.2-0.3
```

## 性能优化

1. **仅在需要时启用**
   - 脚本已实现按需启用/禁用，不会持续消耗性能

2. **调整分辨率**
   - 如果性能不足，可以在 Volume Profile 中降低效果的采样分辨率

3. **移动平台优化**
   - 降低 Noise Intensity 和 Distortion
   - 禁用 Chromatic Aberration（性能消耗较大）

## 故障排查

### 效果不显示
1. **检查 Main Camera 的后处理设置（Unity 2022.3）**
   - 选中 Main Camera
   - 在 **Camera 组件**中向下滚动（不是 Universal Additional Camera Data）
   - 找到 `Rendering` 部分
   - 确认 `Post Processing` 已勾选 ✓

2. **检查 Volume 配置**
   - Volume 的 `Weight` 是否大于 0（运行时通过脚本控制）
   - Volume 的 `Is Global` 已勾选
   - Volume Profile 中已添加 RetroTV 效果

3. **检查 URP 设置**
   - 打开 `Edit > Project Settings > Graphics`
   - 确认使用了 URP 渲染管线
   - 打开 URP Asset，确认启用了 Post Processing

4. **Cinemachine 用户特别注意**
   - 确认 Main Camera 上有 `Cinemachine Brain` 组件
   - 不要在 Virtual Camera 上添加后处理相关组件
   - 后处理只作用在 Main Camera 上

5. **如果实在找不到 Post Processing 选项**
   - 查看详细指南：`Unity2022_URP相机配置指南.md`
   - 或使用代码方式启用（见指南中的临时解决方案）

### 效果太强/太弱
- 调整 `RetroTVEffectController` 的 `Target Weight` 参数（0-1）
- 或直接调整 Volume Profile 中各个效果的强度

### 淡入淡出不流畅
- 增加 `Fade In Duration` 和 `Fade Out Duration`
- 检查帧率是否稳定

## 扩展功能

### 动态调整效果强度
可以根据游戏状态动态调整：
```csharp
// 在 RetroTVEffectController 中添加公开方法
public void SetTargetWeight(float weight)
{
    _targetWeight = Mathf.Clamp01(weight);
}
```

### 添加音效配合
在 `EnableEffect()` 和 `DisableEffect()` 中添加音效播放：
```csharp
public void EnableEffect()
{
    // ... 原有代码 ...

    // 播放电视开启音效
    AudioManager.Instance?.PlaySFX("TV_On");
}
```

## 相关文件
- 控制脚本：`Assets/Scripts/Retrospect/RetroTVEffectController.cs`
- 回溯管理器：`Assets/Scripts/Retrospect/RetrospectManager.cs`
- 插件包：`Packages/com.glairedaggers.retrotvfx`

## 注意事项
1. RetroTV 效果会影响整个屏幕，包括 UI
2. 如果只想影响游戏画面不影响 UI，需要使用 Render Texture 方案
3. 在编辑器中测试时，确保 Game 视图已启用后处理预览
