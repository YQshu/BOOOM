# 使用 URP 内置效果实现回溯 CRT 风格

## 概述

由于 RetroTVFX 插件安装存在问题，我们使用 **URP 内置的后处理效果**来实现类似的复古电视效果。

优势：
- ✅ 无需额外安装插件
- ✅ URP 原生支持，性能更好
- ✅ 立即可用
- ✅ 效果同样出色

## 🎯 快速配置步骤

### 1. 创建 Global Volume

1. 在 Hierarchy 中右键 > `Create Empty`
2. 命名为 `Retro Effect Volume`
3. 添加组件：`Volume`（搜索 Volume）
4. 配置 Volume：
   ```
   ☑ Is Global
   Weight: 0 (初始值，运行时由脚本控制)
   Priority: 1
   Profile: 点击 "New" 创建新配置
   ```

### 2. 添加后处理效果

点击 Volume 的 Profile 下方的 **`Add Override`** 按钮，依次添加：

#### ① Chromatic Aberration（色差效果）
```
☑ Intensity: 0.4
```
**作用：** 模拟 CRT 显示器的 RGB 色彩分离

#### ② Vignette（暗角效果）
```
☑ Intensity: 0.35
☑ Smoothness: 0.3
☑ Rounded: true
Color: 黑色 (默认)
```
**作用：** 模拟 CRT 屏幕边缘变暗

#### ③ Film Grain（胶片颗粒）
```
☑ Type: Thin1
☑ Intensity: 0.4
☑ Response: 0.8
```
**作用：** 模拟 CRT 的噪点和扫描线效果

#### ④ Lens Distortion（镜头扭曲）
```
☑ Intensity: -0.15 (负值产生桶形畸变)
☑ X Multiplier: 1.0
☑ Y Multiplier: 1.0
```
**作用：** 模拟 CRT 屏幕的弧形表面

### 3. 配置控制脚本

1. 在 Hierarchy 中创建空物体，命名为 `Retro Effect Controller`
2. 添加组件：`RetroTVEffectController`
3. 配置参数：
   ```
   Retro TV Volume: 拖入刚创建的 "Retro Effect Volume"
   Fade In Duration: 0.5
   Fade Out Duration: 0.3
   Target Weight: 0.8
   ```

### 4. 确认相机配置

选中 Main Camera，在 **Camera 组件**中：
- 向下滚动找到 `Rendering` 部分
- 确保 `☑ Post Processing` 已勾选

## 🎨 效果参数调整指南

### 强度级别对比

#### 轻微效果（适合长时间观看）
```
Chromatic Aberration > Intensity: 0.2
Vignette > Intensity: 0.25
Film Grain > Intensity: 0.2
Lens Distortion > Intensity: -0.08
```

#### 中等效果（推荐，平衡视觉与可玩性）
```
Chromatic Aberration > Intensity: 0.4
Vignette > Intensity: 0.35
Film Grain > Intensity: 0.4
Lens Distortion > Intensity: -0.15
```

#### 强烈效果（适合短暂回溯场景）
```
Chromatic Aberration > Intensity: 0.6
Vignette > Intensity: 0.5
Film Grain > Intensity: 0.6
Lens Distortion > Intensity: -0.25
```

### 单独调整建议

**如果觉得色差太强：**
- 降低 `Chromatic Aberration > Intensity` 到 0.2-0.3

**如果觉得边缘太暗：**
- 降低 `Vignette > Intensity` 到 0.2-0.25
- 增加 `Vignette > Smoothness` 到 0.4-0.5

**如果觉得噪点太多：**
- 降低 `Film Grain > Intensity` 到 0.2-0.3
- 或改变 `Film Grain > Type` 为 `Thin2` 或 `Medium1`

**如果觉得画面扭曲太明显：**
- 将 `Lens Distortion > Intensity` 调整到 -0.05 到 -0.1

## 🧪 测试步骤

