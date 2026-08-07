# 更新日志

## 0.1.5

- 将 SpriteRenderer 和 UGUI Image 的自定义 Inspector 纳入独立包。
- 导入包后默认替换这两个组件的 Sprite 选择字段，并保留 Unity 原版面板布局。
- 增加 SpriteRenderer/Image 与 Unity 原版 Editor 的菜单切换选项。
- 增加 ScriptableObject 类型筛选，可直接浏览自定义 ScriptableObject 资产。
- 增加 ScriptableObject 类型筛选回归测试。
- 调整打开窗口时的文件夹定位：已有资源时定位到资源所在文件夹，空属性时恢复上次浏览位置。

## 0.1.4

- 修复 Prefab 扫描将每个子物体当作独立条目并导致列表卡顿的问题，现在每个 Prefab 只显示根对象。
- 增加 Prefab 根对象查询回归测试。

## 0.1.3

- 同步当前项目核心选择器能力。
- 增加资源索引缓存、完整目录开关、缩略图尺寸调整、可拖拽目录栏和搜索目录联动。
- 选择资源后保持窗口打开。
- 增加 EditMode 自动化测试。

## 0.1.2

- 新增面向后续 Agent 的编辑器接入提示文档。

## 0.1.1

- 修复 Unity 2022.3 不支持 `EditorStyles.toolbarLabel` 导致的编译错误。

## 0.1.0

- 新增独立的 Unity 资源文件夹选择器。
- 支持资源类型、文件夹、子文件夹和名称搜索。
- 支持点击资源立即选择，并保存最近使用的文件夹。
