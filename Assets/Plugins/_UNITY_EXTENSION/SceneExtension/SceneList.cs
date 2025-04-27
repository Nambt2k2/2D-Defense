#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneList", menuName = "Unity Extension/Scene List")]
public class SceneList : ScriptableObject {
    public SceneAsset[] scenes;
}
#endif