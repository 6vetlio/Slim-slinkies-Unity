using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class CreateMapPrefab : EditorWindow
{
    [MenuItem("Tools/Extract Map Prefab from Original Scene")]
    public static void ExtractMapPrefab()
    {
        string originalScenePath = "Assets/Scenes/SimpleNaturePack_Demo.unity";
        var originalScene = EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        
        GameObject canvasMap = GameObject.Find("Canvas_Map");
        GameObject mapRoot = GameObject.Find("MapRoot");
        GameObject mapSystems = GameObject.Find("MapSystems");
        GameObject vipMarkers = GameObject.Find("VipMarkers_Container");
        
        if (canvasMap != null)
        {
            string prefabPath = "Assets/Prefabs/CanvasMap.prefab";
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            
            PrefabUtility.SaveAsPrefabAsset(canvasMap, prefabPath);
            Debug.Log("Created Canvas_Map prefab at: " + prefabPath);
        }
        
        if (mapRoot != null)
        {
            string prefabPath = "Assets/Prefabs/MapRoot.prefab";
            PrefabUtility.SaveAsPrefabAsset(mapRoot, prefabPath);
            Debug.Log("Created MapRoot prefab at: " + prefabPath);
        }
        
        if (mapSystems != null)
        {
            string prefabPath = "Assets/Prefabs/MapSystems.prefab";
            PrefabUtility.SaveAsPrefabAsset(mapSystems, prefabPath);
            Debug.Log("Created MapSystems prefab at: " + prefabPath);
        }
        
        if (vipMarkers != null)
        {
            string prefabPath = "Assets/Prefabs/VipMarkers.prefab";
            PrefabUtility.SaveAsPrefabAsset(vipMarkers, prefabPath);
            Debug.Log("Created VipMarkers prefab at: " + prefabPath);
        }
        
        Debug.Log("Map prefabs extracted successfully! Now open HyperloopDemo scene.");
    }
}