### 方法 1：使用脚本测试功能

1. 运行游戏
2. 在 Inspector 中选中 `Retro Effect Controller`
3. 右键点击 `RetroTVEffectController` 组件
4. 选择 **"测试：启用效果"**
5. 观察效果是否出现
6. 选择 **"测试：禁用效果"**
7. 观察效果是否平滑淡出

### 方法 2：手动调整 Volume Weight

1. 运行游戏
2. 在 Inspector 中选中 `Retro Effect Volume`
3. 手动调整 `Weight` 从 0 到 1
4. 实时观察效果变化

### 方法 3：测试回溯集成

1. 运行游戏
2. 打开线索墙（Tab 键）
3. 选择嫌疑人进入回溯
4. 效果应该自动淡入
5. 按 ESC 退出回溯
6. 效果应该自动淡出

## 📊 效果对比

### URP 内置效果 vs RetroTVFX

| 特性 | URP 内置 | RetroTVFX |
|------|----------|-----------|
| 安装难度 | ✅ 无需安装 | ⚠️ 需要 Git |
| 性能 | ✅ 优秀 | ✅ 良好 |
| 扫描线 | ⚠️ 通过 Film Grain 模拟 | ✅ 原生支持 |
| 色差 | ✅ 原生支持 | ✅ 原生支持 |
| 噪点 | ✅ Film Grain | ✅ 原生支持 |
| 画面扭曲 | ✅ Lens Distortion | ✅ 原生支持 |
| 自定义性 | ⚠️ 有限 | ✅ 更多选项 |
| 稳定性 | ✅ 官方支持 | ⚠️ 第三方插件 |

**结论：** URP 内置效果完全够用，效果也很好！

## 🎬 进阶技巧

### 添加颜色调整（可选）

如果想要更强的赛博朋克风格，可以额外添加：

#### Color Adjustments
```
☑ Post Exposure: 0 到 0.2 (稍微提亮)
☑ Contrast: 10 到 20 (增加对比度)
☑ Saturation: -10 到 -20 (降低饱和度，更复古)
```

#### Tonemapping
```
Mode: ACES
```

### 动态调整效果强度（高级）

如果想根据不同嫌疑人使用不同强度的效果，可以修改 `RetroTVEffectController.cs`：

```csharp
// 添加公开方法
public void SetEffectIntensity(float intensity)
{
    _targetWeight = Mathf.Clamp01(intensity);
}

// 在 RetrospectManager 中调用
// retroTVController.SetEffectIntensity(0.5f); // 50% 强度
```

## ✅ 完成检查清单

配置完成后，确认：
- [ ] Volume 已创建并设置为 Global
- [ ] Volume Weight 初始值为 0
- [ ] 已添加 4 个后处理效果（Chromatic Aberration, Vignette, Film Grain, Lens Distortion）
- [ ] RetroTVEffectController 已添加并配置
- [ ] Main Camera 的 Post Processing 已启用
- [ ] 测试功能正常（右键菜单测试）
- [ ] 进入回溯时效果自动淡入
- [ ] 退出回溯时效果自动淡出
- [ ] 效果强度适中，不影响游戏可玩性

## 🎯 推荐的最终配置

基于赛博朋克回溯场景，推荐使用：

```yaml
Volume:
  Is Global: true
  Weight: 0 (由脚本控制)
  Priority: 1

Chromatic Aberration:
  Intensity: 0.35

Vignette:
  Intensity: 0.3
  Smoothness: 0.35
  Rounded: true

Film Grain:
  Type: Thin1
  Intensity: 0.35
  Response: 0.8

Lens Distortion:
  Intensity: -0.12

RetroTVEffectController:
  Fade In Duration: 0.5
  Fade Out Duration: 0.3
  Target Weight: 0.75
```

这个配置在视觉效果和可玩性之间取得了良好平衡。

---

**配置完成后，记得保存场景！**
