using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class SplineData
{
    public List<Vector3Serializable> spline;
}

[System.Serializable]
public class Vector3Serializable
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

public struct PositionDirection
{
    public PositionDirection(Vector3 position, Vector3 direction, Vector3 forward, Vector3 up)
    {
        _position = position;
        _direction = direction;
        _forward = forward;
        _up = up;
    }

    public Vector3 _position { get; }
    public Vector3 _direction { get; }
    public Vector3 _forward { get; }
    public Vector3 _up { get; }
}

[ExecuteInEditMode, RequireComponent(typeof(SplineContainer))]
public class FusionSpline : MonoBehaviour
{
    [HideInInspector]
    public SplineContainer splineContainer;

    public TextAsset jsonFile;

    private bool drawGizmos = false;

    public void Setup(bool _drawGizmos = false)
    {
        Debug.Log("FusionSpline.Setup() running");
        splineContainer = GetComponent<SplineContainer>();
        if (!splineContainer)
        {
            throw new System.Exception("FusionSpline.Setup() SplineContainer component not attached");
        }
        else if (splineContainer.Splines.Count > 1)
        {
            throw new System.Exception("FusionSpline.Setup() SplineContainer must only have one spline");
        }
        if (!jsonFile)
        {
            throw new System.Exception("FusionSpline.Setup() jsonFile is not set");
        }
        SplineData splineData = JsonUtility.FromJson<SplineData>(jsonFile.text);
        if (splineData == null)
        {
            throw new System.Exception("FusionSpline.Setup() failed to parse json file");
        }
        drawGizmos = _drawGizmos;
        var spline = splineContainer.Spline;
        spline.Clear();
        foreach (var vec in splineData.spline)
        {
            spline.Add(vec.ToVector3());
        }
        Debug.Log("FusionSpline.Setup() finished");
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

    public PositionDirection GetPositionDirection(float t, float theta)
    {
        float3 position;
        float3 forward;
        float3 up;
        splineContainer.Spline.Evaluate(t, out position, out forward, out up);
        float3 right = Vector3.Cross(up, forward);
        float radians = Mathf.Deg2Rad * theta;
        float3 direction = math.cos(radians) * right + math.sin(radians) * up;
        forward = math.normalize(forward);
        direction = math.normalize(direction);
        up = math.normalize(up);
        return new PositionDirection(position, direction, forward, up);
    }

    public void GetNearestTAndDirection(float3 point, out float t, out bool isLeft)
    {
        float3 _splinePoint;
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            point,
            out _splinePoint,
            out t
        );
        float3 directionToPoint = math.normalize(point - _splinePoint);
        float3 splineTangent = SplineUtility.EvaluateTangent(splineContainer.Spline, t);
        float angle = math.degrees(math.atan2(directionToPoint.y, directionToPoint.x) - math.atan2(splineTangent.y, splineTangent.x));
        angle -= 90;
        if (angle < 0) angle += 360;
        else if (angle > 360) angle -= 360;
        isLeft = angle > 90 && angle < 270;

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
    }
}
