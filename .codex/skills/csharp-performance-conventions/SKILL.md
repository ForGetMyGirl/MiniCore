---
name: csharp-performance-conventions
description: Apply the user's global C# coding and performance conventions whenever creating or editing C# code in any project, including Unity gameplay, framework, editor, UI, hot-update, and network code. Require Chinese XML documentation, one top-level type per file with narrow exceptions, member regions, encoding preservation, UTF-8 without BOM for new files, and avoidance of unnecessary hot-path allocations.
---

# 全局 C# 开发规范

对所有项目中创建或修改的 C# 文件执行以下规则；仅做文档、分析或非 C# 文件变更时不触发。

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

## 文件与类型组织

- 默认一个 `.cs` 文件只声明一个顶级类型。类、结构、接口、枚举、委托和记录类型都遵守此规则。
- 文件名与其中的顶级类型同名。例如 `Animal.cs` 只放 `Animal`，不要同时放入顶级的 `Dog`、`Cat` 或配套枚举；为它们分别创建对应文件，并按职责放入合适目录。
- 不要为了减少文件数量，把多个业务类型、数据类型、接口、实现类或枚举堆在同一文件中。优先用目录表达模块和类别。
- 新增类型时不要继续向已有的多类型文件中追加声明。修改旧的多类型文件时，若拆分属于当前改动范围且不会造成无关的大面积变更，应将涉及的顶级类型拆到独立文件。
- `partial` 类型可以按生成代码、平台实现或明确职责拆成多个文件，但每个文件仍只包含这一个顶级类型。
- 只有下列少数情况允许同一文件包含多个类型，并在无法从结构直接看出原因时添加简短说明：
  - 同一协议或通讯契约中的纯消息 DTO，例如一组 `LoginRequest`、`LoginResponse`、`CreateRoomRequest`。这些类型必须作为同一协议单元共同生成、版本化或维护，不得借此形成无边界的消息大杂烩；文件名应描述该契约组，例如 `LoginMessages.cs`。
  - 自动生成的代码，且文件布局由生成器、协议编译器或设计器决定。修改模板、Schema 或生成器，不要手工拆改生成产物。
  - 只服务于一个宿主类型、没有独立领域含义且不应被外部复用的私有嵌套类型，例如内部状态枚举、比较器、缓存键或判别联合的私有分支。它们必须真正嵌套在宿主类型中，不能作为同文件的并列顶级类型。
  - 集中封装原生互操作边界的嵌套声明，例如 `NativeMethods` 内与同一 ABI 紧密绑定的私有结构和枚举。若声明会被其他模块使用或具有独立语义，仍应拆分。
- 公共或内部可复用的辅助类型、选项、结果、Attribute、枚举和扩展类通常都应独立成文件，即使当前只有一个调用方。
- 无法确定是否属于例外时，选择拆分。例外应极少使用，并以语义不可分割或工具生成约束为依据，而不是以“文件少一些”为理由。

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
6. 检查每个新增顶级类型是否独占文件；若使用多类型文件例外，确认它确实符合协议、生成代码或私有嵌套等严格条件。
7. 确认新文件为 UTF-8（无 BOM），已有文件编码未被改变。
8. 审查高频路径中的 `new`、LINQ、字符串拼接、闭包、装箱和临时集合；优先采用安全缓存或现有对象池。
