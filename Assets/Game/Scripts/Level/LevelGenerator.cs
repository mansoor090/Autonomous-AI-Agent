using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LevelDimensionBoundary
{
    public Vector2Int dimension;
}

[System.Serializable]
public class LevelData
{
    [FormerlySerializedAs("dimensions")] public Vector2Int dimension;
    public List<PlacedGroupData> groups = new();
}

[System.Serializable]
public class PlacedGroupData
{
    public string typeName;
    public List<Vector3> positions = new();
}


[System.Serializable]
public class PrefabType
{
    public string name;
    public GameObject prefab;
    public float defaultYHeight;
}

[System.Serializable]
public class PlacedPrefab
{
    public Vector3 position;
    public GameObject prefab;
}

[System.Serializable]
public class PlacedPrefabGroup
{
    public string typeName;
    public List<PlacedPrefab> prefabs = new List<PlacedPrefab>();
}

[ExecuteAlways]
public class LevelGenerator : MonoBehaviour
{
    public Camera mainCamera;
    public List<PrefabType> prefabTypes;
    public float gridSize = 1f;
    public Material previewMaterial;

    [SerializeField, HideInInspector] private PrefabType selectedPrefabType;
    private GameObject previewObject;
    public List<PlacedPrefabGroup> placedPrefabGroups = new List<PlacedPrefabGroup>();
    public int selectedPrefabIndex = 0;
    public LevelDimensionBoundary levelDimension;
    [HideInInspector] public string levelName = "MyLevel";
    [ContextMenu("ASD")]
    public void CreateGrid()
    {

        for (int i = 0; i < levelDimension.dimension.x; i++)
        {
            SetSelectedPrefab(0);
            for (int j = 0; j < levelDimension.dimension.y; j++)
            {
                PlacePrefab(new Vector3(i, 0, j));
            }
        }
    }
    void Start()
    {

        if (prefabTypes.Count > 0)
            SetSelectedPrefab(0);
    }

    public void PlacePrefab(Vector3 mouseWorldPos)
    {

        Vector3Int gridPosInt = WorldToGrid(mouseWorldPos);
        Vector3 gridPos = new Vector3(gridPosInt.x, selectedPrefabType.defaultYHeight, gridPosInt.z);

        gridPos.x = Mathf.Clamp(gridPos.x, 0 , levelDimension.dimension.x);
        gridPos.z = Mathf.Clamp(gridPos.z, 0 , levelDimension.dimension.y);
        
        if (!IsPositionOccupied(gridPos))
        {
            // clamp position
            if(gridPos.x < 0)
            {
                gridPos.x = 0;
            }
            if (gridPos.z < 0)
            {
                gridPos.z = 0;
            }
            GameObject obj;
            #if UNITY_EDITOR
            obj = PrefabUtility.InstantiatePrefab(selectedPrefabType.prefab) as GameObject;
            #endif

            #if !UNITY_EDITOR
            obj =             Instantiate(selectedPrefabType.prefab) as GameObject;
            #endif
            
            int childCount = transform.childCount;

            if(childCount > 0)
            {
                bool foundChild = false;
                int childIndex = 0;

                for (int i = 0; i < childCount; i++)
                {
                    if (transform.GetChild(i).name == selectedPrefabType.name)
                    {
                        childIndex = i;
                        foundChild = true;
                    }
                }
                if (foundChild)
                {
                    obj.transform.parent = transform.GetChild(childIndex);
                }
                else
                {
                    GameObject newParent = new GameObject(selectedPrefabType.name);
                    newParent.transform.parent = transform;
                    obj.transform.parent = newParent.transform;
                }
            }

            obj.transform.position = gridPos;

            PlacedPrefabGroup group = placedPrefabGroups.Find(g => g.typeName == selectedPrefabType.name);
            if (group == null)
            {
                group = new PlacedPrefabGroup { typeName = selectedPrefabType.name };
                placedPrefabGroups.Add(group);
            }

            group.prefabs.Add(new PlacedPrefab
            {
                position = gridPos,
                prefab = obj
            });
        }

    }

    public void RemovePrefab(Vector3 mouseWorldPos)
    {
        Vector3Int gridPosInt = WorldToGrid(mouseWorldPos);
        Vector3 gridPos = new Vector3(gridPosInt.x, selectedPrefabType.defaultYHeight, gridPosInt.z);
        
        foreach (var group in placedPrefabGroups)
        {
            var placed = group.prefabs.Find(p => p.position == gridPos);
            if (placed != null)
            {
                if (placed.prefab != null)
                    DestroyImmediate(placed.prefab);
                group.prefabs.Remove(placed);
                break;
            }
        }
    }

    public void UpdatePreview(Vector3 mouseWorldPos)
    {
        if (previewObject == null && selectedPrefabType != null)
        {
            previewObject = Instantiate(selectedPrefabType.prefab);
            if (previewObject.TryGetComponent(out Collider collider))
                collider.enabled = false;
            ApplyPreviewMaterial(previewObject);
        }

        Vector3Int gridPosInt = WorldToGrid(mouseWorldPos);
        Vector3 worldPos = new Vector3(gridPosInt.x, selectedPrefabType != null ? selectedPrefabType.defaultYHeight : 0f, gridPosInt.z);
        worldPos.x = Mathf.Clamp(worldPos.x, 0 , levelDimension.dimension.x);
        worldPos.z = Mathf.Clamp(worldPos.z, 0 , levelDimension.dimension.y);
        
        if (previewObject != null)
        {
            previewObject.transform.position = worldPos;
        }
    }

