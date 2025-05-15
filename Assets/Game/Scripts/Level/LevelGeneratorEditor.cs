#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    private LevelGenerator generator;
    private bool isDragging = false; // NEW

    private void OnEnable()
    {
        generator = (LevelGenerator)target;
    }

    public void OnSceneGUI()
    {
        Event e = Event.current;

        generator.UpdatePreview(GetMouseWorldPosition());

        if (e.type == EventType.MouseDown && e.button == 0) // Left Click down
        {
            isDragging = true;
            PlaceAtMouse();
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0) // Left Click released
        {
            isDragging = false;
            e.Use();
        }

        if (isDragging && e.type == EventType.MouseDrag) // While dragging
        {
            PlaceAtMouse();
            e.Use();
        }

        if (e.type == EventType.MouseDown && e.button == 1) // Right Click once
        {
            generator.RemovePrefab(GetMouseWorldPosition());
            e.Use();
        }
    }

    private void PlaceAtMouse()
    {
        generator.PlacePrefab(GetMouseWorldPosition());
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return Vector3.zero;
    }


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelGenerator generator = (LevelGenerator)target;
        GUILayout.Space(10);
        GUILayout.Label("Select Prefab Type:", EditorStyles.boldLabel);

        if (generator.prefabTypes != null && generator.prefabTypes.Count > 0)
        {
            string[] options = new string[generator.prefabTypes.Count];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = generator.prefabTypes[i].prefab.name;
            }

            int selected = EditorGUILayout.Popup(generator.selectedPrefabIndex, options);

            if (selected != generator.selectedPrefabIndex)
            {
                generator.SetSelectedPrefab(selected);
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Level IO", EditorStyles.boldLabel);

        generator.levelName = EditorGUILayout.TextField("Level Name", generator.levelName);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("💾 Save Level"))
        {
            generator.SaveLevel(generator.levelName);
        }

        if (GUILayout.Button("📂 Load Level"))
        {
            generator.LoadLevel(generator.levelName);
        }
        GUILayout.EndHorizontal();
    }

    private void OnDisable()
    {
        generator.RemovePreview();
    }
}
#endif