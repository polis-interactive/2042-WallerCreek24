using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[ExecuteInEditMode, RequireComponent(typeof(MeshFilter))]
public class ScaffoldingSectionFinal : MonoBehaviour
{
    private MeshFilter meshFilter;
    private bool drawScaffolding = false;
    private bool drawGizmos = false;
    private SectionFinal section = null;

    [HideInInspector]
    public float area = 0.0f;

    public void Setup(FusionSplineFinal spline, bool _drawScaffolding, bool _drawGizmos)
    {
        meshFilter = GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            throw new System.Exception("ScaffoldingSectionFinal.Setup() MeshFilter component not attached");
        }
        DestroyFish();
        var corners = Edge.GetMeshCorners(meshFilter.sharedMesh);
        section = new SectionFinal(corners);
        area = section.Area;
        drawScaffolding = _drawScaffolding;
        drawGizmos = _drawGizmos;
    }

    public void DestroyFish()
    {
        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public int CreateFish(GameObject fishPrefab, LayoutFinal layout)
    {
        if (!drawScaffolding)
        {
            GetComponent<Renderer>().enabled = false;
        } else
        {
            GetComponent<Renderer>().enabled = true;
        }
        var position = section.GetRandomPoint(layout.xSpacing, layout.ySpacing);
        var collision = layout.boundingVolume.CheckCollision(transform.TransformPoint(position));
        if (!collision.doesCollide)
        {
            return -1;
        }
        var fishStringConfig = layout.stringHelper.SelectFishString(collision);
        var newString = new GameObject($"String {transform.childCount + 1}");
        newString.transform.parent = transform;
        newString.transform.localPosition = position;
        newString.transform.localRotation = Quaternion.identity;
        var fishString = newString.AddComponent<FishStringFinal>();
        var downPosition = fishString.Initialize(layout.spline, fishPrefab, fishStringConfig, collision, name, transform.childCount);
        layout.SetMinMaxDistance(downPosition);
        return fishStringConfig.fishCount;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || section == null)
        {
            return;
        }
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(section.points[0]), 0.1f);
        Gizmos.color = Color.blue;
        foreach (var point in section.points.Skip(1))
        {
            Gizmos.DrawSphere(transform.TransformPoint(point), 0.1f);
        }

    }
}
