using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class Room : MonoBehaviour
{
    public int xCount = 4;
    public int yCount = 2;
    public int zCount = 3;
    public float spacing = 1;
    [SerializeField] GameObject prefab;

    private List<GameObject> _instances = new List<GameObject>();

    private void OnEnable()
    {
        Recreate();
    }

    public void Recreate()
    {
        foreach (var child in _instances)
        {
            DestroyImmediate(child);
        }
        _instances.Clear();
        var midPoint = new Vector3((xCount - 1) / 2.0f, (yCount - 1) / 2.0f, (zCount - 1) / 2.0f);
        int count = 1;
        for (int y = 0; y < yCount; y++)
        {
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    var instance = Instantiate(prefab);
                    instance.transform.parent = gameObject.transform;
                    instance.transform.position = new Vector3(x * spacing, y * spacing, z * spacing);
                    instance.transform.LookAt(midPoint);
                    instance.name = $"Fish {count++} ({x}, {y}, {z})";
                    _instances.Add(instance);
                }
            }
        }
    }
}

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var room = (Room)target;
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            room.Recreate();
        }
    }
}

