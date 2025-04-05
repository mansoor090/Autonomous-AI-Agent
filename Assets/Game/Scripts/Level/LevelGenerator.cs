using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class LevelDimensionBoundary
{
    public Vector2 dimension;
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

            // calculate level boundary 
            if (levelDimension.dimension.x < gridPos.x)
            {
                levelDimension.dimension.x = gridPos.x;
            }

            if (levelDimension.dimension.y < gridPos.z)
            {
                levelDimension.dimension.y = gridPos.z;
            }

            GameObject obj = PrefabUtility.InstantiatePrefab(selectedPrefabType.prefab) as GameObject;
           
           

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

        if (previewObject != null)
        {
            previewObject.transform.position = worldPos;
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
}