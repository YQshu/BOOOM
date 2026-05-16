# RetroTVFX 配置检查清单

## 📋 配置前检查

### Main Camera 配置（Unity 2022.3）
- [ ] Main Camera 上有 `Camera` 组件
- [ ] Main Camera 上有 `Cinemachine Brain` 组件（如果使用 Virtual Camera）
- [ ] Main Camera 上有 `Universal Additional Camera Data` 组件（自动添加，无 Inspector 面板）
- [ ] **在 Camera 组件中**向下滚动找到 `Rendering` 部分
- [ ] `Camera > Rendering > Post Processing` = ✓ 已勾选

**重要提示：** 在 Unity 2022.3 中，Post Processing 选项在 **Camera 组件**里，不是在 Universal Additional Camera Data 里！需要向下滚动 Camera 组件的 Inspector 才能看到。

### Virtual Camera 配置（如果使用）
- [ ] Virtual Camera 配置了跟随目标（Follow）
- [ ] Virtual Camera 配置了观察目标（Look At）
- [ ] **不要**在 Virtual Camera 上添加 Volume 或后处理组件

### URP 渲染管线
- [ ] `Edit > Project Settings > Graphics` 中使用了 URP Asset
- [ ] URP Asset 中启用了 Post Processing

## 📦 RetroTVFX 安装步骤

### 1. 创建 Global Volume
- [ ] 场景中创建空物体，命名为 `RetroTV Volume`
- [ ] 添加 `Volume` 组件（UnityEngine.Rendering）
- [ ] 设置 `Is Global` = ✓
- [ ] 设置 `Weight` = 0（初始值）
- [ ] 设置 `Priority` = 1

### 2. 配置 Volume Profile
- [ ] 点击 `Profile` 的 `New` 按钮创建新配置
- [ ] 点击 `Add Override` 添加 RetroTV 效果
- [ ] 调整效果参数（扫描线、噪点、扭曲等）

### 3. 添加控制脚本
- [ ] 场景中创建空物体，命名为 `RetroTV Controller`
- [ ] 添加 `RetroTVEffectController` 组件
- [ ] 将 `RetroTV Volume` 物体拖入 `Retro TV Volume` 字段
- [ ] 设置 `Fade In Duration` = 0.5
- [ ] 设置 `Fade Out Duration` = 0.3
- [ ] 设置 `Target Weight` = 0.7-0.8

## 🧪 测试步骤

### 手动测试
1. [ ] 运行游戏
2. [ ] 在 Inspector 中右键点击 `RetroTVEffectController`
3. [ ] 选择 "测试：启用效果"
4. [ ] 观察屏幕是否出现 CRT 效果
5. [ ] 选择 "测试：禁用效果"
6. [ ] 观察效果是否平滑淡出

### 回溯系统集成测试
1. [ ] 运行游戏
2. [ ] 打开线索墙（按 Tab 键）
3. [ ] 选择一个嫌疑人进入回溯
4. [ ] 观察 RetroTV 效果是否自动淡入
5. [ ] 按 ESC 退出回溯
6. [ ] 观察 RetroTV 效果是否自动淡出

## ❌ 常见问题

### 问题：效果完全不显示
**可能原因：**
- Main Camera 未启用 Post Processing
- Volume Profile 未正确配置
- RetroTVFX 插件未正确安装

**解决方案：**
1. 选中 Main Camera，在 **Camera 组件**中向下滚动
2. 找到 `Rendering` 部分，确认 `Post Processing` 已勾选 ✓
3. 在 Volume Profile 中确认已添加 RetroTV Override
4. 在 Package Manager 中确认 `com.glairedaggers.retrotvfx` 已安装

**注意：** Unity 2022.3 中，Post Processing 选项在 Camera 组件里，不是在 Universal Additional Camera Data 里！

### 问题：效果在编辑器中看不到，但运行时可以
**原因：**
- Game 视图未启用后处理预览

**解决方案：**
- 点击 Game 视图右上角的设置图标
- 确保 `Post Processing` 选项已勾选

### 问题：效果影响了 UI
**原因：**
- Global Volume 会影响整个屏幕，包括 UI

**解决方案：**
- 如果只想影响游戏画面：需要使用 Render Texture 方案（较复杂）
- 或者设计 UI 时考虑 CRT 效果的存在（推荐）

### 问题：Virtual Camera 切换时效果消失
**原因：**
- 可能在 Virtual Camera 上错误配置了 Volume

**解决方案：**
- 确保 Volume 是 Global 的，不要绑定到特定相机
- 不要在 Virtual Camera 上添加任何后处理组件

## 🎨 推荐参数（赛博朋克风格）

```
Volume Profile 参数：
├─ Scanline Intensity: 0.3-0.5
├─ Noise Intensity: 0.1-0.2
├─ Distortion: 0.05-0.1
├─ Vignette: 0.3-0.4
├─ Color Bleeding: 0.2-0.3
└─ Chromatic Aberration: 0.1-0.15

RetroTVEffectController 参数：
├─ Fade In Duration: 0.5s
├─ Fade Out Duration: 0.3s
└─ Target Weight: 0.7-0.8
```

## 📝 完成确认

配置完成后，确认以下功能正常：
- [ ] 进入回溯时，CRT 效果平滑淡入
- [ ] 退出回溯时，CRT 效果平滑淡出
- [ ] 效果强度适中，不影响游戏可玩性
- [ ] 性能稳定，帧率无明显下降
- [ ] Virtual Camera 切换时效果保持稳定

---

**配置完成日期：** ___________

**测试人员：** ___________

**备注：** ___________
