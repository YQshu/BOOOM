# Git 协作工作流（双人开发）

## 1. 基本原则

- `main` 永远保持可运行。
- 不直接 push 到 `main`，通过分支 + PR 合并。
- 小步提交，降低冲突与回滚成本。

## 2. 日常命令流程

### 开始任务前

1. `git checkout main`
2. `git pull --rebase origin main`
3. `git checkout -b feature/<module>-<task>`

### 开发中

1. `git status`
2. `git add <files>`
3. `git commit -m "feat(scope): message"`

### 推送与提审

1. `git push -u origin <branch>`
2. 创建 PR 到 `main`

## 3. 同步最新 main（推荐 rebase）

在功能分支执行：

1. `git fetch origin`
2. `git rebase origin/main`
3. 若有冲突，解决后 `git add .` + `git rebase --continue`
4. `git push --force-with-lease`（仅对自己分支）

## 4. 冲突处理优先级

1. 先保证功能正确
2. 再保证风格统一
3. 最后做小规模整理（不要顺便大重构）

## 5. Unity 特殊建议

- Scene/Prefab 冲突高发，改动前先沟通。
- 对共享场景的结构性修改，尽量拆成独立 PR。
- 如出现大冲突，优先保留功能正确版本，再进行手工对齐。

## 6. 发布前检查

- 拉取最新 main 后可正常运行
- 核心流程可走通（启动、交互、线索、结局）
- 无临时代码（调试开关、测试资源引用）
