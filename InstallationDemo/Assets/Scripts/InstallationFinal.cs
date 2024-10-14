using ClosedXML.Excel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


[
    RequireComponent(typeof(InstallationConfig)),
    RequireComponent(typeof(InstallationController)),
    RequireComponent(typeof(ForceRenderRate))
]
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

    public bool drawVolume = false;
    public bool drawSplineGizmos = false;
    public bool drawScaffolding = false;
    public bool drawScaffoldingGizmos = false;

    public StringSelectionStrategy stringSelectionStrategy;

    [SerializeField]
    GameObject fishPrefab;

    private LayoutFinal layout;

    private string manufacturingPath = "Manufacturing";
    private string configPath = Path.Combine("Manufacturing", "Config");
    private string manifestFile = "assembly_manifest.xlsx";

    public void Recreate()
    {
        Debug.Log("InstallationFinal.Recreate() running");
        var layoutConfig = new LayoutFinalConfig();
        layoutConfig.xSpacingInInches = hogwireSpacingXInInches;
        layoutConfig.ySpacingInInches = hogwireSpacingYInInches;
        layoutConfig.fishStrings = fishStrings;
        layoutConfig.stringSelectionStrategy = stringSelectionStrategy;
        layoutConfig.drawVolume = drawVolume;
        layoutConfig.drawSplineGizmos = drawSplineGizmos;
        layoutConfig.drawScaffolding = drawScaffolding;
        layoutConfig.drawScaffoldingGizmos = drawScaffoldingGizmos;
        SetupLayout(layoutConfig);
        GenerateStrings();
        OrderStrings();
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        layout.stringHelper.PrintStringResults();
        layout.PrintLayoutStats();
        Debug.Log("InstallationFinal.Recreate() finished");
    }

    public void GenerateManufacturing()
    {
        Debug.Log("InstallationFinal.GenerateManufacturing() running");
        AssetManager.CreateOrResetFolder(manufacturingPath);
        CreateManifest();
        CreateConfigs();
        Debug.Log("InstallationFinal.GenerateManufacturing() finished");
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

    private void OrderStrings()
    {
        foreach (var section in layout.scaffolding)
        {
            var strings = section.GetComponentsInChildren<FishStringFinal>().ToList();
            strings = strings.OrderBy(s => s.yPosition).ThenBy(s => s.xPosition).ToList();
            for (int i = 0; i < strings.Count; i++)
            {
                strings[i].transform.SetSiblingIndex(i);
                strings[i].SetString(i + 1);
            }
        }
    }

    private void CreateManifest()
    {
        var filePath = AssetManager.PrepareLocation(manufacturingPath, manifestFile);
        using (var workbook = new XLWorkbook())
        {
            var manifestSheet = workbook.Worksheets.Add("Manifest");
            manifestSheet.Cell(1, 2).Value = "Assembled?";
            manifestSheet.Cell(1, 3).Value = "Mounted?";
            manifestSheet.Row(1).Style.Font.Bold = true;
            manifestSheet.Cell(2, 1).Value = "All Sections";
            manifestSheet.Cell(2, 1).Style.Font.Bold = true;
            var manifestRow = 2;
            foreach (var section in layout.scaffolding)
            {
                // add to the manifest file
                manifestRow++;
                manifestSheet.Cell(manifestRow, 1).Value = section.name;
                manifestSheet.Cell(manifestRow, 2).FormulaA1 = $"='{section.name}'!C2";
                manifestSheet.Cell(manifestRow, 3).FormulaA1 = $"='{section.name}'!D2";
                // create section sheet
                var worksheet = workbook.Worksheets.Add(section.name);
                // header
                worksheet.Cell(1, 1).Value = section.name;
                worksheet.Cell(1, 3).Value = "Assembled?";
                worksheet.Cell(1, 4).Value = "Mounted?";
                worksheet.Row(1).Style.Font.Bold = true;
                // titles
                worksheet.Cell(4, 1).Value = "StringNumber";
                worksheet.Cell(4, 2).Value = "FishCount";
                worksheet.Cell(4, 3).Value = "StringSpacingInCm";
                worksheet.Cell(4, 4).Value = "StringToScaffoldingInM";
                worksheet.Cell(4, 5).Value = "Y";
                worksheet.Cell(4, 6).Value = "X";
                worksheet.Cell(4, 7).Value = "Assembled?";
                worksheet.Cell(4, 8).Value = "Mounted?";
                worksheet.Row(4).Style.Font.Bold = true;

                // body
                var row = 4;
                var strings = section.GetComponentsInChildren<FishStringFinal>().ToList();
                foreach (var fishString in strings)
                {
                    row++;
                    var line = fishString.GetManufacturingLine();
                    worksheet.Cell(row, 1).Value = line.stringNumber;
                    worksheet.Cell(row, 2).Value = line.fishCount;
                    worksheet.Cell(row, 3).Value = line.stringSpacingInCm.ToString("F2");
                    worksheet.Cell(row, 4).Value = line.stringToScaffoldingInM.ToString("F2");
                    worksheet.Cell(row, 5).Value = line.yPosition;
                    worksheet.Cell(row, 6).Value = line.xPosition;
                }
                worksheet.Range($"E4:E{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                worksheet.Range($"F4:F{row}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                worksheet.Cell(2, 3).FormulaA1 =
                    $"=IF(COUNTA(G5:G{row}) = 0, \"No...\", IF(COUNTBLANK(G5:G{row}) > 0, \"Somewhat?\", \"YES!\"))";
                worksheet.Cell(2, 4).FormulaA1 =
                    $"=IF(COUNTA(H5:H{row}) = 0, \"No...\", IF(COUNTBLANK(H5:H{row}) > 0, \"Somewhat?\", \"YES!\"))";
                worksheet.Columns().AdjustToContents();
                foreach (var column in worksheet.ColumnsUsed())
                {
                    column.Width += 2;
                }
            }
            manifestSheet.Cell(2, 2).FormulaA1 =
                $"=IF(COUNTIF(B3:B{manifestRow}, \"No...\") = COUNTA(B3:B{manifestRow}), \"No...\", IF(COUNTIF(B3:B{manifestRow}, \"YES!\") = COUNTA(B3:B{manifestRow}), \"YES!\", \"Somewhat?\"))";
            manifestSheet.Cell(2, 3).FormulaA1 =
                $"=IF(COUNTIF(C3:C{manifestRow}, \"No...\") = COUNTA(C3:C{manifestRow}), \"No...\", IF(COUNTIF(C3:C{manifestRow}, \"YES!\") = COUNTA(C3:C{manifestRow}), \"YES!\", \"Somewhat?\"))";
            manifestSheet.Columns().AdjustToContents();
            foreach (var column in manifestSheet.ColumnsUsed())
            {
                column.Width += 2;
            }
            workbook.SaveAs(filePath);
        }
        AssetManager.LoadLocation(manufacturingPath, manifestFile);
    }

    private void CreateConfigs()
    {
        AssetManager.CreateOrResetFolder(configPath);
        foreach (var section in layout.scaffolding)
        {
            var universe = section.universe;
            var config = new ReceiverConfig()
            {
                universe = universe.universe,
                channels = universe.channels,
                strings = section.transform.childCount,
                is_rgbw = true,
                use_dhcp = false,
                local_ip = section.universe.address.ToString()
            };
            AssetManager.WriteOutFile(configPath, config.ToFileName(), JsonUtility.ToJson(config));
        }
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
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Manufacturing Output"))
        {
            installation.GenerateManufacturing();
        }
    }
}