using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteInEditMode]
public class InstallationDensity : MonoBehaviour
{

    private Layout _layout;
    private GameObject _fish;

    public int followControlPoints = 30;
    public int fishCount = 20;
    public int randomSeed = 69;

    [SerializeField]
    GameObject FishPrefab;

    public void Recreate()
    {
        Debug.Log("InstallationDensity.Recreate() running");
        InitChildren(false);
        CreateAllFish();
        Debug.Log("InstallationDensity.Recreate() finished!");
    }

    public void DrawGizoms()
    {
        Debug.Log("InstallationDensity.DrawGizoms() running");
        InitChildren(true);
        DestroyFish();
        Debug.Log("InstallationDensity.DrawGizoms() finished!");
    }

    private void InitChildren(bool drawGizmos)
    {
        Debug.Log("InstallationDensity.InitChildren() running");
        var layouts = GetComponentsInChildren<Layout>();
        if (layouts.Length == 0)
        {
            throw new System.Exception("InstallationDensity.InitChildren() Requires a child that implements Layout");
        }
        else if (layouts.Length > 1)
        {
            throw new System.Exception("InstallationDensity.InitChildren() Multiple children implementing Layout found");
        }
        _layout = layouts[0];
        _layout.SetupDensity(followControlPoints, drawGizmos);
        Debug.Log("InstallationDensity.InitChildren() finished");
    }

    private void DestroyFish()
    {
        Debug.Log("InstallationDensity.DestroyFish() running");
        Transform fishTransform = transform.Find("FishContainer");
        if (fishTransform != null)
        {
            DestroyImmediate(fishTransform.gameObject);
        }
        Debug.Log("InstallationDensity.DestroyFish() finished!");
    }

    private void CreateAllFish()
    {
        Debug.Log("InstallationDensity.CreateAllFish() running");
        DestroyFish();
        GameObject fishContainer = new GameObject("FishContainer");
        fishContainer.transform.parent = transform;
        fishContainer.transform.localPosition = Vector3.zero;

        Random.InitState(randomSeed);
        int madeFish = 0;
        for (int i = 1; i <= fishCount; i++)
        {
            var fish = CreateFish(i);
            if (fish)
            {
                fish.transform.parent = fishContainer.transform;
                madeFish++;
            }
        }
        Debug.Log($"InstallationDensity.CreateAllFish() created {madeFish}");
    }

    private GameObject CreateFish(int i)
    {
        Debug.Log($"InstallationDensity.CreateAllFish({i}) running");
        GameObject maybeFish = null;
        var positionDirection = _layout.spline.GetPositionDirection(
            Random.Range(0.0f, 1.0f),
            Random.Range(0.0f, 360.0f)
        );
        // Debug.DrawRay(positionDirection._position, positionDirection._direction * 100, Color.red, 30.0f);
        if (Physics.Raycast(positionDirection._position, positionDirection._direction, out RaycastHit hit, Mathf.Infinity, ~0))
        {
            var hitObject = hit.transform.gameObject;
            var target = hitObject.GetComponent<BoundingVolume>();
            if (target)
            {
                var placeAtDistance = Random.Range(0.0f, hit.distance);
                var outPosition = positionDirection._position + placeAtDistance * positionDirection._direction;
                maybeFish = Instantiate(FishPrefab);
                maybeFish.name = $"Fish {i}";
                maybeFish.transform.position = outPosition;
                maybeFish.transform.rotation = Quaternion.LookRotation(positionDirection._forward);
            } else
            {
                Debug.LogWarning($"InstallationDensity.CreateAllFish({i}) How did it hit not the bounding Volume? {hit.transform.name}");
            }
        } else
        {
            Debug.LogWarning($"InstallationDensity.CreateAllFish({i}) We didn't hit the bounding volume?");
        }
        Debug.Log($"InstallationDensity.CreateAllFish({i}) finished");
        return maybeFish;
    }
}

[CustomEditor(typeof(InstallationDensity))]
public class InstallationDensityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var installation = (InstallationDensity)target;
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("Draw Gizmos"))
        {
            installation.DrawGizoms();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Recreate Layout"))
        {
            installation.Recreate();
        }
    }
}

