using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.Rendering.CoreUtils;

public class ScaffoldingSection : MonoBehaviour
{
    [HideInInspector]
    public Section section = null;
    [HideInInspector]
    public float area = 0.0f;
    [HideInInspector]
    public List<GameObject> strings = new List<GameObject>();

    private bool drawGizmos = false;

    public void Initialize(Transform parentTransform, Section _section, bool _drawGizmos)
    {
        transform.parent = parentTransform;
        transform.localPosition = _section.originPoint;
        Vector3 yAxis = _section.yAxisLine.normalized;
        float angleY = Mathf.Atan2(yAxis.x, yAxis.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(270 - angleY, 270f, 0f);
        //Debug.Log(yAxis);
        //Debug.Log(angleY);
        //Debug.Log(rotation);
        //Debug.Log(Quaternion.LookRotation(yAxis, Vector3.up).eulerAngles);
        // TODO: this is litterally rigged
        transform.localRotation = rotation * Quaternion.Inverse(parentTransform.rotation);
        section = _section.GetRelativeToOrigin(Quaternion.Inverse(transform.localRotation));
        drawGizmos = _drawGizmos;
        area = _section.Area;
    }
    
    public int CreateFishString(GameObject fishPrefab, Layout layout)
    {
        var position = section.GetRandomPoint(layout.xSpacing, layout.ySpacing);
        var collision = layout.boundingVolume.CheckCollision(transform.TransformPoint(position));
        if (!collision.doesCollide)
        {
            return 0;
        }
        // z is down, thanks unity
        // position.z += collision.entryDistance;
        var fishStringConfig = layout.stringHelper.SelectFishString(collision);
        var newString = new GameObject($"String {strings.Count + 1}");
        newString.transform.parent = transform;
        newString.transform.localPosition = position;
        newString.transform.localRotation = Quaternion.identity;
        var fishString = newString.AddComponent<FishString>();
        fishString.Initialize(layout.spline, fishPrefab, fishStringConfig, collision);
        strings.Add(newString);
        return fishStringConfig.fishCount;
    }

    private void OnDrawGizmos()
    {
        if (section == null || !drawGizmos)
        {
            return;
        }
        var originPoint = transform.TransformPoint(section.originPoint);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(originPoint, 0.1f);
        var xPoint = transform.TransformPoint(section.xAxisPoint);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(originPoint, xPoint);
        var yPoint = transform.TransformPoint(section.yAxisPoint);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(originPoint, yPoint);
        if (!section.isTriangle)
        {
            var farthestPoint = transform.TransformPoint(section.furthestPoint);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(originPoint, farthestPoint);
        }
    }
}
