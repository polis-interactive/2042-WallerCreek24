using UnityEngine;

public class FishStringFinal : MonoBehaviour
{
    [HideInInspector]
    public int sectionNumber;

    [HideInInspector]
    public int stringNumber;

    [HideInInspector]
    public char unit;

    [HideInInspector]
    public int fishCount;

    [HideInInspector]
    public int stringSpacingInCm;

    [HideInInspector]
    public float stringToScaffoldingInM;

    [HideInInspector]
    public int xPosition;

    [HideInInspector]
    public string yPosition;

    [HideInInspector]
    public int strLength;

    public float Initialize(
        FusionSplineFinal spline,
        GameObject fishPrefab,
        FishStringConfigFinal fishStringConfig,
        BoundingCollisionFinal collision,
        int sectionNumber,
        int xPosition,
        int yPosition
    )
    {
        unit = fishStringConfig.unit;
        fishCount = fishStringConfig.fishCount;
        stringSpacingInCm = (int) fishStringConfig.stringSpacingInCm;
        this.xPosition = xPosition;
        if (yPosition >= 0 && yPosition <= 25)
        {
            this.yPosition = ((char)('A' + yPosition)).ToString();
        }
        else
        {
            Debug.LogWarning($"yPosition must be between 0 and 25. {yPosition}");
        }
        this.sectionNumber = sectionNumber;
        var padding = collision.exitDistance - fishStringConfig.stringLength;
        stringToScaffoldingInM = collision.entryDistance + FishStringConfigFinal.fishHeightInM + UnityEngine.Random.Range(0.0f, 1.0f) * padding;
        for (int i = 0; i < fishStringConfig.fishCount; i++)
        {
            var fish = Instantiate(fishPrefab);
            var fishFinal = fish.GetComponent<FishFinal>();
            if (!fishFinal)
            {
                throw new System.Exception($"FishStringFinal.Initialize() prefab {fishPrefab.name} does not implement FishFinal.");
            }
            fish.name = $"Fish {i}";
            fish.transform.parent = transform;
            fish.transform.localPosition = Vector3.forward * (stringToScaffoldingInM + fishStringConfig.GetIthFishDistance(i));
            var lookAt = spline.LookAtSpline(fish.transform.position);
            fish.transform.rotation = lookAt;
        }
        return stringToScaffoldingInM;
    }

    public void SetString(int stringNumber)
    {
        name += $" {stringNumber}";
        this.stringNumber = stringNumber;
        var stringStart = (stringNumber - 1) * 5 * 4;
        var fishes = GetComponentsInChildren<FishFinal>();
        int i = -1;
        foreach (var fish in fishes)
        {
            fish.Setup(stringStart + ++i * 4, 4);
        }
    }

    public void SetStrLen(float rawLength)
    {
        var intLength = Mathf.CeilToInt(rawLength);
        strLength = Mathf.Min(Mathf.Max(intLength, 2), 4);
    }

    public ManufacturingLine GetManufacturingLine()
    {
        var useScaffoldingDistance = Mathf.Round(stringToScaffoldingInM * 100.0f) / 100.0f;
        return new ManufacturingLine(
            sectionNumber, stringNumber, unit, strLength, useScaffoldingDistance, xPosition, yPosition
        );
    }
}