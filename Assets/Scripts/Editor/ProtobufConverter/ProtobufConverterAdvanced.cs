using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor
{
    /// <summary>
    /// 高级Protobuf转换器 - 支持批量转换
    /// </summary>
    public class ProtobufConverterAdvanced : EditorWindow
    {
        private List<MonoScript> _targetScripts = new List<MonoScript>();
        private Vector2 _scriptsScrollPosition;
        private Vector2 _resultScrollPosition;
        private string _generatedContent = "";
        private string _outputFolder = "Assets/Generated/Proto";
        private string _namespace = "Rogue.Protocol";
        private bool _generateCSharpProtoClasses = true;
        private bool _useProto3Optional = false;
        
        // 类型映射配置
        private Dictionary<string, string> _typeMapping = new Dictionary<string, string>
        {
            { "int", "int32" },
            { "Int32", "int32" },
            { "long", "int64" },
            { "Int64", "int64" },
            { "uint", "uint32" },
            { "UInt32", "uint32" },
            { "ulong", "uint64" },
            { "UInt64", "uint64" },
            { "float", "float" },
            { "Single", "float" },
            { "double", "double" },
            { "Double", "double" },
            { "bool", "bool" },
            { "Boolean", "bool" },
            { "string", "string" },
            { "String", "string" },
            { "byte[]", "bytes" },
            { "Byte[]", "bytes" },
            { "DateTime", "int64" },
            { "TimeSpan", "int64" },
            { "Guid", "string" },
            { "Vector2", "Vector2Proto" },
            { "Vector3", "Vector3Proto" },
            { "Quaternion", "QuaternionProto" },
            { "Color", "ColorProto" },
        };

        [MenuItem("Tools/Protobuf Converter/Advanced Converter", false, 101)]
        public static void ShowWindow()
        {
            var window = GetWindow<ProtobufConverterAdvanced>("Advanced Protobuf Converter");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawScriptSelection();
            DrawConfiguration();
            DrawActions();
            DrawResult();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Advanced C# to Protobuf Converter", titleStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawScriptSelection()
        {
            EditorGUILayout.LabelField("Target Scripts", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Script", GUILayout.Width(100)))
            {
                AddScript();
            }
            if (GUILayout.Button("Add Folder", GUILayout.Width(100)))
            {
                AddFolder();
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(100)))
            {
                _targetScripts.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 脚本列表
            _scriptsScrollPosition = EditorGUILayout.BeginScrollView(_scriptsScrollPosition, 
                GUI.skin.box, GUILayout.Height(150));
            
            for (int i = 0; i < _targetScripts.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _targetScripts[i] = EditorGUILayout.ObjectField(_targetScripts[i], typeof(MonoScript), false) as MonoScript;
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    _targetScripts.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (_targetScripts.Count == 0)
            {
                EditorGUILayout.HelpBox("Drag C# scripts here or click 'Add Script' to select", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawConfiguration()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            _namespace = EditorGUILayout.TextField("Protobuf Package:", _namespace);
            _outputFolder = EditorGUILayout.TextField("Output Folder:", _outputFolder);
            
            EditorGUILayout.Space(5);
            _generateCSharpProtoClasses = EditorGUILayout.Toggle("Generate C# Proto Classes", _generateCSharpProtoClasses);
            _useProto3Optional = EditorGUILayout.Toggle("Use Proto3 Optional", _useProto3Optional);
            
            EditorGUI.indentLevel--;
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(_targetScripts.Count == 0 || _targetScripts.All(s => s == null));
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Single .proto", GUILayout.Height(35)))
            {
                GenerateSingleProto();
            }
            if (GUILayout.Button("Generate Separate .proto Files", GUILayout.Height(35)))
            {
                GenerateSeparateProtos();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
        }

        private void DrawResult()
        {
            if (string.IsNullOrEmpty(_generatedContent)) return;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Generated Protobuf:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = _generatedContent;
                ShowNotification(new GUIContent("Copied to clipboard!"), 2f);
            }
            if (GUILayout.Button("Save to File(s)"))
            {
                SaveGeneratedFiles();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            _resultScrollPosition = EditorGUILayout.BeginScrollView(_resultScrollPosition, 
                GUILayout.Height(200));
            EditorGUILayout.TextArea(_generatedContent, EditorStyles.textArea);
            EditorGUILayout.EndScrollView();
        }

        private void AddScript()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Select C# Script", "Assets",
                new[] { "C# Script", "cs" });
            
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                path = path.Replace(Application.dataPath, "Assets");
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && !_targetScripts.Contains(script))
                {
                    _targetScripts.Add(script);
                }
            }
        }

        private void AddFolder()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
            
            if (!string.IsNullOrEmpty(folderPath))
            {
                string[] csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.TopDirectoryOnly);
                foreach (string file in csFiles)
                {
                    string relativePath = file.Replace(Application.dataPath, "Assets").Replace('\\', '/');
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
                    if (script != null && !_targetScripts.Contains(script))
                    {
                        _targetScripts.Add(script);
                    }
                }
            }
        }

        private void GenerateSingleProto()
        {
            try
            {
                StringBuilder protoBuilder = new StringBuilder();
                
                // 文件头
                protoBuilder.AppendLine("syntax = \"proto3\";");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"package {_namespace};");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"// Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                protoBuilder.AppendLine($"// Total messages: {_targetScripts.Count}");
                protoBuilder.AppendLine();
                
                // 生成每个message
                foreach (var script in _targetScripts.Where(s => s != null))
                {
                    GenerateMessageFromScript(script, protoBuilder);
                    protoBuilder.AppendLine();
                }
                
                // 添加Unity类型message定义
                AppendUnityTypeDefinitions(protoBuilder);
                
                _generatedContent = protoBuilder.ToString();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating proto: {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        private void GenerateSeparateProtos()
        {
            try
            {
                StringBuilder combinedBuilder = new StringBuilder();
                
                combinedBuilder.AppendLine("// Multiple .proto files will be generated");
                combinedBuilder.AppendLine($"// Output folder: {_outputFolder}");
                combinedBuilder.AppendLine();
                
                foreach (var script in _targetScripts.Where(s => s != null))
                {
                    StringBuilder singleProto = new StringBuilder();
                    singleProto.AppendLine("syntax = \"proto3\";");
                    singleProto.AppendLine();
                    singleProto.AppendLine($"package {_namespace};");
                    singleProto.AppendLine();
                    
                    GenerateMessageFromScript(script, singleProto);
                    
                    // 添加到组合视图
                    combinedBuilder.AppendLine($"// ========== {script.name}.proto ==========");
                    combinedBuilder.AppendLine(singleProto.ToString());
                    combinedBuilder.AppendLine();
                }
                
                _generatedContent = combinedBuilder.ToString();
                Repaint();
                
                // 提示用户保存
                if (EditorUtility.DisplayDialog("Save Files?", 
                    "Do you want to save the generated .proto files to the output folder?", "Yes", "No"))
                {
                    SaveSeparateProtoFiles();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating proto: {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        private void GenerateMessageFromScript(MonoScript script, StringBuilder protoBuilder)
        {
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string content = File.ReadAllText(scriptPath);
            string className = script.name;
            
            protoBuilder.AppendLine($"// Source: {className}.cs");
            protoBuilder.AppendLine($"message {className} {{");
            
            var fields = ParseClassFields(content);
            int fieldNumber = 1;
            
            foreach (var field in fields)
            {
                string protoType = MapCSharpToProtoType(field.Type);
                string protoFieldName = ToSnakeCase(field.Name);
                
                // 处理repeated
                if (field.IsCollection)
                {
                    protoType = $"repeated {protoType}";
                }
                
                // 处理可空类型
                if (field.IsNullable && _useProto3Optional)
                {
                    protoType = $"optional {protoType}";
                }
                
                protoBuilder.AppendLine($"    {protoType} {protoFieldName} = {fieldNumber};{(field.HasComment ? $" // {field.Comment}" : "")}");
                fieldNumber++;
            }
            
            if (fields.Count == 0)
            {
                protoBuilder.AppendLine("    // No public fields/properties found");
            }
            
            protoBuilder.AppendLine("}");
        }

        private List<ParsedField> ParseClassFields(string content)
        {
            List<ParsedField> fields = new List<ParsedField>();
            
            // 移除单行注释和多行注释
            string cleanContent = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
            cleanContent = Regex.Replace(cleanContent, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            // 匹配字段模式
            // 1. 自动属性: public Type Name { get; set; }
            // 2. 字段: public Type _name;
            // 3. 带初始化的字段: public Type Name = value;
            
            string propertyPattern = @"public\s+(\??[\w\[\]<>]+)\s+(\w+)\s*\{\s*get\s*;\s*set\s*;\s*\}";
            string fieldPattern = @"public\s+(\??[\w\[\]<>]+)\s+(\w+)\s*(?:=\s*[^;]+)?\s*;";
            
            // 匹配属性
            var propertyMatches = Regex.Matches(cleanContent, propertyPattern);
            foreach (Match match in propertyMatches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                
                // 跳过特殊属性
                if (ShouldSkipField(name)) continue;
                
                fields.Add(ParseField(type, name));
            }
            
            // 匹配字段（排除已匹配的属性）
            var fieldMatches = Regex.Matches(cleanContent, fieldPattern);
            foreach (Match match in fieldMatches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                
                // 跳过特殊字段和已存在的属性
                if (ShouldSkipField(name)) continue;
                if (fields.Any(f => f.Name == name || f.Name == name.TrimStart('_'))) continue;
                
                fields.Add(ParseField(type, name));
            }
            
            return fields;
        }

        private bool ShouldSkipField(string name)
        {
            string[] skipPatterns = { "get", "set", "value", "Equals", "GetHashCode", "ToString" };
            return skipPatterns.Contains(name) || name.StartsWith("<");
        }

        private ParsedField ParseField(string type, string name)
        {
            var field = new ParsedField();
            
            // 处理可空类型
            if (type.EndsWith("?"))
            {
                field.IsNullable = true;
                type = type.TrimEnd('?');
            }
            
            // 处理数组和List
            if (type.EndsWith("[]"))
            {
                field.IsCollection = true;
                type = type.TrimEnd('[', ']');
            }
            else if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                field.IsCollection = true;
                type = type.Substring(5, type.Length - 6);
            }
            else if (type.StartsWith("IList<") && type.EndsWith(">"))
            {
                field.IsCollection = true;
                type = type.Substring(6, type.Length - 7);
            }
            else if (type.StartsWith("IEnumerable<") && type.EndsWith(">"))
            {
                field.IsCollection = true;
                type = type.Substring(12, type.Length - 13);
            }
            
            // 处理Dictionary - 转换为message
            if (type.StartsWith("Dictionary<") && type.EndsWith(">"))
            {
                field.IsCollection = true;
                // Dictionary需要特殊处理，这里简化处理为map类型
                var match = Regex.Match(type, @"Dictionary<([^,]+),\s*([^>]+)>");
                if (match.Success)
                {
                    string keyType = MapCSharpToProtoType(match.Groups[1].Value.Trim());
                    string valueType = MapCSharpToProtoType(match.Groups[2].Value.Trim());
                    field.Type = $"map<{keyType}, {valueType}>";
                    return field;
                }
            }
            
            // 移除下划线前缀
            if (name.StartsWith("_"))
            {
                name = name.Substring(1);
            }
            
            field.Type = type;
            field.Name = name;
            
            return field;
        }

        private string MapCSharpToProtoType(string csharpType)
        {
            if (_typeMapping.TryGetValue(csharpType, out string protoType))
            {
                return protoType;
            }
            
            // 默认返回原类型（作为自定义类型）
            return csharpType;
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            StringBuilder result = new StringBuilder();
            result.Append(char.ToLower(input[0]));
            
            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }
            
            return result.ToString();
        }

        private void AppendUnityTypeDefinitions(StringBuilder protoBuilder)
        {
            protoBuilder.AppendLine("// Unity类型定义");
            protoBuilder.AppendLine("message Vector2Proto {");
            protoBuilder.AppendLine("    float x = 1;");
            protoBuilder.AppendLine("    float y = 2;");
            protoBuilder.AppendLine("}");
            protoBuilder.AppendLine();
            protoBuilder.AppendLine("message Vector3Proto {");
            protoBuilder.AppendLine("    float x = 1;");
            protoBuilder.AppendLine("    float y = 2;");
            protoBuilder.AppendLine("    float z = 3;");
            protoBuilder.AppendLine("}");
            protoBuilder.AppendLine();
            protoBuilder.AppendLine("message QuaternionProto {");
            protoBuilder.AppendLine("    float x = 1;");
            protoBuilder.AppendLine("    float y = 2;");
            protoBuilder.AppendLine("    float z = 3;");
            protoBuilder.AppendLine("    float w = 4;");
            protoBuilder.AppendLine("}");
            protoBuilder.AppendLine();
            protoBuilder.AppendLine("message ColorProto {");
            protoBuilder.AppendLine("    float r = 1;");
            protoBuilder.AppendLine("    float g = 2;");
            protoBuilder.AppendLine("    float b = 3;");
            protoBuilder.AppendLine("    float a = 4;");
            protoBuilder.AppendLine("}");
        }

        private void SaveGeneratedFiles()
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
            
            string fileName = "combined.proto";
            string fullPath = Path.Combine(_outputFolder, fileName);
            
            File.WriteAllText(fullPath, _generatedContent);
            AssetDatabase.Refresh();
            
            Debug.Log($"Saved: {fullPath}");
            ShowNotification(new GUIContent($"Saved: {fileName}"), 2f);
        }

        private void SaveSeparateProtoFiles()
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }
            
            foreach (var script in _targetScripts.Where(s => s != null))
            {
                StringBuilder protoBuilder = new StringBuilder();
                protoBuilder.AppendLine("syntax = \"proto3\";");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"package {_namespace};");
                protoBuilder.AppendLine();
                
                GenerateMessageFromScript(script, protoBuilder);
                
                string fileName = $"{script.name}.proto";
                string fullPath = Path.Combine(_outputFolder, fileName);
                
                File.WriteAllText(fullPath, protoBuilder.ToString());
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"Saved {_targetScripts.Count} .proto files to {_outputFolder}");
            ShowNotification(new GUIContent($"Saved {_targetScripts.Count} files"), 2f);
        }

        private class ParsedField
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public bool IsCollection { get; set; }
            public bool IsNullable { get; set; }
            public string Comment { get; set; }
            public bool HasComment => !string.IsNullOrEmpty(Comment);
        }
    }
}
