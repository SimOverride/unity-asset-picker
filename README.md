# Unity 资源文件夹选择器

这是一个完全独立的 Unity Editor UPM 工具，不依赖 ReactUI、游戏运行时代码或其他项目程序集。

## 功能

- 保留 Unity 原版 `ObjectField`，旁边提供按文件夹选择入口；
- 支持 Sprite、Material、Texture、Prefab 等资源类型；
- 支持按资源类型过滤文件夹树，也可以切换为显示全部文件夹；
- 支持当前文件夹/子文件夹筛选、名称和路径搜索，搜索可同步过滤文件夹；
- 支持网格布局、小中大缩略图和可拖拽目录栏；
- 点击资源立即完成选择，窗口保持打开；
- 不显示“选择”和“取消”按钮；
- 按资源类型保存最近一次文件夹位置到本地 `EditorPrefs`；
- 按资源类型缓存资源索引，项目变化或手动刷新时自动清理；
- 资源扫描只读取 `Assets`，不会修改项目资源。

## 安装

有两种安装方式：

- Unity 资产包：打开 `Assets > Import Package > Custom Package...`，导入生成的 `.unitypackage` 文件；
- UPM 包：在 Unity Package Manager 中选择 `Add package from tarball...`，导入生成的 `.tgz` 文件。

也可以把本目录作为本地包，或通过 Git URL 安装。

后续 Agent 的编辑器接入提示见 `Documentation~/AGENT_PROMPT.md`。

## 接入字段

在需要提供文件夹筛选的 Editor 程序集中引用 `SimOverride.AssetPicker.Editor`：

```csharp
using AssetPicker.Editor;
using UnityEngine;

private Sprite sprite;

private void DrawSpriteField()
{
    sprite = AssetPickerField.Draw(
        "Sprite",
        sprite,
        typeof(Sprite),
        selected => sprite = selected as Sprite) as Sprite;
}
```

材质字段将类型和值改为 `Material` 即可。原版 ObjectField 仍然可直接使用；只有点击旁边的文件夹按钮时才打开本工具窗口。

如果需要隐藏 Unity 原生对象选择按钮，可以使用 `AssetPickerField.DrawPickerField`；如果需要从包菜单直接打开可切换类型的窗口，可以使用 `Tools > 项目资源选择器 > 打开选择窗口`。

## 当前限制

- 不能通过 Unity 公开 API 自动修改第三方 Inspector 中的原版选择器，因此需要在目标字段中使用 `AssetPickerField.Draw`，或直接调用 `AssetPicker.Open`。
- 当前只支持选择一个资源，不支持多选。
- 当前只选择具体资源，不保存文件夹引用。
- 包内测试位于 `Tests/Editor`，覆盖类型过滤、搜索联动和缓存失效。
