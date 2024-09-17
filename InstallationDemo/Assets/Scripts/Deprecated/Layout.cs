using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.CoreUtils;

public struct LayoutConfig
{
    public int targetScaffoldingLengthInFeet;
    public int xSpacingInInches;
    public int ySpacingInInches;
    public bool drawSplineGizmos;
    public bool drawScaffoldingGizmos;
    public bool drawSectionGizmos;
    public List<FishStringConfig> fishStrings;
}

[System.Serializable]
public struct FishStringConfig : IComparable<FishStringConfig>
{
    public int fishCount;
    public float stringSpacingInCm;
    public float stringLength
    {
        get
        {

            // for now, fish height is hardcoded to 0.211
            return fishCount * 0.211f + stringSpacingInCm * 0.01f * (fishCount - 1.0f); 
        }
    }

    public float GetIthFishDistance(int i)
    {
        return i * 0.211f + stringSpacingInCm * 0.01f * i;
    }

    public int CompareTo(FishStringConfig other)
    {
        return stringLength.CompareTo(other.stringLength);
    }

}

public class LayoutHelper
{
    public LayoutHelper(List<FishStringConfig> fishStrings)
    {
        _fishStrings = fishStrings;
        _fishStrings.Sort();
        _stringsUsed = new List<int>(Enumerable.Repeat(0, _fishStrings.Count));
    }
    public FishStringConfig SelectFishString(BoundingCollision collision)
    {
        // assume if we get called, doesCollide is true
        maxEntryDistance = Math.Max(maxEntryDistance, collision.entryDistance);
        minEntryDistance = Math.Min(minEntryDistance, collision.entryDistance);
        if (!collision.hitTwice)
        {
            _stringsUsed[0]++;
            return _fishStrings[0];
        }
        var distance = collision.exitDistance;
        maxSpan = Math.Max(maxSpan, distance);
        var chosenString = chooseString(distance);
        if (chosenString != 0)
        {
            chosenString = UnityEngine.Random.Range(0, chosenString + 1);
        }
        _stringsUsed[chosenString]++;
        return _fishStrings[chosenString];
    }

    private int chooseString (float distance) {
        int left = 0;
        int right = _fishStrings.Count - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var midString = _fishStrings[mid];
            if (midString.stringLength == distance)
            {
                return mid;
            }
            if (midString.stringLength < distance)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        if (left < _fishStrings.Count)
        {
            return left;
        }
        return _fishStrings.Count - 1;
    }

    public void PrintStringResults()
    {
        for (int i = 0; i < _fishStrings.Count; i++)
        {
            var fishString = _fishStrings[i];
            var stringsUsed = _stringsUsed[i];
            var fishCount = fishString.fishCount * stringsUsed;
            var fishLabel = $"({fishString.fishCount}, {fishString.stringSpacingInCm}cm, {fishString.stringLength}m)";
            Debug.Log($"String {fishLabel} - Strings {stringsUsed }; Fish {fishCount}");
        }
        Debug.Log($"Total strings used: {_stringsUsed.Sum()}");
        Debug.Log($"Longest entry : {maxEntryDistance}; Shortest entry: {minEntryDistance} Longest span: { maxSpan }");
    }

    private List<FishStringConfig> _fishStrings;
    private List<int> _stringsUsed;
    private float maxEntryDistance = 0.0f;
    private float minEntryDistance = 40.0f;
    private float maxSpan = 0.0f;
}

[ExecuteInEditMode]
public class Layout : MonoBehaviour
{
    [HideInInspector]
    public BoundingVolume boundingVolume;
    [HideInInspector]
    public FusionSpline spline;
    [HideInInspector]
    public Scaffolding scaffolding;
    [HideInInspector]
    public List<ScaffoldingSection> sections;

    [HideInInspector]
    public float xSpacing;
    [HideInInspector]
    public float ySpacing;

    [HideInInspector]
    public LayoutHelper stringHelper;

