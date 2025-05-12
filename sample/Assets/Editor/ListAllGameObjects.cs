using UnityEditor;
using UnityEngine;

public class ListAllGameObjects
{
    [MenuItem("Tools/List All GameObjects In Scene")]
    static void ListGameObjects()
    {
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.scene.isLoaded)
                Debug.Log(go.name);
        }
    }
}