using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.CoreUtils;

public class WeightHelper
{
    public WeightHelper(List<ScaffoldingSection> sections)
    {
        _baseWeights = new List<float>();
        _fishCount = new List<int>();
        _useWeights = new List<float>();
        _totalFish = 0;
        foreach (var section in sections)
        {
            _totalWeight += section.area;
            _baseWeights.Add(section.area);
            _fishCount.Add(1);
            _useWeights.Add(_totalWeight);
        }
    }
    private float _totalWeight;
    private List<float> _baseWeights;
    private int _totalFish;
    private List<int> _fishCount;
    private List<float> _useWeights;

    public int totalFish
    {
        get
        {
            return _totalFish;
        }
    }
    
    private void UpdateWeights()
    {
        _totalWeight = 0;
        for (int i = 0; i < _baseWeights.Count; i++)
        {
            _totalWeight += _baseWeights[i] / _fishCount[i];
            _useWeights[i] = _totalWeight;
        }
    }
    public void AddFish(int index, int count)
    {
        _fishCount[index] += count;
        _totalFish += count;
        UpdateWeights();
    }
    public int GetSection()
    {
        var weight = Random.Range(0.0f, _totalWeight);
        int low = 0;
        int high = _useWeights.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (_useWeights[mid] == weight)
            {
                return mid;
            }
            else if (_useWeights[mid] < weight)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        return low;
    }

    public void PrintFishArea()
    {
        for (int i = 0; i < _baseWeights.Count; i++)
        {
            Debug.Log($"Section {i + 1} - Area: {_baseWeights[i]}; Fish {_fishCount[i] - 1}");
        }
        Debug.Log($"Total fish: {_totalFish}");
    }
}

[ExecuteInEditMode]
public class InstallationLayout : MonoBehaviour
{

    private Layout _layout;

    public int randomSeed = 69;
    public int targetFishCount = 20;

    public int targetScaffoldingLengthInFeet = 4;
    public int hogwireSpacingXInInches = 4;
    public int hogwireSpacingYInInches = 4;

    public List<FishStringConfig> fishStrings = new List<FishStringConfig>() {
        new FishStringConfig() { fishCount = 2, stringSpacingInCm = 20 },
        new FishStringConfig() { fishCount = 2, stringSpacingInCm = 40 },
        new FishStringConfig() { fishCount = 3, stringSpacingInCm = 20 },
        new FishStringConfig() { fishCount = 3, stringSpacingInCm = 40 },
        new FishStringConfig() { fishCount = 4, stringSpacingInCm = 20 },
        new FishStringConfig() { fishCount = 4, stringSpacingInCm = 30 },
    };

    public bool drawSplineGizmos = false;
    public bool drawScaffoldingGizmos = false;
    public bool drawSectionGizmos = false;

    [SerializeField]
    GameObject fishPrefab;

    public void Recreate()
    {
        Debug.Log("InstallationLayout.Recreate() running");

        var layoutConfig = new LayoutConfig();
        layoutConfig.targetScaffoldingLengthInFeet = targetScaffoldingLengthInFeet;
        layoutConfig.xSpacingInInches = hogwireSpacingXInInches;
        layoutConfig.ySpacingInInches = hogwireSpacingYInInches;
        layoutConfig.drawSplineGizmos = drawSplineGizmos;
        layoutConfig.drawScaffoldingGizmos = drawScaffoldingGizmos;
        layoutConfig.drawSectionGizmos = drawSectionGizmos;
        layoutConfig.fishStrings = fishStrings;

        InitChildren(layoutConfig);
        GenerateStrings();
        _layout.stringHelper.PrintStringResults();
        Debug.Log("InstallationLayout.Recreate() finished!");
    }


    private void InitChildren(LayoutConfig config)
    {
        Debug.Log("InstallationLayout.InitChildren() running");
        var layouts = GetComponentsInChildren<Layout>();
        if (layouts.Length == 0)
        {
            throw new System.Exception("InstallationLayout.InitChildren() Requires a child that implements Layout");
        }
        else if (layouts.Length > 1)
        {
            throw new System.Exception("InstallationLayout.InitChildren() Multiple children implementing Layout found");
        }
        _layout = layouts[0];
        _layout.SetupFinal(config);
        Debug.Log("InstallationLayout.InitChildren() finished");
    }

    private void GenerateStrings()
    {
        Debug.Log("InstallationLayout.GenerateStrings() running");
        var weights = new WeightHelper(_layout.sections);
        Random.InitState(randomSeed);
        while (weights.totalFish < targetFishCount)
        {
            var useSection = weights.GetSection();
            var fishCount = _layout.sections[useSection].CreateFishString(fishPrefab, _layout);
            if (fishCount != 0)
            {
                weights.AddFish(useSection, fishCount);
            }
        }
        weights.PrintFishArea();
        Debug.Log("InstallationLayout.GenerateStrings() finished!");
    }
}

[CustomEditor(typeof(InstallationLayout))]
public class InstallationLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var installation = (InstallationLayout)target;
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("Recreate Layout"))
        {
            installation.Recreate();
        }
    }
}
