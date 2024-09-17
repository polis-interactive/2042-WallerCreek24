using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class InstallationFinal : MonoBehaviour
{
    public int randomSeed = 69;
    public int targetFishCount = 20;

    public int maxStringsPerSection = 24;

    public int hogwireSpacingXInInches = 4;
    public int hogwireSpacingYInInches = 4;

    public List<FishStringConfigFinal> fishStrings = new List<FishStringConfigFinal>() {
        new FishStringConfigFinal() { fishCount = 2, stringSpacingInCm = 20, stringTailInCm = 15 },
        new FishStringConfigFinal() { fishCount = 2, stringSpacingInCm = 40, stringTailInCm = 15 },
        new FishStringConfigFinal() { fishCount = 3, stringSpacingInCm = 20, stringTailInCm = 15 },
        new FishStringConfigFinal() { fishCount = 3, stringSpacingInCm = 40, stringTailInCm = 15 },
        new FishStringConfigFinal() { fishCount = 4, stringSpacingInCm = 20, stringTailInCm = 15 },
        new FishStringConfigFinal() { fishCount = 4, stringSpacingInCm = 30, stringTailInCm = 15 },
    };


    public bool drawSplineGizmos = false;
    public bool drawScaffolding = false;
    public bool drawScaffoldingGizmos = false;

    [SerializeField]
    GameObject fishPrefab;

    private LayoutFinal layout;

    public void Recreate()
    {
        Debug.Log("InstallationFinal.Setup() running");
        var layoutConfig = new LayoutFinalConfig();
        layoutConfig.xSpacingInInches = hogwireSpacingXInInches;
        layoutConfig.ySpacingInInches = hogwireSpacingYInInches;
        layoutConfig.fishStrings = fishStrings;
        layoutConfig.drawSplineGizmos = drawSplineGizmos;
        layoutConfig.drawScaffolding = drawScaffolding;
        layoutConfig.drawScaffoldingGizmos = drawScaffoldingGizmos;
        SetupLayout(layoutConfig);
        GenerateStrings();
        layout.stringHelper.PrintStringResults();
        layout.PrintMinMax();
    }

    private void SetupLayout(LayoutFinalConfig config)
    {
        Debug.Log("InstallationFinal.SetupLayout() running");
        var layouts = GetComponentsInChildren<LayoutFinal>();
        if (layouts.Length == 0)
        {
            throw new System.Exception("InstallationFinal.SetupLayout() Requires a child that implements Layout");
        }
        else if (layouts.Length > 1)
        {
            throw new System.Exception("InstallationFinal.SetupLayout() Multiple children implementing Layout found");
        }
        layout = layouts[0];
        layout.Setup(config);
        Debug.Log("InstallationFinal.SetupLayout() finished");
    }

    private void GenerateStrings()
    {

        Debug.Log("InstallationFinal.GenerateStrings() running");
        var weights = new WeightHelperFinal(layout.scaffolding, maxStringsPerSection);
        Random.InitState(randomSeed);
        while (weights.totalFish < targetFishCount)
        {
            var useSection = weights.GetSection();
            var fishCount = layout.scaffolding[useSection].CreateFish(fishPrefab, layout);
            if (fishCount > 0)
            {
                weights.AddFish(useSection, fishCount);
            } else
            {
                weights.AddDeadWeight(useSection);
            }
        }
        weights.PrintFishArea();
        Debug.Log("InstallationFinal.GenerateStrings() finished!");
    }
}

[CustomEditor(typeof(InstallationFinal))]
public class InstallationFinalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var installation = (InstallationFinal)target;
        DrawDefaultInspector();
        GUILayout.Space(10);
        if (GUILayout.Button("Recreate Layout"))
        {
            installation.Recreate();
        }
    }
}