    public void RemovePreview()
    {
        if (previewObject != null && selectedPrefabType != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }
    }

    private Vector3Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSize);
        int y = 0;
        int z = Mathf.RoundToInt(worldPos.z / gridSize);
        return new Vector3Int(x, y, z);
    }

    private bool IsPositionOccupied(Vector3 gridPos)
    {
        foreach (var group in placedPrefabGroups)
        {
            foreach (var placed in group.prefabs)
            {
                if (placed.position == gridPos)
                    return true;
            }
        }
        return false;
    }

    public void SetSelectedPrefab(int index)
    {
        selectedPrefabIndex = index;
        if (index >= 0 && index < prefabTypes.Count)
        {
            selectedPrefabType = prefabTypes[index];

            if (previewObject != null)
                DestroyImmediate(previewObject);

            previewObject = Instantiate(selectedPrefabType.prefab);
            if (previewObject.TryGetComponent(out Collider collider))
                collider.enabled = false;
            ApplyPreviewMaterial(previewObject);
        }
    }

    void ApplyPreviewMaterial(GameObject obj)
    {
        if (previewMaterial == null) return;

        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            renderer.material = previewMaterial;
        }
    }

    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        foreach (var group in placedPrefabGroups)
        {
            foreach (var placed in group.prefabs)
            {
                if (placed.prefab != null)
                    DestroyImmediate(placed.prefab);
            }
        }
        placedPrefabGroups.Clear();
    }
    
    
    
    
    
    ////////////////////////////////////////
    /// helping methods
    ///
    ///
    /////////////////////////////////////////
    
    
    public List<Vector3> GetAllSimpleNodes()
    {
        List<Vector3> simpleNodes = new List<Vector3>();
        foreach (var group in placedPrefabGroups)
        {
            // Debug.Log("group.typeName" + group.typeName);
            if (group.typeName.ToLower().Contains("simple")) // adjust as needed
            {
                foreach (var placed in group.prefabs)
                {
                    simpleNodes.Add(placed.position);
                }
            }
        }
        return simpleNodes;
    }
    
    public List<Vector3> GetAllWaterNodes()
    {
        List<Vector3> simpleNodes = new List<Vector3>();
        foreach (var group in placedPrefabGroups)
        {
            // Debug.Log("group.typeName" + group.typeName);
            if (group.typeName.ToLower().Contains("lake")) // adjust as needed
            {
                foreach (var placed in group.prefabs)
                {
                    simpleNodes.Add(placed.position);
                }
            }
        }
        return simpleNodes;
    }
    
    
    public List<Vector3> GetAllHurdlesNodes()
    {
        List<Vector3> hurdles = new List<Vector3>();
        foreach (var group in placedPrefabGroups)
        {
            if (group.typeName.ToLower().Contains("hurdle")) // adjust as needed
            {
                foreach (var placed in group.prefabs)
                {
                    hurdles.Add(placed.position);
                }
            }
        }
        return hurdles;
    }
    
    public List<Vector3> GetAllSimpleAndWaterNodes()
    {
        List<Vector3> hurdles = new List<Vector3>();
        foreach (var group in placedPrefabGroups)
        {
            if (group.typeName.ToLower().Contains("simple") || group.typeName.ToLower().Contains("lake")) // adjust as needed
            {
                foreach (var placed in group.prefabs)
                {
                    hurdles.Add(placed.position);
                }
            }
        }
        return hurdles;
    }
    
    public List<Vector3> GetAllHurdleAndWaterNodes()
    {
        List<Vector3> hurdles = new List<Vector3>();
        foreach (var group in placedPrefabGroups)
        {
            if (group.typeName.ToLower().Contains("hurdle") || group.typeName.ToLower().Contains("lake")) // adjust as needed
            {
                foreach (var placed in group.prefabs)
                {
                    hurdles.Add(placed.position);
                }
            }
        }
        return hurdles;
    }
 
    
    private void OnDisable()
    {
#if UNITY_EDITOR
        RemovePreview();
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        RemovePreview();
#endif
    }
    
    
    public void SaveLevel(string fileName)
    {
        LevelData levelData = new();

        levelData.dimension = levelDimension.dimension;
        
        foreach (var group in placedPrefabGroups)
        {
            PlacedGroupData groupData = new() { typeName = group.typeName };
            foreach (var placed in group.prefabs)
            {
                groupData.positions.Add(placed.position);
            }
            levelData.groups.Add(groupData);
        }

        string json = JsonUtility.ToJson(levelData, true);
        System.IO.File.WriteAllText($"Assets/Resources/Levels/{fileName}.json", json);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log($"Level saved: {fileName}");
    }
    
    public void LoadLevel(string fileName, TextMeshProUGUI levelInfo = null)
    {
        ClearAll();

        string path = $"Levels/{fileName}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(path);
        if (jsonAsset == null)
        {
            if (levelInfo != null)
            {
                levelInfo.text = fileName + ": not found";
            }
            Debug.LogError("Level file not found.");
            return;
        }
        else
        {

            if (levelInfo)
            {
                levelInfo.text = fileName;
            }
        }

        LevelData data = JsonUtility.FromJson<LevelData>(jsonAsset.text);
        
        // ✨ restore dimensions
        levelDimension.dimension = data.dimension;
        
        foreach (var group in data.groups)
        {
            int prefabIndex = prefabTypes.FindIndex(p => p.name == group.typeName);
            if (prefabIndex == -1) continue;

            SetSelectedPrefab(prefabIndex);

            foreach (var pos in group.positions)
            {
                PlacePrefab(pos);
            }
        }

        Debug.Log($"Level loaded: {fileName}");
    }
    
}