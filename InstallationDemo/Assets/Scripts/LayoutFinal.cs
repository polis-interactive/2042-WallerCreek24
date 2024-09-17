using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct LayoutFinalConfig
{
    public int xSpacingInInches;
    public int ySpacingInInches;
    public bool drawSplineGizmos;
    public bool drawScaffolding;
    public bool drawScaffoldingGizmos;
    public List<FishStringConfigFinal> fishStrings;
}

[ExecuteInEditMode]
public class LayoutFinal : MonoBehaviour
{
    [HideInInspector]
    public BoundingVolumeFinal boundingVolume;
    [HideInInspector]
    public FusionSplineFinal spline;
    [HideInInspector]
    public List<ScaffoldingSectionFinal> scaffolding;

    [HideInInspector]
    public float xSpacing;
    [HideInInspector]
    public float ySpacing;

    [HideInInspector]
    public LayoutHelperFinal stringHelper;

    private float minDistance;
    private float maxDistance;
    private List<int> meterBuckets;


    public void Setup(LayoutFinalConfig config)
    {
        Debug.Log("Layout.Setup() running");
        var boundingVolumes = GetComponentsInChildren<BoundingVolumeFinal>();
        if (boundingVolumes.Length == 0)
        {
            throw new System.Exception("Layout.Setup() Requires a child that implements BoundingVolume");
        }
        else if (boundingVolumes.Length > 1)
        {
            throw new System.Exception("Layout.Setup() Multiple children implementing BoundingVolume found");
        }
        boundingVolume = boundingVolumes[0];
        var splines = GetComponentsInChildren<FusionSplineFinal>();
        if (splines.Length == 0)
        {
            throw new System.Exception("Layout.Setup() Requires a child that implements Scaffolding");
        }
        else if (splines.Length > 1)
        {
            throw new System.Exception("Layout.Setup() Multiple children implementing Scaffolding found");
        }
        spline = splines[0];
        spline.Setup(config.drawSplineGizmos);
        SetupScaffoldingSections(config);
        xSpacing = config.xSpacingInInches * 0.0254f;
        ySpacing = config.ySpacingInInches * 0.0254f;
        stringHelper = new LayoutHelperFinal(config.fishStrings);
        minDistance = 40.0f;
        maxDistance = 0.0f;
        meterBuckets = Enumerable.Repeat(0, 3).ToList();
        Debug.Log("Layout.Setup() finished");
    }

    private void SetupScaffoldingSections(LayoutFinalConfig config)
    {
        Debug.Log("Layout.SetupScaffoldingSections() running");
        var scaffoldingTransform = transform.Find("Scaffolding");
        if (scaffoldingTransform == null)
        {
            throw new System.Exception("Layout.SetupScaffoldingSections() No child named Scaffolding found");
        }
        var scaffoldingSections = scaffoldingTransform.GetComponentsInChildren<ScaffoldingSectionFinal>().ToList();
        if (scaffoldingSections.Count != scaffoldingTransform.childCount)
        {
            throw new System.Exception("Layout.SetupScaffoldingSections() Not all children of scafolding implement ScaffoldingSectionFinal");
        }
        Debug.Log($"Scaffolding sections: {scaffoldingSections.Count}");
        scaffolding = scaffoldingSections;
        foreach (var section in scaffolding)
        {
            section.Setup(spline, config.drawScaffolding, config.drawScaffoldingGizmos);
        }
        Debug.Log("Layout.SetupScaffoldingSections() finished");
    }

    public void SetMinMaxDistance(float distance)
    {
        minDistance = Mathf.Min(minDistance, distance);
        maxDistance = Mathf.Max(maxDistance, distance);
        meterBuckets[(int)Mathf.Ceil(distance) - 1] += 1;
    }

    public void PrintMinMax()
    {
        Debug.Log($"Max string length: {maxDistance}, Min String Length {minDistance}");
        var strOut = "";
        for (int i = 0; i < meterBuckets.Count; i++)
        {
            strOut += $"({i} - {i + 1}): {meterBuckets[i]} ";
        }
        Debug.Log(strOut);
    }
}
