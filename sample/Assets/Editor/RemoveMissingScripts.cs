using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;

public class FullRemoveMissingScripts
{
    [MenuItem("Tools/Full Remove All Missing Scripts (Scenes & Prefabs)")]
    static void RemoveAllMissingScriptsEverywhere()
    {
        int totalRemoved = 0;

        // 1. Clean all scenes
        string[] scenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
        foreach (string scenePath in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            int removed = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene == scene && go.hideFlags == HideFlags.None)
                {
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                }
            }
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Removed {removed} missing scripts from scene: {scenePath}");
                totalRemoved += removed;
            }
        }

        // 2. Clean all prefabs
        string[] prefabPaths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                if (removed > 0)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    Debug.Log($"Removed {removed} missing scripts from prefab: {prefabPath}");
                    totalRemoved += removed;
                }
            }
        }

        Debug.Log($"Full cleanup complete. Total missing scripts removed: {totalRemoved}");
        AssetDatabase.Refresh();
    }
} 