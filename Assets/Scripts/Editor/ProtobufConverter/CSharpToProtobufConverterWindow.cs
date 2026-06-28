using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor
{
    /// <summary>
    /// C#类转Protobuf转换器窗口
    /// </summary>
    public class CSharpToProtobufConverterWindow : EditorWindow
    {
        private MonoScript _targetScript;
        private Vector2 _scrollPosition;
        private string _generatedProtoContent = "";
        private bool _showAdvancedOptions = false;
        private string _outputPath = "Assets/ProtoDefinitions";
        private string _packageName = "rogue.game";
        private int _fieldNumberStart = 1;
        private bool _addJsonTag = true;

        [MenuItem("Tools/C# to Protobuf Converter", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<CSharpToProtobufConverterWindow>("C# to Protobuf");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("C# Class to Protobuf Converter", titleStyle);
            
            EditorGUILayout.Space(10);
            
            // 脚本选择
            EditorGUILayout.LabelField("Target C# Script", EditorStyles.boldLabel);
            _targetScript = EditorGUILayout.ObjectField("Script:", _targetScript, typeof(MonoScript), false) as MonoScript;
            
            EditorGUILayout.Space(10);
            
            // 高级选项
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options");
            if (_showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                _packageName = EditorGUILayout.TextField("Package Name:", _packageName);
                _outputPath = EditorGUILayout.TextField("Output Path:", _outputPath);
                _fieldNumberStart = EditorGUILayout.IntField("Field Start Number:", _fieldNumberStart);
                _addJsonTag = EditorGUILayout.Toggle("Add JSON Tags:", _addJsonTag);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            
            // 转换按钮
            EditorGUI.BeginDisabledGroup(_targetScript == null);
            if (GUILayout.Button("Generate Protobuf", GUILayout.Height(40)))
            {
                GenerateProtobuf();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // 结果展示
            if (!string.IsNullOrEmpty(_generatedProtoContent))
            {
                EditorGUILayout.LabelField("Generated Protobuf:", EditorStyles.boldLabel);
                
                // 复制和保存按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy to Clipboard"))
                {
                    GUIUtility.systemCopyBuffer = _generatedProtoContent;
                    ShowNotification(new GUIContent("Copied!"), 2f);
                }
                if (GUILayout.Button("Save to File"))
                {
                    SaveToFile();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // 预览区域
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                EditorGUILayout.TextArea(_generatedProtoContent, EditorStyles.textArea);
                EditorGUILayout.EndScrollView();
            }
        }

        private void GenerateProtobuf()
        {
            if (_targetScript == null) return;

            try
            {
                string scriptPath = AssetDatabase.GetAssetPath(_targetScript);
                string scriptContent = File.ReadAllText(scriptPath);
                string className = _targetScript.name;
                
                StringBuilder protoBuilder = new StringBuilder();
                
                // 文件头注释
                protoBuilder.AppendLine($"// Generated from {className}.cs");
                protoBuilder.AppendLine($"// Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                protoBuilder.AppendLine();
                
                // 包名
                if (!string.IsNullOrEmpty(_packageName))
                {
                    protoBuilder.AppendLine($"package {_packageName};");
                    protoBuilder.AppendLine();
                }
                
                // 语法版本
                protoBuilder.AppendLine("syntax = \"proto3\";");
                protoBuilder.AppendLine();
                
                // 解析类
                ParseClassToProto(scriptContent, className, protoBuilder);
                
                _generatedProtoContent = protoBuilder.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating protobuf: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate protobuf:\n{e.Message}", "OK");
            }
        }

        private void ParseClassToProto(string scriptContent, string className, StringBuilder protoBuilder)
        {
            // 提取命名空间
            string namespaceName = ExtractNamespace(scriptContent);
            if (!string.IsNullOrEmpty(namespaceName))
            {
                protoBuilder.AppendLine($"// Original namespace: {namespaceName}");
                protoBuilder.AppendLine();
            }

            // 开始message定义
            protoBuilder.AppendLine($"message {className} {{");
            
            // 解析字段
            List<FieldInfo> fields = ParseFields(scriptContent);
            int fieldNumber = _fieldNumberStart;
            
            foreach (var field in fields)
            {
                string protoType = ConvertToProtoType(field.Type);
                string fieldName = ToSnakeCase(field.Name);
                
                protoBuilder.AppendLine($"    {protoType} {fieldName} = {fieldNumber};");
                fieldNumber++;
            }
            
            protoBuilder.AppendLine("}");
        }

        private string ExtractNamespace(string scriptContent)
        {
            Match match = Regex.Match(scriptContent, @"namespace\s+([\w\.]+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        private List<FieldInfo> ParseFields(string scriptContent)
        {
            List<FieldInfo> fields = new List<FieldInfo>();
            
            // 移除注释
            string noComments = Regex.Replace(scriptContent, @"//.*$", "", RegexOptions.Multiline);
            noComments = Regex.Replace(noComments, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            // 匹配public字段和属性
            // 匹配模式: public Type Name { get; set; } 或 public Type _name;
            string fieldPattern = @"public\s+(\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)";
            
            MatchCollection matches = Regex.Matches(noComments, fieldPattern);
            foreach (Match match in matches)
            {
                string type = match.Groups[1].Value;
                string name = match.Groups[2].Value;
                
                // 跳过属性访问器中的字段
                if (name == "get" || name == "set") continue;
                
                // 移除下划线前缀
                if (name.StartsWith("_"))
                {
                    name = name.Substring(1);
                }
                
                fields.Add(new FieldInfo { Type = type, Name = name });
            }
            
            return fields;
        }

        private string ConvertToProtoType(string csharpType)
        {
            // 移除空格
            csharpType = csharpType.Trim();
            
            // 处理可空类型
            if (csharpType.EndsWith("?"))
            {
                csharpType = csharpType.TrimEnd('?');
            }
            
            // 处理List<T>
            Match listMatch = Regex.Match(csharpType, @"List<(.+)>");
            if (listMatch.Success)
            {
                string innerType = ConvertToProtoType(listMatch.Groups[1].Value);
                return $"repeated {innerType}";
            }
            
            // 处理数组
            if (csharpType.EndsWith("[]"))
            {
                string innerType = ConvertToProtoType(csharpType.TrimEnd('[', ']'));
                return $"repeated {innerType}";
            }
            
            // 基础类型映射
            switch (csharpType.ToLower())
            {
                case "int":
                case "int32": return "int32";
                case "long":
                case "int64": return "int64";
                case "uint":
                case "uint32": return "uint32";
                case "ulong":
                case "uint64": return "uint64";
                case "bool": return "bool";
                case "float": return "float";
                case "double": return "double";
                case "string": return "string";
                case "byte[]":
                case "bytes": return "bytes";
                case "datetime": return "int64"; // Unix timestamp
                case "guid": return "string";
                default:
                    // 自定义类型
                    return csharpType;
            }
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

        private void SaveToFile()
        {
            if (string.IsNullOrEmpty(_generatedProtoContent) || _targetScript == null) return;
            
            try
            {
                // 确保目录存在
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }
                
                string fileName = $"{_targetScript.name}.proto";
                string fullPath = Path.Combine(_outputPath, fileName);
                
                File.WriteAllText(fullPath, _generatedProtoContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"Protobuf definition saved to: {fullPath}");
                ShowNotification(new GUIContent($"Saved: {fileName}"), 2f);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving file: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to save file:\n{e.Message}", "OK");
            }
        }

        private class FieldInfo
        {
            public string Type { get; set; }
            public string Name { get; set; }
        }
    }
}
