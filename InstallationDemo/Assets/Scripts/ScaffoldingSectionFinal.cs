using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Polis.UArtnet.Device;
using System.Net;


[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(Universe))]
public class ScaffoldingSectionFinal : MonoBehaviour
{
    private MeshFilter meshFilter;
    private bool drawScaffolding = false;
    private bool drawGizmos = false;
    private SectionFinal section = null;
    private SectionFinal relativeSection = null;

    [HideInInspector]
    public Universe universe;

    [HideInInspector]
    public float area = 0.0f;
    [HideInInspector]
    public Vector3 center = Vector3.zero;
    [HideInInspector]
    public int sectionNumber;

    void Awake()
    {
        universe = GetComponent<Universe>();
        if (!universe)
        {
            throw new System.Exception($"ScaffoldingSectionFinal.Start(): no universe found");
        }
    }

    public void Setup(FusionSplineFinal spline, bool _drawScaffolding, bool _drawGizmos)
    {
        meshFilter = GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            throw new System.Exception("ScaffoldingSectionFinal.Setup() MeshFilter component not attached");
        }
        universe = GetComponent<Universe>();
        if (!universe)
        {
            throw new System.Exception("ScaffoldingSectionFinal.Setup() Universe component not attached");
        }
        Debug.Log($"Running for {name}");
        DestroyFish();
        sectionNumber = ParseNumberFromName(name);
        SetupUniverse();
        var corners = Edge.GetMeshCorners(meshFilter.sharedMesh);
        OrderCorners(spline, corners, out List<Vector3> orderedCorners);
        section = new SectionFinal(orderedCorners);
        Debug.Log($"{name} - Absolute: {section.GetString()}");
        relativeSection = new SectionFinal(section);
        Debug.Log($"{name} - Relative: {relativeSection.GetString()}");
        area = section.Area;
        center = section.Center;
        drawScaffolding = _drawScaffolding;
        drawGizmos = _drawGizmos;
        if (!drawScaffolding)
        {
            GetComponent<Renderer>().enabled = false;
        }
        else
        {
            GetComponent<Renderer>().enabled = true;
        }
    }

    private void SetupUniverse()
    {
        var universeNumber = sectionNumber - 1;
        var ipBytes = new byte[] { 10, 0, 0, (byte)(100 + universeNumber) };
        var address = new IPAddress(ipBytes);
        universe.Setup(address, universeNumber);
    }

    private int ParseNumberFromName(string name)
    {
        // Regular expression to find digits in the string
        Match match = Regex.Match(name, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int result))
        {
            return result;
        }

        Debug.LogWarning($"Failed to parse number from name: {name}");
        return -1; // Return a default value if parsing fails
    }

    public void DestroyFish()
    {
        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    private void OrderCorners(FusionSplineFinal spline, List<Vector3> corners, out List<Vector3> orderedCorners)
    {
        orderedCorners = new List<Vector3>();
        // map corners to t points; gets t position and leftness
        var tPoints = new List<TPoint>();
        foreach (var corner in corners)
        {
            spline.GetTPoint(transform.TransformPoint(corner), out TPoint tPoint);
            tPoints.Add(tPoint);
        }
        // order points on lowest T
        var sortedTPoints = tPoints.OrderBy(p => p.t).ToList();
        //Debug.Log("[" + String.Join(", ", sortedTPoints.Select(p => p.GetString())) + "]");
        // 0 should be the leftest point
        if (sortedTPoints[0].left < sortedTPoints[1].left)
        {
            (sortedTPoints[0], sortedTPoints[1]) = (sortedTPoints[1], sortedTPoints[0]);
        }
        if (sortedTPoints.Count > 3)
        {
            // 3 should be the leftest point
            if (sortedTPoints[3].left < sortedTPoints[2].left)
            {
                (sortedTPoints[3], sortedTPoints[2]) = (sortedTPoints[2], sortedTPoints[3]);
            }
        }
        orderedCorners = sortedTPoints.Select(tp => transform.InverseTransformPoint(tp.position)).ToList();
    }

    public int CreateFish(GameObject fishPrefab, LayoutFinal layout)
    {
        // why are we in cm
        var useXSpacing = layout.xSpacing * 100.0f;
        var useYSpacing = layout.ySpacing * 100.0f;
        var relativePosition = relativeSection.GetRandomPoint(useXSpacing, useYSpacing);
        var gridX = Mathf.FloorToInt(relativePosition.x / useXSpacing);
        var gridY = Mathf.FloorToInt(relativePosition.y / useYSpacing);
        var position = section.InverseTransform(relativePosition);
        var collision = layout.boundingVolume.CheckCollision(transform.TransformPoint(position));
        if (!collision.doesCollide)
        {
            return -1;
        }
        var fishStringConfig = layout.stringHelper.SelectFishString(collision);
        var newString = new GameObject($"String");
        newString.transform.parent = transform;
        newString.transform.localPosition = position;
        newString.transform.localRotation = Quaternion.LookRotation(section.zAxisDireciton, section.yAxisDirection); ;
        var fishString = newString.AddComponent<FishStringFinal>();
        var downPosition = fishString.Initialize(
            layout.spline, fishPrefab, fishStringConfig, collision, sectionNumber, gridX, gridY
        );
        var centerToPosition = (position - center).magnitude / 100.0f;
        var strLength = downPosition + centerToPosition + 0.65f;
        fishString.SetStrLen(strLength);
        layout.SetStrLen(strLength);
        return fishStringConfig.fishCount;
    }

    public int CountFish()
    {
        return GetComponentsInChildren<FishStringFinal>().Aggregate(0, (acc, fishString) => acc + fishString.fishCount);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || section == null)
        {
            return;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(section.originPoint), 0.1f);
        Gizmos.DrawRay(transform.TransformPoint(section.originPoint), transform.TransformDirection(section.yAxisDirection));
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(section.xAxisPoint), 0.1f);
        Gizmos.DrawRay(transform.TransformPoint(section.originPoint), transform.TransformDirection(section.zAxisDireciton));
        if (!section.isTriangle)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.TransformPoint(section.furthestPoint), 0.1f);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(section.finalPoint), 0.1f);
        Gizmos.DrawRay(transform.TransformPoint(section.originPoint), transform.TransformDirection(section.xAxisDirection));
    }
}
