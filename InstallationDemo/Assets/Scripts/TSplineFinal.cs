using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;


[RequireComponent(typeof(SplineContainer))]
public class TSplineFinal : MonoBehaviour
{
    [HideInInspector]
    public SplineContainer splineContainer;

    public TextAsset jsonFile;

    private bool drawGizmos = false;

    public void Setup(bool _drawGizmos = false)
    {
        Debug.Log("TSplineFinal.Setup() running");
        splineContainer = GetComponent<SplineContainer>();
        if (!splineContainer)
        {
            throw new System.Exception("TSplineFinal.Setup() SplineContainer component not attached");
        }
        else if (splineContainer.Splines.Count > 1)
        {
            throw new System.Exception("TSplineFinal.Setup() SplineContainer must only have one spline");
        }
        if (!jsonFile)
        {
            throw new System.Exception("TSplineFinal.Setup() jsonFile is not set");
        }
        FusionSplineData splineData = JsonUtility.FromJson<FusionSplineData>(jsonFile.text);
        if (splineData == null)
        {
            throw new System.Exception("TSplineFinal.Setup() failed to parse json file");
        }
        drawGizmos = _drawGizmos;
        var spline = splineContainer.Spline;
        spline.Clear();
        foreach (var vec in splineData.spline)
        {
            spline.Add(vec.ToVector3());
        }
        Debug.Log("TSplineFinal.Setup() finished");
    }

    public void GetParameters(
       Vector3 position, out float tValue, out float rValue, out float thetaValue
    )
    {
        splineContainer = GetComponent<SplineContainer>();
        Vector3 localPosition = splineContainer.transform.InverseTransformPoint(position);
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            localPosition,
            out float3 splinePoint,
            out tValue
        );
        var useSplinePoint = new Vector3(splinePoint.x, splinePoint.y, splinePoint.z);
        var rVec = localPosition - useSplinePoint;
        rValue = rVec.magnitude;
        SplineUtility.Evaluate(splineContainer.Spline, tValue, out _, out float3 tangent, out float3 up);
        up = Quaternion.AngleAxis(90, tangent) * up;
        var right = Vector3.Cross(up, tangent).normalized;
        // set theta value, an angle in degrees with right as 0 degrees
        thetaValue = Vector3.SignedAngle(right, rVec, tangent);
        if (thetaValue < 0)
        {
            thetaValue += 360;
        }
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmos || splineContainer == null)
        {
            return;
        }
        var position = splineContainer.Spline.EvaluatePosition(0.0f);
        Gizmos.color = UnityEngine.Color.magenta;
        Gizmos.DrawSphere(transform.TransformPoint(position), 0.1f);
        position = splineContainer.Spline.EvaluatePosition(1.0f);
        Gizmos.color = UnityEngine.Color.yellow;
        Gizmos.DrawSphere(transform.TransformPoint(position), 0.1f);
    }
}
