using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using static System.Collections.Specialized.BitVector32;

public class FishString : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> fishes = new List<GameObject>();

    public void Initialize(
        FusionSpline spline, GameObject fishPrefab, FishStringConfig fishStringConfig, BoundingCollision collision
    )
    {
        var offset = 0.0f; //Random.Range(-6.0f, 6.0f);
        var downPosition = offset * 0.0254f + collision.entryDistance;
        for (int i = 0; i < fishStringConfig.fishCount; i++)
        {
            var fish = Instantiate(fishPrefab);
            fish.name = $"Fish {i}";
            fish.transform.parent = transform;
            fish.transform.localPosition = Vector3.forward * (downPosition + fishStringConfig.GetIthFishDistance(i));
            var lookAt = spline.LookAtSpline(fish.transform.position);
            fish.transform.rotation = lookAt;
        }
    }
}
