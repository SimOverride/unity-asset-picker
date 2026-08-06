# Unity 资源选择器 Agent 接入提示

你正在为 Unity Editor 编写资源字段或自定义 Inspector。需要按文件夹选择 Sprite、Material、Texture 或 Prefab 时，优先使用本包已经提供的接口，不要重新实现资源扫描和选择窗口。

## 适用边界

- 本包是独立的 Unity Editor UPM 包，只能被 Editor 程序集引用。
- 不要在运行时程序集、MonoBehaviour 运行时代码、ReactUI、Node.js、浏览器运行时或 `tongqubase-unity-ui-tool` 中引用本包。
- 不要修改项目无关的脚本，也不要尝试全局替换 Unity 内部的所有 ObjectField。
- Unity 的内置组件或第三方组件只有在项目明确要求时才编写对应的 `CustomEditor`；ScriptableObject 或自定义脚本的资源字段不会被本包自动接管。

## 可用接口

在 Editor 程序集中引用包的程序集 `SimOverride.AssetPicker.Editor`，然后使用：

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

`AssetPickerField.Draw` 会保留 Unity 原版 ObjectField，并在右侧增加文件夹入口。点击文件夹入口后，在选择窗口中按文件夹、子文件夹和名称/路径筛选；点击具体资源会通过回调返回结果。材质、Texture2D 和 Prefab 只需要分别传入对应的值、类型和回调转换类型。筛选 Prefab 时每个 Prefab 只返回根 GameObject，不会把子物体作为独立条目。

如果不需要 Unity 原生对象选择按钮，可以使用 `AssetPickerField.DrawPickerField`。窗口支持资源类型目录过滤、显示全部文件夹开关、小中大缩略图、可拖拽目录栏和搜索目录联动；选择资源后窗口保持打开。

如果需要直接打开选择窗口，可调用：

```csharp
AssetPicker.Open(
    typeof(Sprite),
    sprite,
    selected => sprite = selected as Sprite,
    "选择 Sprite");
```

包内窗口没有“选择”和“取消”按钮，点击资源即视为选择，回调执行后窗口保持打开。资源索引按类型缓存，项目变化或手动刷新后清理；不要在调用方重复实现全项目资源扫描。

## 编写自定义 Inspector 时的规则

1. 在 `CustomEditor` 中通过 `SerializedObject` 和 `SerializedProperty` 维护字段，不要直接绕过序列化系统写入资源字段。
2. 选择结果回调中使用 `Undo.RecordObjects` 或 `Undo.RecordObject`，并对 Prefab 实例记录属性修改。
3. 只替换需求指定的资源字段，其他 Inspector 字段保持 Unity 原版顺序、标签和行为。
4. 外部资源类型必须使用明确的 `typeof(Sprite)`、`typeof(Material)` 等类型约束。
5. 不要为运行时组件增加包程序集引用；编辑器程序集中的接入代码应保持 Editor-only。
6. 修改完成后检查 Unity 版本、程序集引用、Console 编译错误，并在隔离的临时 Unity 工程中验证包可以导入。
7. 如果修改了查询或窗口核心逻辑，同时运行包内 `Tests/Editor` 的 EditMode 测试。

## 交付前检查

- 资源字段点击后能打开文件夹选择入口。
- 选择结果能写回目标字段，并支持 Undo。
- 资源类型筛选没有显示不兼容对象。
- 不需要额外运行时脚本或 Inspector 手工拖拽引用。
- 没有修改 ReactUI、`tongqubase-unity-ui-tool` 或其他无关目录。