    public void SetupDensity(int numberOfControlPoints = 30, bool drawGizmos = false)
    {
        Debug.Log("Layout.SetupDensity() running");
        var boundingVolumes = GetComponentsInChildren<BoundingVolume>();
        if (boundingVolumes.Length == 0)
        {
            throw new System.Exception("Layout.SetupDensity() Requires a child that implements BoundingVolume");
        }
        else if (boundingVolumes.Length > 1)
        {
            throw new System.Exception("Layout.SetupDensity() Multiple children implementing BoundingVolume found");
        }
        boundingVolume = boundingVolumes[0];
        var splines = GetComponentsInChildren<FusionSpline>();
        if (splines.Length == 0)
        {
            throw new System.Exception("Layout.SetupFinal() Requires a child that implements Scaffolding");
        }
        else if (splines.Length > 1)
        {
            throw new System.Exception("Layout.SetupFinal() Multiple children implementing Scaffolding found");
        }
        spline = splines[0];
        spline.Setup();
        boundingVolume.Setup(spline, drawGizmos);
        Debug.Log("Layout.SetupDensity() finished");
    }
    public void SetupFinal(LayoutConfig config)
    {
        Debug.Log("InstallationLayout.SetupFinal() running");
        var splines = GetComponentsInChildren<FusionSpline>();
        if (splines.Length == 0)
        {
            throw new System.Exception("Layout.SetupFinal() Requires a child that implements FusionSpline");
        }
        else if (splines.Length > 1)
        {
            throw new System.Exception("Layout.SetupFinal() Multiple children implementing FusionSpline found");
        }
        spline = splines[0];
        spline.Setup(config.drawSplineGizmos);
        var boundingVolumes = GetComponentsInChildren<BoundingVolume>();
        if (boundingVolumes.Length == 0)
        {
            throw new System.Exception("Layout.SetupFinal() Requires a child that implements BoundingVolume");
        }
        else if (boundingVolumes.Length > 1)
        {
            throw new System.Exception("Layout.SetupFinal() Multiple children implementing BoundingVolume found");
        }
        boundingVolume = boundingVolumes[0];
        var scaffoldings = GetComponentsInChildren<Scaffolding>();
        if (scaffoldings.Length == 0)
        {
            throw new System.Exception("Layout.SetupFinal() Requires a child that implements Scaffolding");
        }
        else if (scaffoldings.Length > 1)
        {
            throw new System.Exception("Layout.SetupFinal() Multiple children implementing Scaffolding found");
        }
        scaffolding = scaffoldings[0];
        scaffolding.Setup(spline, config.drawScaffoldingGizmos);
        GenerateSections(config);
        xSpacing = config.xSpacingInInches * 0.0254f;
        ySpacing = config.ySpacingInInches * 0.0254f;
        stringHelper = new LayoutHelper(config.fishStrings);
        Debug.Log("Layout.SetupFinal() finished");
    }
    private void GenerateSections(LayoutConfig config)
    {
        Debug.Log("Layout.GenerateSections() running");
        DestroySections();
        GameObject sectionContainer = new GameObject("Sections");
        sectionContainer.transform.parent = transform.parent;
        sectionContainer.transform.localPosition = Vector3.zero;
        sectionContainer.transform.localRotation = transform.localRotation;
        var newSections = DoGenerateSections(config.targetScaffoldingLengthInFeet);
        int count = 1;
        foreach (var section in newSections)
        {
            GameObject newSection = new GameObject($"Section {count}");
            var scaffoldingSection = newSection.AddComponent<ScaffoldingSection>();
            scaffoldingSection.Initialize(sectionContainer.transform, section, config.drawSectionGizmos);
            sections.Add(scaffoldingSection);
            count += 1;
        }
        Debug.Log("Layout.GenerateSections() finished");
    }

    private void DestroySections()
    {
        Debug.Log("Layout.DestroySections() running");
        sections.Clear();
        Transform sectionContainer = transform.parent.Find("Sections");
        if (sectionContainer != null)
        {
            DestroyImmediate(sectionContainer.gameObject);
        }
        Debug.Log("Layout.DestroySections() finished");
    }
    private List<Section> DoGenerateSections(int targetScaffoldingLengthInFeet)
    {
        Debug.Log("Layout.DoGenerateSections() running");
        var subdividedSections = new List<Section>();
        float targetScaffoldingLengthInMeters = targetScaffoldingLengthInFeet * 0.3048f;
        bool yAxisIsLonger;
        float longestLength, shortestLength;
        var sections = scaffolding.sections;
        foreach (var section in scaffolding.sections)
        {
            section.GetSideLengths(out yAxisIsLonger, out longestLength, out shortestLength);
            int longSubdivisions = Mathf.Max(1, Mathf.RoundToInt(longestLength / targetScaffoldingLengthInMeters));
            int shortSubdivisions = Mathf.Max(1, Mathf.RoundToInt(shortestLength / targetScaffoldingLengthInMeters));
            if (longSubdivisions - shortSubdivisions > 2)
            {
                throw new System.Exception("Scaffolding.SubdivideSections() I don't think the algo will run...");
            }
            // todo: handle triangles when you are sober
            for (int i = 1; i <= longSubdivisions; i++)
            {
                Section lastSection = subdividedSections.Count > 0 ? subdividedSections.Last() : null;
                section.CreateSubsection(
                    i, longSubdivisions, lastSection, yAxisIsLonger, out Section newSection
                );
                subdividedSections.Add(newSection);
            }
        }
        Debug.Log("Layout.DoGenerateSections() finished");
        return subdividedSections;
    }
}
