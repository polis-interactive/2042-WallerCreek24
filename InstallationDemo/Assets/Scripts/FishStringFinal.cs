using UnityEngine;

public class FishStringFinal : MonoBehaviour
{

    public float Initialize(
        FusionSplineFinal spline, GameObject fishPrefab, FishStringConfigFinal fishStringConfig, BoundingCollisionFinal collision, string section, int str
    )
    {
        var padding = collision.exitDistance - fishStringConfig.stringLength;
        var downPosition = collision.entryDistance + FishStringConfigFinal.fishHeightInM + UnityEngine.Random.Range(0.0f, 1.0f) * padding;
        for (int i = 0; i < fishStringConfig.fishCount; i++)
        {
            var fish = Instantiate(fishPrefab);
            fish.name = $"Fish {i}";
            fish.transform.parent = transform;
            fish.transform.localPosition = Vector3.back * (downPosition + fishStringConfig.GetIthFishDistance(i));
            var lookAt = spline.LookAtSpline(fish.transform.position);
            fish.transform.rotation = lookAt;
        }
        return downPosition;
    }
}