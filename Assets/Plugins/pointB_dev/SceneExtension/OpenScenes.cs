#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class OpenScenes : EditorWindow {
    SceneList sceneList;

    [MenuItem("Tools/Scene/Open Scene")]
    static void ShowWindow() {
        GetWindow<OpenScenes>("Scene Selector");
    }

    void OnGUI() {
        string[] guids = AssetDatabase.FindAssets("t:SceneList");
        if (guids.Length > 0) {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            sceneList = AssetDatabase.LoadAssetAtPath<SceneList>(path);
        }
        GUILayout.Label("Scene Selector", EditorStyles.boldLabel);
        if (sceneList == null || sceneList.scenes == null || sceneList.scenes.Length == 0) {
            EditorGUILayout.HelpBox("No Scene Available", MessageType.Warning);
        } else {
            for (int i = 0; i < sceneList.scenes.Length; i++) {
                var scene = sceneList.scenes[i];
                if (scene != null)
                    if (GUILayout.Button($"{i + 1}. {scene.name}"))
                        OpenScene(scene);
            }
        }
    }

    void OpenScene (SceneAsset sceneAsset) {
		if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            EditorSceneManager.OpenScene(scenePath);

        }
	}
}
#endif