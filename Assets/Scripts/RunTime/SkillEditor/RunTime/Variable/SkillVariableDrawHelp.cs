using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 变量绘制
/// </summary>
public static class SkillVariableDrawHelp
{
    /// <summary>
    /// 绘制变量
    /// </summary>
    /// <param name="propertyType">类型</param>
    /// <param name="propertyValue">值</param>
    /// <param name="inputFieldHight">变量宽度</param>
    /// <param name="inputFieldWeight">变量高度</param>
    /// <param name="isExpandWidth"></param>
    /// <returns></returns>
    [Obsolete("Obsolete")]
    public static object DrawVariable(Type propertyType, object propertyValue, float inputFieldHight,
        float inputFieldWeight, bool isExpandWidth)
    {
        GUILayoutOption[] options = new GUILayoutOption[2];

        options[0] = GUILayout.Height(inputFieldHight);

        if (!isExpandWidth)
        {
            options[1] = GUILayout.Width(inputFieldWeight);
        }
        else
        {
            options[1] = GUILayout.ExpandWidth(true);
        }

        object tempValue = null;
#if UNITY_EDITOR
        if (propertyType == typeof(int))
        {
            tempValue = EditorGUILayout.IntField((int)propertyValue, options);
        }
        else if (propertyType == typeof(float))
        {
            tempValue = EditorGUILayout.FloatField((float)propertyValue, options);
        }
        else if (propertyType == typeof(bool))
        {
            tempValue = EditorGUILayout.Toggle((bool)propertyValue, options);
        }
        else if (propertyType == typeof(string))
        {
            tempValue = EditorGUILayout.TextField((string)propertyValue, options);
        }
        else if (propertyType == typeof(Vector3))
        {
            tempValue =
                EditorGUILayout.Vector3Field("", (Vector3)propertyValue, options);
        }
        else if (propertyType == typeof(Vector2))
        {
            tempValue =
                EditorGUILayout.Vector2Field("", (Vector2)propertyValue, options);
        }
        else if (propertyType == typeof(AnimationCurve))
        {
            tempValue = EditorGUILayout.CurveField((AnimationCurve)propertyValue, options);
        }
        else if (propertyType.IsSubclassOf(typeof(System.Enum)))
        {
            tempValue = EditorGUILayout.EnumPopup((System.Enum)propertyValue, options);
        }
        else if (propertyType == typeof(AnimationClip))
        {
            tempValue = EditorGUILayout.ObjectField((AnimationClip)propertyValue, typeof(AnimationClip), options);
        }
        else if (propertyType == typeof(GameObject))
        {
            tempValue = (GameObject)EditorGUILayout.ObjectField((GameObject)propertyValue, typeof(GameObject), false);
        }
        else if (propertyType == typeof(AudioClip))
        {
            tempValue = EditorGUILayout.ObjectField((AudioClip)propertyValue, typeof(AudioClip), options);
        }
        else if (propertyType == typeof(List<int>))
        {
            List<int> myList = (List<int>)propertyValue;

            if (myList == null)
            {
                myList = new List<int>();
            }

            EditorGUILayout.BeginVertical();
            // 绘制列表元素
            for (int i = 0; i < myList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                myList[i] = EditorGUILayout.IntField(myList[i]);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    myList.RemoveAt(i);
                    i--; // 调整索引
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            // 添加按钮
            if (GUILayout.Button("Add New"))
            {
                myList.Add(1);
            }

            tempValue = myList;
        }
        else if (propertyType.IsEnumerableCollection())
        {
            IList list = (IList)propertyValue;

            if (list == null)
            {
                return null;
            }

            Type type = propertyType.GetEnumerableElementType();

            if (type.IsSubclassOf(typeof(System.Enum)))
            {
                EditorGUILayout.BeginVertical();
                // 绘制列表元素
                for (int i = 0; i < list.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    list[i] = EditorGUILayout.EnumPopup((System.Enum)list[i], options);
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        list.RemoveAt(i);
                        i--; // 调整索引
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                // 添加按钮
                if (GUILayout.Button("Add New"))
                {
                    list.Add(0);
                }
            }

            tempValue = list;
        }
#endif
        return tempValue;
    }
    
    public static bool IsEnumerableCollection(this Type type)
    {
        if (type == null)
        {
            return false;
        }

        return typeof(IEnumerable).IsAssignableFrom(type) && (type.IsGenericType || type.IsArray);
    }
    
    public static Type GetEnumerableElementType(this Type type)
    {
        if (type == null)
        {
            return null;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(type))
        {
            return null;
        }

        if (type.HasElementType || type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var args = type.RTGetGenericArguments();
            if (args.Length == 1)
            {
                return args[0];
            }

            if (typeof(IDictionary).RTIsAssignableFrom(type) && args.Length == 2)
            {
                return args[1];
            }
        }
        
        return null;
    }
    
    public static Type[] RTGetGenericArguments(this Type type)
    {
        return type.GetGenericArguments();
    }
    
    public static bool RTIsAssignableTo(this Type type, Type other)
    {
        return other.RTIsAssignableFrom(type);
    }
    
    public static bool RTIsAssignableFrom(this Type type, Type other)
    {
        return type.IsAssignableFrom(other);
    }

    public static SkillBlackVariable GetVariableInitValue(Type propertyType)
    {
        if (propertyType == typeof(int))
        {
            return new SkillIntVariable();
        }
        else if (propertyType == typeof(float))
        {
            return new SkillFloatVariable();
        }
        else if (propertyType == typeof(bool))
        {
            return new SkillBoolVariable();
        }
        else if (propertyType == typeof(string))
        {
            return new SkillStringVariable();
        }
        else if (propertyType == typeof(Vector3))
        {
            return new SkillVector3Variable();
        }
        else if (propertyType == typeof(Vector2))
        {
            return new SkillVector2Variable();
        }
        else if (propertyType == typeof(AnimationCurve))
        {
            return new SkillCuveVariable();
        }

        if (propertyType.IsEnum)
        {
            SkillEnumVariable skillEnumVariable = new SkillEnumVariable();

            skillEnumVariable.SetEnumType(propertyType.FullName);

            Array array = Enum.GetValues(propertyType);

            skillEnumVariable.SetValue(array.GetValue(0));

            return skillEnumVariable;
        }

        return null;
    }
}