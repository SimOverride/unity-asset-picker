# Unity Asset Picker

一个独立的 Unity Editor UPM 工具，为资源字段提供按文件夹浏览和筛选能力，适合 Sprite、材质、纹理、Prefab 等项目资源较多的场景。

包名：`com.simoverride.asset-picker`

命名空间：`AssetPicker.Editor`

## 功能概览

- 在窗口内按资源类型筛选，默认类型为 Sprite；
- 通过类似 Unity Project 窗口的层级树浏览 `Assets` 文件夹；
- 默认隐藏不包含目标资源的文件夹，也可以切换为显示全部文件夹；
- 支持当前文件夹或当前文件夹及其子文件夹筛选；
- 搜索资源名称和路径，搜索条件会同步影响文件夹树；
- 使用网格布局显示资源，支持小、中、大三档缩略图；
- 支持拖拽调整文件夹面板宽度；
- 点击资源即可完成选择，窗口不会自动关闭；
- 不显示额外的“选择”和“取消”按钮，行为与 Unity 原版资源选择器一致；
- 支持在窗口内筛选自定义 ScriptableObject 资产；
- 按资源类型保存最近浏览的文件夹、缩略图大小和目录栏宽度；
- 对资源索引进行缓存，在项目资源变化或手动刷新时自动清理；
- 筛选 Prefab 时每个 Prefab 只显示根 GameObject，不展开子物体条目；
- 只读取 `Assets` 下的资源，不创建索引文件，也不修改资源内容。

## 兼容性

- Unity 2022.3 及以上版本；
- 仅在 Unity Editor 中运行，不包含运行时代码依赖；
- 不依赖 React、Node.js、浏览器或其他项目级 UI 框架；
- 导入后默认接管 SpriteRenderer 和 UGUI Image 的自定义 Inspector；
- 不会接管第三方组件或其他未列出的 Unity 内置组件 Inspector。

## 安装

### 使用 UPM tarball

在 Unity 中打开 `Window > Package Manager`，点击左上角的 `+`，选择 `Add package from tarball...`，然后选择发布包中的 `.tgz` 文件。

### 使用 Git URL

仓库发布后，可以在 Package Manager 中选择 `Add package from git URL...`，或在项目的 `Packages/manifest.json` 中加入：

```json
{
  "dependencies": {
    "com.simoverride.asset-picker": "https://github.com/<owner>/<repository>.git"
  }
}
```

如果包位于 Git 仓库的子目录，需要在 URL 后追加 UPM 路径参数：

```text
https://github.com/<owner>/<repository>.git?path=/asset-picker
```

### 使用本地目录

开发或调试时，可以在 `manifest.json` 中使用本地路径：

```json
{
  "dependencies": {
    "com.simoverride.asset-picker": "file:../../asset-picker"
  }
}
```

请根据 Unity 项目与 `asset-picker` 目录的实际相对位置调整路径。

### 使用 Unity Package

如果发布包同时提供 `.unitypackage`，可以通过 `Assets > Import Package > Custom Package...` 导入。UPM tarball 或 Git URL 更适合长期维护和版本升级。

## 快速开始

### 从菜单打开

导入包后，使用 Unity 菜单：

`Tools > 项目资源选择器 > 打开选择窗口`

窗口默认筛选 Sprite。资源类型可以直接在窗口内部切换，不需要为每种类型创建菜单项。

### SpriteRenderer 和 Image 的内置接入

包内已包含 SpriteRenderer 和 UGUI Image 的自定义 Inspector，导入后无需额外脚本即可使用：

- SpriteRenderer 保持 Unity 原版面板结构，只替换 `Sprite` 字段；
- Image 保持 Unity 原版面板结构，只替换 `Source Image` 字段；
- 两个 Inspector 都支持多对象编辑、拖拽资源和 Undo；
- 选择窗口中的资源被点击后会直接写回组件，窗口保持打开。

可以通过以下菜单分别切换项目 Inspector 和 Unity 原版 Inspector：

- `Tools > 项目资源选择器 > SpriteRenderer > 使用项目资源选择器`；
- `Tools > 项目资源选择器 > SpriteRenderer > 使用 Unity 原版 Editor`；
- `Tools > 项目资源选择器 > Image > 使用项目资源选择器`；
- `Tools > 项目资源选择器 > Image > 使用 Unity 原版 Editor`。

切换状态保存在当前用户的 Unity `EditorPrefs` 中，并会立即刷新已打开的 Inspector。

## 窗口操作

- 资源类型：选择 Sprite、Material、Texture2D、Prefab 或 ScriptableObject；
- 搜索：按资源名称或资源路径过滤；
- 包含子文件夹：决定当前文件夹是否包含下级文件夹资源；
- 仅显示匹配文件夹：隐藏不包含当前类型资源的文件夹；
- 刷新：重新扫描项目资源并清理缓存；
- 缩略图：切换小图标、中图标或大图标；
- 文件夹面板：拖动文件夹树与资源区域之间的分隔线调整宽度。

## API

| API | 用途 |
| --- | --- |
| `AssetPicker.Open` | 按指定类型打开选择窗口，并通过回调返回资源 |
| `AssetPicker.OpenSelectionWindow` | 打开默认 Sprite 类型且支持窗口内切换类型的窗口 |
| `AssetPickerField.Draw` | 保留 Unity 原版 ObjectField，并增加文件夹选择入口 |
| `AssetPickerField.DrawPickerField` | 绘制点击后打开本工具窗口的资源字段 |

这些 API 仅可在 Editor 程序集中调用。需要接入自定义 Inspector 时，请确保该程序集引用 `SimOverride.AssetPicker.Editor`，或在代码中使用 `#if UNITY_EDITOR` 包裹 Editor 专用代码。

## 后续 Agent 接入

面向自动化编程 Agent 的接口约定、接入边界和调用示例见 [Documentation~/AGENT_PROMPT.md](Documentation~/AGENT_PROMPT.md)。该文件只描述公开包当前提供的能力，不要求修改 Unity 内置选择器或引入额外运行时依赖。

## 支持范围与限制

- 窗口内置类型为 Sprite、Material、Texture2D、Prefab 和 ScriptableObject；
- API 可以传入其他继承自 `UnityEngine.Object` 的类型，但是否能被 Unity `AssetDatabase` 正确检索取决于该资源类型的导入方式；
- 当前只支持单选，不支持多选；
- 当前选择的是具体资源，不是文件夹引用；
- 资源扫描范围固定为 `Assets`；
- 工具不会自动替换第三方组件或其他未列出的 Unity 内置 Inspector；需要接入其他组件时，请在目标自定义 Editor 中调用公开 API；
- 包内置的 SpriteRenderer 和 Image Inspector 依赖 Unity 序列化属性，并通过反射调用 Unity 原版 Editor 作为切换回退路径；不同 Unity 版本可能存在字段或内部类型差异。

## 许可证

当前版本尚未在仓库中声明具体许可证。公开分发前，请在仓库根目录补充 `LICENSE` 文件，并在此处说明授权范围。
