---
name: csharp-performance-conventions
description: Apply MiniCore C# coding conventions when creating or editing C# code in this project. Use for gameplay, framework, editor, UI, hot-update, and network changes that require Chinese XML documentation for all methods, organize class members into regions by access scope and Unity reference fields, preserve existing file encodings, create new files as UTF-8 without BOM, and avoid avoidable allocations in hot paths such as Update.
---

# MiniCore C# 开发规范

对本项目中创建或修改的 C# 文件执行以下规则；仅做文档、分析或非 C# 文件变更时不触发。

## 注释

- 为所有新增或修改的方法添加中文 `///` XML 文档注释，不区分 `public`、`private`、`protected`、`internal`、`static` 或 Unity 生命周期方法。
- 方法注释必须使用多行 XML 文档格式，不要把 `<summary>` 写成单行。例如使用：
  `/// <summary>`
  `/// 网络消息中枢，负责多会话的发包、收包、RPC、心跳和处理器派发。`
  `/// </summary>`
  不要使用 `/// <summary>默认客户端会话标识。</summary>`。
- 方法的每个参数都需要补充中文 `/// <param name="...">...</param>` 说明；有返回值的方法需要补充中文 `/// <returns>...</returns>` 说明。
- 为所有新增或修改的 `public` 字段、属性、事件、类、结构、枚举和接口添加中文 `///` XML 文档注释。注释说明用途、关键约束或返回/参数语义；不要写重复代码字面的空话。
- 新增或修改的私有字段，尽量在声明末尾添加简短中文 `//` 注释，说明其持有的数据或缓存目的。
- 修改已有公共 API 时，保留原有有效注释；若缺失或与实现不符，再补充或修正。

## 类成员分区

- 修改某个 C# 类时，按本规则整理该类完整成员分区；不要只整理新增代码。未触碰的其他类不强制整理。
- 类顶层成员使用英文+中文标题的 `#region` 分组；不要创建没有成员的空分区。可用标题包括：
  - `#region UnityProperty Unity 引用属性`
  - `#region Private 私有成员`
  - `#region Protected 受保护成员`
  - `#region Internal 内部成员`
  - `#region Public 公共成员`
  - `#region Interface 接口实现`
  - `#region Override 重写实现`
- 继承 `MonoBehaviour` 或 Unity 相关基类的类，所有 inspector/UI 绑定的 Unity 引用字段统一放入 `UnityProperty` 分区，标题必须包含精确文本 `UnityProperty`。
- `UnityProperty` 包含 `Button`、`InputField`、`TMP_InputField`、`Text`、`TMP_Text`、`Image`、`RawImage`、`Slider`、`Toggle`、`Dropdown`、`TMP_Dropdown`、`ScrollRect`、`RectTransform`、`Transform`、`GameObject`、`MonoBehaviour` 派生组件等 Unity 对象引用字段。
- 普通成员按访问修饰符放入对应分区；每个分区内建议顺序为字段、属性/事件、构造/初始化、方法。
- 接口显式实现和抽象类/基类重写方法单独分区：接口实现放入 `Interface 接口实现`，`override` 方法放入 `Override 重写实现`。
- 若成员同时符合多个分区，优先级为 `UnityProperty`、`Interface 接口实现`、`Override 重写实现`、访问修饰符分区；不要重复放入多个分区。
- 已有大段复杂逻辑中的功能型 `#region` 可以保留，但类顶层分区必须以访问作用域、`UnityProperty`、接口实现和重写实现为主。

## 编码

- 新建源文件默认使用 UTF-8（无 BOM）。
- 修改已有文件前先保留其原有编码；若不是 UTF-8，不要为了统一格式转换编码。
- 避免用会整体重写文件且不能保留编码的方式编辑已有文件。只做必要的局部改动。

## 分配与高频路径

- 默认避免在方法内部创建不必要的局部对象，尤其是 `Update`、`FixedUpdate`、网络收发、循环和高频回调。
- 避免在高频路径中反复 `new` 临时 `Vector2`、`Vector3`、数组、集合、委托、字符串或闭包。可复用且不影响并发/重入语义的值，优先缓存为私有字段或 `static readonly` 常量。
- 缓存前先判断对象是否可变、调用是否可能重入、是否存在多线程访问。不能安全复用的对象不得为了零分配而共享。
- 对需要频繁创建、生命周期明确且数量较多的对象，先搜索项目现有对象池或集合池实现并复用；没有合适对象池时，再说明新增对象池的必要性和归还时机。
- 普通业务方法中为提高可读性而创建的少量值类型局部变量可以保留。重点消除的是可观测 GC、重复分配和高频调用中的抖动。

## 修改前后检查

1. 检查新增或修改的所有方法是否有准确的中文 `///` 注释，且 `<summary>` 为多行格式。
2. 检查新增或修改的私有字段是否有必要的行尾中文说明。
3. 检查方法参数是否有中文 `param` 说明，有返回值时是否有中文 `returns` 说明。
4. 检查被修改类是否按访问作用域、`UnityProperty`、接口实现和重写实现整理完整 `#region`，且没有空分区。
5. 检查继承 `MonoBehaviour` 或 Unity 相关基类的类是否把 Unity 引用字段集中放入 `UnityProperty` 分区。
6. 确认新文件为 UTF-8（无 BOM），已有文件编码未被改变。
7. 审查高频路径中的 `new`、LINQ、字符串拼接、闭包、装箱和临时集合；优先采用安全缓存或现有对象池。
