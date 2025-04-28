#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true)]
public class PropertyCopier : Editor {
    static string name_selectProperty;
    static object obj_copy;

    public override void OnInspectorGUI() {
        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        property.NextVisible(true);
        while (property.NextVisible(true)) {
            Rect propertyRect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(propertyRect, property, true);
            if (Event.current.type == EventType.MouseDown && propertyRect.Contains(Event.current.mousePosition)) {
                name_selectProperty = property.name;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("Tools/Reference/Property Copier/Copy")]
    static void CopyProperty() {
        Component target = Selection.activeGameObject.GetComponent<MonoBehaviour>();
        if (target == null)
            return;
        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            if (field.Name == name_selectProperty && typeof(Object).IsAssignableFrom(field.FieldType)) {
                obj_copy = field.GetValue(target);
                break;
            }
        }
    }

    [MenuItem("Tools/Reference/Property Copier/Paste")]
    static void PasteProperty() {
        Component target = Selection.activeGameObject.GetComponent<MonoBehaviour>();
        if (target == null)
            return;

        FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields) {
            if (field.Name == name_selectProperty && typeof(Object).IsAssignableFrom(field.FieldType)) {
                object convertedValue = obj_copy;
                if (typeof(Component).IsAssignableFrom(convertedValue.GetType()))
                    convertedValue = (convertedValue as Component).gameObject;
                if (typeof(Component).IsAssignableFrom(field.FieldType)) {
                    Component componentCopy = obj_copy as Component;
                    if (componentCopy != null)
                        convertedValue = (obj_copy as Component).GetComponent(field.FieldType);
                    else
                        convertedValue = ((GameObject)obj_copy).GetComponent(field.FieldType);
                }
                if (convertedValue != null) {
                    field.SetValue(target, convertedValue);
                    break;
                }
            }
        }
    }
}
#endif