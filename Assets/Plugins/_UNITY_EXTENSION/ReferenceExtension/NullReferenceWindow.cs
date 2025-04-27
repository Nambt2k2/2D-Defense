#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class NullReferenceWindow : EditorWindow {
    Vector2 scrollPos;
    bool needsRepaint = false;
    List<NullReferenceInfo> nullReferences = new List<NullReferenceInfo>();
    bool hasChecked = false;

    struct NullReferenceInfo {
        public string Message;
        public GameObject TargetObject;
    }

    GUIStyle labelStyle;
    GUIStyle headerStyle;

    [MenuItem("_UNITY_EXTENSION/Reference Extension/Null Reference Checker")]
    static void ShowWindow() {
        var window = GetWindow<NullReferenceWindow>("Null References");
        window.position = new Rect(100, 100, 400, 300);
        window.minSize = new Vector2(300, 200);
    }

    void OnEnable() {
        CheckNullReferences();

        // Khởi tạo GUIStyle
        labelStyle = new GUIStyle(EditorStyles.label) {
            fontStyle = FontStyle.Normal,
            normal = { textColor = Color.cyan },
            margin = new RectOffset(10, 10, 2, 2)
        };

        headerStyle = new GUIStyle(EditorStyles.boldLabel) {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.9f, 0.1f) },
            margin = new RectOffset(5, 5, 5, 5)
        };
    }

    void OnGUI() {
        if (!hasChecked) {
            Debug.Log("OnGUI is running - First check");
        }

        EditorGUILayout.BeginVertical("box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (GUILayout.Button("Check Null References", GUILayout.Height(30))) {
            CheckNullReferences();
            needsRepaint = true;
        }

        GUILayout.Space(10);

        if (nullReferences.Count == 0) {
            EditorGUILayout.LabelField("No null references found!", headerStyle);
        } else {
            EditorGUILayout.LabelField($"Found {nullReferences.Count} null references:", headerStyle);
            GUILayout.Space(5);

            var groupedRefs = nullReferences.GroupBy(r => r.TargetObject).ToList();
            foreach (var group in groupedRefs) {
                var targetObject = group.Key;
                var refs = group.ToList();

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"GameObject: {targetObject.name}", headerStyle);
                if (GUILayout.Button("Ping", GUILayout.Width(50))) {
                    EditorGUIUtility.PingObject(targetObject);
                    Selection.activeObject = targetObject;
                }
                EditorGUILayout.EndHorizontal();

                foreach (var nullRef in refs) {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField(nullRef.Message, labelStyle);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void OnInspectorUpdate() {
        if (needsRepaint) {
            Repaint();
            needsRepaint = false;
        }
    }

    void CheckNullReferences() {
        Debug.Log("Checking null references...");
        nullReferences.Clear();
        hasChecked = true;

        var objects = FindObjectsOfType<MonoBehaviour>();
        Debug.Log($"Found {objects.Length} MonoBehaviours");

        if (objects.Length == 0) {
            nullReferences.Add(new NullReferenceInfo { Message = "No MonoBehaviours found!", TargetObject = null });
            return;
        }

        foreach (var obj in objects) {
            var fields = obj.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                                               System.Reflection.BindingFlags.NonPublic |
                                               System.Reflection.BindingFlags.Public);
            var relevantFields = fields.Where(f => f.GetCustomAttributes(typeof(RequireReferenceAttribute), false).Length > 0).ToArray();
            Debug.Log($"Checking {obj.gameObject.name} - Found {fields.Length} fields, {relevantFields.Length} with RequireReference");

            foreach (var field in relevantFields) {
                object value = field.GetValue(obj);
                bool isNull;

                if (value is UnityEngine.Object unityObj) {
                    isNull = !unityObj;
                } else {
                    isNull = value == null;
                }

                Debug.Log($"Field {field.Name} - Value: {(value == null ? "null" : value.ToString())}, IsNull: {isNull}");

                if (isNull) {
                    string nullRef = $"NULL DETECTED: {obj.gameObject.name}.{field.Name}";
                    nullReferences.Add(new NullReferenceInfo {
                        Message = nullRef,
                        TargetObject = obj.gameObject
                    });
                    Debug.LogError($"Added to nullReferences: {nullRef}, Count: {nullReferences.Count}");
                }
            }
        }
    }
}
#endif