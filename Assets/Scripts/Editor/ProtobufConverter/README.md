# C# to Protobuf Converter Tools

为Unity项目提供的C#类转Protobuf定义编辑器工具。

## 已导入的DLL

- `Assets/Plugins/Google.Protobuf.dll` - Google Protobuf库

## 工具列表

### 1. CSharpToProtobufConverterWindow (基础版)

**打开方式**: `Tools > C# to Protobuf Converter`

**功能**:
- 选择单个C#脚本
- 解析public字段和属性
- 生成`.proto`文件
- 复制到剪贴板或保存到文件
- 支持自定义包名和输出路径

**类型映射**:
| C#类型 | Protobuf类型 |
|--------|-------------|
| int/int32 | int32 |
| long/int64 | int64 |
| uint/uint32 | uint32 |
| ulong/uint64 | uint64 |
| float | float |
| double | double |
| bool | bool |
| string | string |
| byte[] | bytes |
| DateTime | int64 (Unix时间戳) |
| List<T> | repeated T |
| T[] | repeated T |
| Vector2/3 | Vector2Proto/3Proto |

---

### 2. ProtobufConverterAdvanced (高级版)

**打开方式**: `Tools > Protobuf Converter > Advanced Converter`

**功能**:
- 批量选择多个脚本
- 添加整个文件夹的脚本
- 生成单个合并的.proto文件
- 生成多个单独的.proto文件
- 支持List<T>, Dictionary<K,V>, 数组等多种集合类型
- 自动处理Unity类型(Vector2, Vector3, Quaternion, Color)

**配置选项**:
- Package名称
- 输出文件夹
- 是否生成C# Proto类
- 是否使用Proto3 optional特性

---

### 3. ReflectionProtobufConverter (反射版)

**打开方式**: `Tools > Protobuf Converter > Reflection Converter`

**功能**:
- 使用反射分析编译后的类型
- 更准确类型识别
- 自动收集依赖类型
- 支持嵌套类型
- 可选择是否包含私有字段
- 可选择是否包含属性

**生成模式**:
- `Generate .proto` - 仅转换选中的类
- `Generate with Dependencies` - 转换选中类及其所有依赖的自定义类型

---

## 使用方法

### 基础用法

1. 在Unity中打开 `Tools > C# to Protobuf Converter`
2. 将要转换的C#脚本拖到 `Script` 字段
3. 点击 `Generate Protobuf`
4. 查看生成的Protobuf定义
5. 点击 `Copy to Clipboard` 复制或 `Save to File` 保存

### 高级用法

1. 打开 `Tools > Protobuf Converter > Advanced Converter`
2. 点击 `Add Script` 或 `Add Folder` 添加多个脚本
3. 配置选项（包名、输出路径等）
4. 点击 `Generate Single .proto` 或 `Generate Separate .proto Files`
5. 保存生成的文件

### 反射转换器用法

1. 打开 `Tools > Protobuf Converter > Reflection Converter`
2. 选择编译后的C#脚本
3. 配置选项（是否包含私有字段、属性等）
4. 点击 `Generate .proto` 或 `Generate with Dependencies`
5. 保存结果

---

## 示例

### 输入C#类

```csharp
public class PlayerData
{
    public int HeroId { get; set; }
    public bool IsSelf { get; set; }
    public long ServerEntityId { get; set; }
    public string PlayerName { get; set; }
    public List<int> Inventory { get; set; }
    public Vector3 Position { get; set; }
}
```

### 输出Protobuf

```protobuf
syntax = "proto3";

package rogue.game;

message PlayerData {
    int32 hero_id = 1;
    bool is_self = 2;
    int64 server_entity_id = 3;
    string player_name = 4;
    repeated int32 inventory = 5;
    Vector3Proto position = 6;
}

message Vector3Proto {
    float x = 1;
    float y = 2;
    float z = 3;
}
```

---

## 注意事项

1. **脚本必须编译成功**才能使用反射转换器
2. 基础版通过正则表达式解析源代码，对复杂语法可能不完全支持
3. 反射版从编译后的类型获取信息，更准确但需要脚本可编译
4. Unity类型(Vector2, Vector3等)会被转换为自定义message
5. Dictionary类型会被转换为`map<key_type, value_type>`

---

## 生成的文件位置

默认输出路径:
- 基础版: `Assets/ProtoDefinitions`
- 高级版: `Assets/Generated/Proto`

可以在工具界面中自定义输出路径。
