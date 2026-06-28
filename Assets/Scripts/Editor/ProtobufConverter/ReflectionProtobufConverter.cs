using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Rogue.Editor
{
    /// <summary>
    /// 基于反射的Protobuf转换器 - 直接从编译后的类型生成
    /// </summary>
    public class ReflectionProtobufConverter : EditorWindow
    {
        private MonoScript _targetScript;
        private Vector2 _scrollPosition;
        private string _result = "";
        private bool _includePrivateFields = false;
        private bool _includeProperties = true;
        private bool _generateComments = true;
        private string _protoPackage = "game.protocol";
        
        [MenuItem("Tools/Protobuf Converter/Reflection Converter", false, 102)]
        public static void ShowWindow()
        {
            var window = GetWindow<ReflectionProtobufConverter>("Reflection Converter");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawOptions();
            DrawTargetSelection();
            DrawActions();
            DrawResult();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Reflection-Based Protobuf Converter", titleStyle);
            EditorGUILayout.HelpBox("Uses reflection to analyze compiled types for more accurate results", MessageType.Info);
            EditorGUILayout.Space(10);
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            _protoPackage = EditorGUILayout.TextField("Package:", _protoPackage);
            _includePrivateFields = EditorGUILayout.Toggle("Include Private Fields", _includePrivateFields);
            _includeProperties = EditorGUILayout.Toggle("Include Properties", _includeProperties);
            _generateComments = EditorGUILayout.Toggle("Generate Comments", _generateComments);
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        private void DrawTargetSelection()
        {
            EditorGUILayout.LabelField("Target Script (Compiled Class)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _targetScript = EditorGUILayout.ObjectField(_targetScript, typeof(MonoScript), false, GUILayout.Height(40)) as MonoScript;
            
            if (GUILayout.Button("Select", GUILayout.Width(80), GUILayout.Height(40)))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("Select C# Script", "Assets", new[] { "C#", "cs" });
                if (!string.IsNullOrEmpty(path))
                {
                    string relativePath = path.Replace(Application.dataPath, "Assets").Replace('\\', '/');
                    _targetScript = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }

        private void DrawActions()
        {
            EditorGUI.BeginDisabledGroup(_targetScript == null);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate .proto", GUILayout.Height(40)))
            {
                GenerateProtoFromReflection();
            }
            
            if (GUILayout.Button("Generate with Dependencies", GUILayout.Height(40)))
            {
                GenerateWithDependencies();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);
        }

        private void DrawResult()
        {
            if (string.IsNullOrEmpty(_result)) return;
            
            EditorGUILayout.LabelField("Generated Protobuf:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = _result;
                ShowNotification(new GUIContent("Copied!"), 2f);
            }
            
            if (GUILayout.Button("Save to File..."))
            {
                SaveToFile();
            }
            EditorGUILayout.EndHorizontal();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));
            EditorGUILayout.TextArea(_result, EditorStyles.textArea);
            EditorGUILayout.EndScrollView();
        }

        private void GenerateProtoFromReflection()
        {
            if (_targetScript == null) return;

            try
            {
                // 获取脚本中的类
                var types = GetTypesFromScript(_targetScript);
                
                if (types.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No valid class found in the selected script", "OK");
                    return;
                }

                StringBuilder protoBuilder = new StringBuilder();
                
                // 文件头
                protoBuilder.AppendLine("syntax = \"proto3\";");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"package {_protoPackage};");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"// Generated by ReflectionProtobufConverter");
                protoBuilder.AppendLine($"// Source: {_targetScript.name}");
                protoBuilder.AppendLine($"// Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                protoBuilder.AppendLine();
                
                // 生成每个类型的message
                foreach (var type in types)
                {
                    GenerateMessageFromType(type, protoBuilder);
                    protoBuilder.AppendLine();
                }
                
                _result = protoBuilder.ToString();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating protobuf: {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        private void GenerateWithDependencies()
        {
            if (_targetScript == null) return;

            try
            {
                var types = GetTypesFromScript(_targetScript);
                var allTypes = new HashSet<Type>();
                var visited = new HashSet<Type>();
                
                // 收集所有依赖类型
                foreach (var type in types)
                {
                    CollectDependencies(type, allTypes, visited);
                }

                StringBuilder protoBuilder = new StringBuilder();
                
                // 文件头
                protoBuilder.AppendLine("syntax = \"proto3\";");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"package {_protoPackage};");
                protoBuilder.AppendLine();
                protoBuilder.AppendLine($"// Generated with dependencies");
                protoBuilder.AppendLine($"// Total messages: {allTypes.Count}");
                protoBuilder.AppendLine();
                
                // 生成所有message
                foreach (var type in allTypes.OrderBy(t => t.Name))
                {
                    GenerateMessageFromType(type, protoBuilder);
                    protoBuilder.AppendLine();
                }
                
                _result = protoBuilder.ToString();
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating protobuf: {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        private List<Type> GetTypesFromScript(MonoScript script)
        {
            var result = new List<Type>();
            
            // 获取脚本中的所有类型
            var scriptClass = script.GetClass();
            if (scriptClass != null)
            {
                result.Add(scriptClass);
            }
            
            // 也尝试从文本解析嵌套类型
            string scriptPath = AssetDatabase.GetAssetPath(script);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                string content = File.ReadAllText(scriptPath);
                var nestedTypes = ExtractNestedTypes(content);
                
                foreach (var typeName in nestedTypes)
                {
                    // 尝试找到嵌套类型
                    var nestedType = scriptClass?.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(t => t.Name == typeName);
                    
                    if (nestedType != null && !result.Contains(nestedType))
                    {
                        result.Add(nestedType);
                    }
                }
            }
            
            return result;
        }

        private List<string> ExtractNestedTypes(string content)
        {
            var types = new List<string>();
            
            // 匹配public class或struct定义
            var matches = System.Text.RegularExpressions.Regex.Matches(content, 
                @"public\s+(?:class|struct)\s+(\w+)");
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string typeName = match.Groups[1].Value;
                if (!types.Contains(typeName))
                {
                    types.Add(typeName);
                }
            }
            
            return types;
        }

        private void CollectDependencies(Type type, HashSet<Type> allTypes, HashSet<Type> visited)
        {
            if (visited.Contains(type)) return;
            visited.Add(type);
            
            if (IsValidProtoType(type))
            {
                allTypes.Add(type);
            }
            
            // 收集字段类型
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (_includePrivateFields)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }
            
            foreach (var field in type.GetFields(bindingFlags))
            {
                var fieldType = GetUnderlyingType(field.FieldType);
                if (IsValidProtoType(fieldType) && !visited.Contains(fieldType))
                {
                    CollectDependencies(fieldType, allTypes, visited);
                }
            }
            
            // 收集属性类型
            if (_includeProperties)
            {
                foreach (var prop in type.GetProperties(bindingFlags))
                {
                    if (!prop.CanRead) continue;
                    
                    var propType = GetUnderlyingType(prop.PropertyType);
                    if (IsValidProtoType(propType) && !visited.Contains(propType))
                    {
                        CollectDependencies(propType, allTypes, visited);
                    }
                }
            }
        }

        private void GenerateMessageFromType(Type type, StringBuilder protoBuilder)
        {
            string typeKind = type.IsValueType && !type.IsEnum ? "message" : "message";
            
            if (_generateComments)
            {
                protoBuilder.AppendLine($"// {type.Namespace}.{type.Name}");
            }
            
            protoBuilder.AppendLine($"{typeKind} {type.Name} {{");
            
            int fieldNumber = 1;
            
            // 字段
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (_includePrivateFields)
            {
                bindingFlags |= BindingFlags.NonPublic;
            }
            
            // 生成字段
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.IsStatic) continue;
                if (field.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                
                string protoField = ConvertFieldToProto(field, fieldNumber);
                protoBuilder.AppendLine(protoField);
                fieldNumber++;
            }
            
            // 生成属性
            if (_includeProperties)
            {
                foreach (var prop in type.GetProperties(bindingFlags))
                {
                    if (!prop.CanRead) continue;
                    if (prop.GetIndexParameters().Length > 0) continue; // 跳过索引器
                    
                    // 检查是否已经有对应的字段
                    string backingFieldName = $"<{prop.Name}>k__BackingField";
                    bool hasBackingField = type.GetFields(bindingFlags)
                        .Any(f => f.Name == backingFieldName || f.Name == $"_{prop.Name}");
                    
                    if (hasBackingField) continue; // 自动属性，字段已处理
                    
                    string protoField = ConvertPropertyToProto(prop, fieldNumber);
                    protoBuilder.AppendLine(protoField);
                    fieldNumber++;
                }
            }
            
            if (fieldNumber == 1)
            {
                protoBuilder.AppendLine("    // No fields/properties found");
            }
            
            protoBuilder.AppendLine("}");
        }

        private string ConvertFieldToProto(FieldInfo field, int fieldNumber)
        {
            string protoType = GetProtoTypeName(field.FieldType);
            string fieldName = ToSnakeCase(field.Name.TrimStart('_'));
            
            string comment = "";
            if (_generateComments)
            {
                comment = $" // Field: {field.FieldType.Name}";
            }
            
            return $"    {protoType} {fieldName} = {fieldNumber};{comment}";
        }

        private string ConvertPropertyToProto(PropertyInfo prop, int fieldNumber)
        {
            string protoType = GetProtoTypeName(prop.PropertyType);
            string propName = ToSnakeCase(prop.Name);
            
            string comment = "";
            if (_generateComments)
            {
                comment = $" // Property: {prop.PropertyType.Name}";
            }
            
            return $"    {protoType} {propName} = {fieldNumber};{comment}";
        }

        private string GetProtoTypeName(Type type)
        {
            // 处理可空类型
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }
            
            // 处理集合类型
            if (type.IsArray)
            {
                string elementType = GetProtoTypeName(type.GetElementType());
                return $"repeated {elementType}";
            }
            
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                
                if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) || 
                    genericDef == typeof(IEnumerable<>) || genericDef == typeof(ICollection<>))
                {
                    string elementType = GetProtoTypeName(genericArgs[0]);
                    return $"repeated {elementType}";
                }
                
                if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
                {
                    string keyType = GetProtoTypeName(genericArgs[0]);
                    string valueType = GetProtoTypeName(genericArgs[1]);
                    return $"map<{keyType}, {valueType}>";
                }
            }
            
            // 基础类型映射
            if (type == typeof(int)) return "int32";
            if (type == typeof(uint)) return "uint32";
            if (type == typeof(long)) return "int64";
            if (type == typeof(ulong)) return "uint64";
            if (type == typeof(short)) return "int32";
            if (type == typeof(ushort)) return "uint32";
            if (type == typeof(sbyte)) return "int32";
            if (type == typeof(byte)) return "uint32";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(byte[])) return "bytes";
            if (type == typeof(DateTime)) return "int64"; // Unix timestamp
            if (type == typeof(TimeSpan)) return "int64";
            if (type == typeof(Guid)) return "string";
            
            // Unity类型
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Vector4)) return "Vector4";
            if (type == typeof(Quaternion)) return "Quaternion";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(Color32)) return "Color";
            if (type == typeof(Rect)) return "Rect";
            if (type == typeof(Bounds)) return "Bounds";
            
            // 枚举
            if (type.IsEnum) return type.Name;
            
            // 自定义类型
            return type.Name;
        }

        private Type GetUnderlyingType(Type type)
        {
            // 处理可空类型
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return Nullable.GetUnderlyingType(type);
            }
            
            // 处理集合类型
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    return genericArgs[0];
                }
                return genericArgs.Length > 0 ? genericArgs[0] : type;
            }
            
            return type;
        }

        private bool IsValidProtoType(Type type)
        {
            if (type == null) return false;
            if (type.IsPrimitive) return false; // 基础类型不需要单独定义
            if (type == typeof(string)) return false;
            if (type == typeof(decimal)) return false;
            if (type.IsArray) return false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return false;
            
            // Unity引擎类型
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine")) return false;
            
            // System类型
            if (type.Namespace != null && type.Namespace.StartsWith("System")) return false;
            
            return true;
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
            string path = EditorUtility.SaveFilePanel("Save Protobuf Definition", "Assets", 
                $"{_targetScript?.name ?? "generated"}.proto", "proto");
            
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, _result);
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent($"Saved: {Path.GetFileName(path)}"), 2f);
            }
        }
    }
}
