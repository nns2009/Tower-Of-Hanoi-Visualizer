using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class ReadOnlyAttribute : PropertyAttribute { }

[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : UnityEditor.PropertyDrawer
{
	public override void OnGUI(Rect rect, UnityEditor.SerializedProperty prop, GUIContent label)
	{
		bool wasEnabled = GUI.enabled;
		GUI.enabled = false;
		UnityEditor.EditorGUI.PropertyField(rect, prop);
		GUI.enabled = wasEnabled;
	}
}

public static class SomeExtensions
{
	public static T Pop<T>(this List<T> list)
    {
		T result = list.Last();
		list.RemoveAt(list.Count - 1);
		return result;
    }
	public static List<T> Pop<T>(this List<T> list, int count)
    {
		List<T> result = list.GetRange(list.Count - count, count);
		list.RemoveRange(list.Count - count, count);
		return result;
    }
}