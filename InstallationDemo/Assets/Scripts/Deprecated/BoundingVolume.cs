
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class BoundingCollision {
    public bool doesCollide = false;
    public bool hitTwice = false;
    public float entryDistance = 0.0f;
    public float exitDistance = 0.0f;
}

public struct SplineIds
{
    public SplineIds(Vector3 position, Vector3 forward, Vector3 up, Vector3 right)
    {
        _position = position;
        _forward = forward;
        _up = up;
        _right = right;
    }

    public Vector3 _position { get; }
    public Vector3 _forward { get; }
    public Vector3 _up { get; }
    public Vector3 _right { get; }
}

[ExecuteInEditMode, RequireComponent(typeof(MeshCollider))]
public class BoundingVolume : MonoBehaviour
{
    // vec4 stores east, north, west, south, respectively
    [HideInInspector]
    public List<SplineIds> controlPointDistances = new List<SplineIds>();

    private bool _drawGizmos = false;

    public void Setup(FusionSpline profile, bool drawGizmos = false)
    {
        Debug.Log("BoundingVolume.Setup() running");
        _drawGizmos = drawGizmos;
        controlPointDistances.Clear();
        var count = 30;
        var countf = 1.0f * count;
        for (int i = 0; i <= count; i++)
        {
            float3 position;
            float3 forward;
            float3 up;
            var t = i / countf;
            profile.splineContainer.Spline.Evaluate(t, out position, out forward, out up);
            float3 right = Vector3.Cross(up, forward);
            controlPointDistances.Add(new SplineIds(position, forward, up, right));
        }
        Debug.Log("BoundingVolume.Setup() finished");
    }

    public BoundingCollision CheckCollision(Vector3 position)
    {
        var boundingCollision = new BoundingCollision();
        // for now, always assume the scafolding is directly above the bounding volume
        if (Physics.Raycast(position, Vector3.down, out RaycastHit entryHit, position.y)) {
            boundingCollision.doesCollide = true;
            boundingCollision.entryDistance = entryHit.distance;
            var entryPosition = entryHit.point + 0.1f * Vector3.down;
            if (Physics.Raycast(entryPosition, Vector3.down, out RaycastHit exitHit, entryHit.point.y)) {
                boundingCollision.hitTwice = true;
                boundingCollision.exitDistance = 0.1f + exitHit.distance;
            }
        }
        return boundingCollision;
    }

    void OnDrawGizmos()
    {
        if (!_drawGizmos)
        {
            return;
        }
        foreach (var splineId in controlPointDistances)
        {

            Gizmos.color = Color.blue;
            var forwardRay = new Ray(splineId._position, 3.0f * splineId._forward);
            Gizmos.DrawRay(forwardRay);
            // Vector3 forwardEnd = splineId._position + 3.0f * splineId._forward;
            // Gizmos.DrawLine(splineId._position, forwardEnd);
            // DrawArrowHead(splineId._position, forwardEnd);

            Gizmos.color = Color.green;
            var upRay = new Ray(splineId._position, splineId._up);
            Gizmos.DrawRay(upRay);
            // Vector3 upEnd = splineId._position + splineId._up;
            // Gizmos.DrawLine(splineId._position, upEnd);
            // DrawArrowHead(splineId._position, upEnd);

            Gizmos.color = Color.yellow;
            var rightRay = new Ray(splineId._position, splineId._right);
            Gizmos.DrawRay(rightRay);
            // Vector3 rightEnd = splineId._position + splineId._right;
            // Gizmos.DrawLine(splineId._position, rightEnd);
            // DrawArrowHead(splineId._position, rightEnd);
        }
    }

    private void DrawArrowHead(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        Gizmos.DrawLine(end, end + right * 0.2f);
        Gizmos.DrawLine(end, end + left * 0.2f);
    }
}
