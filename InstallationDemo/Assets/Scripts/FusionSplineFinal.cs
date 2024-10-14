using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;


[System.Serializable]
public class FusionSplineKnot
{
    public float x;
    public float y;
    public float z;

    // Convert the custom class to Unity's Vector3
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

public class ToDraw
{
    public ToDraw(Vector3 position, Vector3 direction, UnityEngine.Color color)
    {
        this.position = position;
        this.direction = direction;
        this.color = color;
    }
    public Vector3 position;
    public Vector3 direction;
    public UnityEngine.Color color;
}

[System.Serializable]
public class FusionSplineData
{
    public List<FusionSplineKnot> spline;
}

public class FusionSplineFinal : MonoBehaviour
{
    [HideInInspector]
    public SplineContainer splineContainer;

    public TextAsset jsonFile;

    private bool drawGizmos = false;

    private List<ToDraw> toDraw = new List<ToDraw>();

    public void Setup(bool _drawGizmos = false)
    {
        Debug.Log("FusionSplineFinal.Setup() running");
        splineContainer = GetComponent<SplineContainer>();
        if (!splineContainer)
        {
            throw new System.Exception("FusionSplineFinal.Setup() SplineContainer component not attached");
        }
        else if (splineContainer.Splines.Count > 1)
        {
            throw new System.Exception("FusionSplineFinal.Setup() SplineContainer must only have one spline");
        }
        if (!jsonFile)
        {
            throw new System.Exception("FusionSplineFinal.Setup() jsonFile is not set");
        }
        FusionSplineData splineData = JsonUtility.FromJson<FusionSplineData>(jsonFile.text);
        if (splineData == null)
        {
            throw new System.Exception("FusionSplineFinal.Setup() failed to parse json file");
        }
        drawGizmos = _drawGizmos;
        var spline = splineContainer.Spline;
        spline.Clear();
        foreach (var vec in splineData.spline)
        {
            spline.Add(vec.ToVector3());
        }
        toDraw = new List<ToDraw>();
        Debug.Log("FusionSplineFinal.Setup() finished");
    }

    public void GetTPoint(Vector3 position, out TPoint tPoint)
    {
        Vector3 localPosition = splineContainer.transform.InverseTransformPoint(position);
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            localPosition,
            out float3 splinePoint,
            out float t
        );
        SplineUtility.Evaluate(splineContainer.Spline, t, out splinePoint, out float3 tangent, out float3 up);
        up = Quaternion.AngleAxis(90, tangent) * up;
        var left = Vector3.Cross(tangent, up).normalized;
        // toDraw.Add(new ToDraw(transform.TransformPoint(splinePoint), transform.TransformPoint(tangent), UnityEngine.Color.green));
        // toDraw.Add(new ToDraw(transform.TransformPoint(splinePoint), transform.TransformPoint(up), UnityEngine.Color.red));
        // toDraw.Add(new ToDraw(transform.TransformPoint(splinePoint), transform.TransformPoint(left), UnityEngine.Color.blue));
        var useSplinePoint = new Vector3(splinePoint.x, splinePoint.y, splinePoint.z);
        var leftActual = localPosition - useSplinePoint;
        var leftVal = Vector3.Dot(left, leftActual);
        tPoint = new TPoint(position, t, leftVal);
    }

    public Quaternion LookAtSpline(Vector3 position)
    {
        Vector3 localPosition = splineContainer.transform.InverseTransformPoint(position);
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            localPosition,
            out float3 _splinePoint,
            out float t
        );
        var tangent = SplineUtility.EvaluateTangent(splineContainer.Spline, t);
        var worldTangent = splineContainer.transform.TransformDirection(tangent);
        return Quaternion.LookRotation(worldTangent, Vector3.up);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || splineContainer == null)
        {
            return;
        }
        var position = splineContainer.Spline.EvaluatePosition(0.0f);
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawSphere(transform.TransformPoint(position), 0.1f);
        position = splineContainer.Spline.EvaluatePosition(1.0f);
        Gizmos.color = UnityEngine.Color.blue;
        Gizmos.DrawSphere(transform.TransformPoint(position), 0.1f);
        foreach (var r in toDraw)
        {
            Gizmos.color = r.color;
            Gizmos.DrawRay(r.position, r.direction);
        }
    }
}
