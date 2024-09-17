
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
public class Side
{
    private Vector3 _left;
    private Vector3 _right;
    public bool IsValid()
    {
        return _left != null && _right != null;
    }
    public Vector3 left
    {
        get
        {
            if (_left.Equals(Vector3.zero))
            {
                throw new System.Exception("Side.left is uninitialized");
            }
            return _left;
        }
        set
        {
            if (!_left.Equals(Vector3.zero))
            {
                throw new System.Exception("Side.left is already set");
            }
            _left = value;
        }
    }
    public Vector3 right
    {
        get
        {
            if (_right.Equals(Vector3.zero))
            {
                throw new System.Exception("Side.right is uninitialized");
            }
            return _right;
        }
        set
        {
            if (!_right.Equals(Vector3.zero))
            {
                throw new System.Exception("Side.right is already set");
            }
            _right = value;
        }
    }
}

public class Section
{
    public Section(Side start, Side end)
    {
        _points.Add(start.left);
        _points.Add(start.right);
        _points.Add(end.right);
        _points.Add(end.left);
    }

    public Section GetRelativeToOrigin(Quaternion rotation)
    {
        var relativePoints = new List<Vector3>();
        relativePoints.Add(Vector3.zero);
        relativePoints.Add(rotation * (xAxisPoint - originPoint));
        relativePoints.Add(rotation * (furthestPoint - originPoint));
        if (!isTriangle)
        {
            relativePoints.Add(rotation * (yAxisPoint - originPoint));
        }
        return new Section(relativePoints);
    }

    private Section(List<Vector3> newPoints)
    {
        _points = newPoints;
    }

    private List<Vector3> _points = new List<Vector3>();
    public bool isTriangle
    {
        get
        {
            return _points.Count == 3;
        }
    }

    public List<Vector3> points
    {
        get
        {
            return _points;
        }
    }
    public Vector3 originPoint
    {
        get
        {
            return _points[0];
        }
    }
    public Vector3 xAxisPoint
    {
        get
        {
            return _points[1];
        }
    }
    public Vector3 yAxisPoint
    {
        get
        {
            return isTriangle ? _points[2] : _points[3];
        }
    }
    public Vector3 furthestPoint
    {
        get
        {
            return isTriangle ? _points[1] : _points[2];
        }
    }
    public Vector3 xAxisLine
    {
        get
        {
            return (xAxisPoint - originPoint);
        }
    }
    public Vector3 yAxisLine
    {
        get
        {
            return (yAxisPoint - originPoint);
        }
    }
    public Vector3 otherSideLine
    {
        get
        {
            return isTriangle ? xAxisPoint : (furthestPoint - xAxisPoint);
        }
    }
    private float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        return Vector3.Cross(ab, ac).magnitude * 0.5f;
    }

    public float Area
    {
        get
        {
            if (isTriangle)
            {
                return TriangleArea(originPoint, xAxisPoint, yAxisPoint);
            } else
            {
                var area1 = TriangleArea(originPoint, xAxisPoint, furthestPoint);
                var area2 = TriangleArea(originPoint, furthestPoint, yAxisPoint);
                return area1 + area2;
            }
        }
    }
    public Vector3 GetRandomPoint(float xSpacing, float ySpacing)
    {
        Vector3 randomPoint;
        if (isTriangle)
        {
            randomPoint = RandomPointInTriangle(originPoint, xAxisPoint, yAxisPoint);
        } else if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f)
        {
            randomPoint = RandomPointInTriangle(originPoint, xAxisPoint, furthestPoint);
        } else
        {
            randomPoint = RandomPointInTriangle(originPoint, furthestPoint, yAxisPoint);
        }
        return SnapToGrid(randomPoint, xSpacing, ySpacing);
    }

    public Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = UnityEngine.Random.Range(0.0f, 1.0f);
        float r2 = UnityEngine.Random.Range(0.0f, 1.0f);
        if (r1 + r2 > 1.0f)
        {
            r1 = 1.0f - r1;
            r2 = 1.0f - r2;
        }
        return (1 - r1 - r2) * a + r1 * b + r2 * c;
    }

    private Vector3 SnapToGrid(Vector3 point, float xSpacing, float ySpacing)
    {
        return new Vector3(
            Mathf.Round(point.x / xSpacing) * xSpacing,
            Mathf.Round(point.y / ySpacing) * ySpacing,
            0
        );
    }

    public void GetSideLengths(out bool yAxisIsLonger, out float longestLength, out float shortestLength)
    {
        var yAxisLength = yAxisLine.magnitude;
        if (isTriangle)
        {
            yAxisIsLonger = true;
            longestLength = yAxisLength;
            shortestLength = 0;
        }
        var otherLength = otherSideLine.magnitude;
        if (yAxisLength >= otherLength)
        {
            yAxisIsLonger = true;
            longestLength = yAxisLength;
            shortestLength = otherLength;
        } else
        {
            yAxisIsLonger = false;
            longestLength = otherLength;
            shortestLength = yAxisLength;
        }
    }
    public void CreateSubsection(
        float subsection, float subsections, Section lastSection, bool yAxisIsLonger, out Section newSection
    )
    {
        var newPoints = new List<Vector3>();
        if (lastSection == null)
        {
            newPoints.Add(originPoint);
            newPoints.Add(xAxisPoint);
        } else
        {
            newPoints.Add(lastSection.yAxisPoint);
            newPoints.Add(lastSection.furthestPoint);
        }
        if (subsection != subsections)
        {
            Vector3 newYAxis;
            Vector3 newOtherSide;
            if (yAxisIsLonger)
            {
                newYAxis = (subsection / subsections) * yAxisLine + originPoint;
                newOtherSide = Project(newYAxis, xAxisPoint, furthestPoint);
                newPoints.Add(newOtherSide);
                newPoints.Add(newYAxis);
            } else
            {
                newOtherSide = (subsection / subsections) * otherSideLine + furthestPoint;
                newYAxis = Project(newOtherSide, originPoint, yAxisPoint);
                newPoints.Add(newOtherSide);
                newPoints.Add(newYAxis);
            }
        } else
        {
            newPoints.Add(furthestPoint);
            newPoints.Add(yAxisPoint);
        }
        newSection = new Section(newPoints);
    }
    private static float ProjectLength(Vector3 point, Vector3 start, Vector3 end, bool useNear)
    {
        var projection = Project(point, start, end);
        if (useNear)
        {
            var nearOut = (projection - start);
            Debug.Log(nearOut);
            return (projection - start).magnitude;
        } else
        {
            var farOut = (end - projection);
            Debug.Log(farOut.ToString());
            return farOut.magnitude;
        }
    }

    public static Vector3 Project(Vector3 point, Vector3 start, Vector3 end)
    {
        var direction = end - start;
        var pointToStart = point - start;
        var lineDirNormalized = direction.normalized;
        float projectionLength = Vector3.Dot(pointToStart, lineDirNormalized);
        return start + lineDirNormalized * projectionLength;
    }
}



[ExecuteInEditMode, RequireComponent(typeof(MeshFilter))]
public class Scaffolding : MonoBehaviour
{
    [HideInInspector]
    public MeshFilter meshFilter;
    [HideInInspector]
    public List<Vector3> vertexList = new List<Vector3>();
    [HideInInspector]
    public List<Section> sections = new List<Section>();

    private bool drawGizmos = false;

    public void Setup(FusionSpline spline, bool _drawGizmos = false)
    {
        meshFilter = GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            throw new System.Exception("FollowProfile._Setup() SplineContainer component not attached");
        }
        drawGizmos = _drawGizmos;
        GetBoundaryPoints();
        sections = GenerateSections(spline);
        
    }

    private void GetBoundaryPoints()
    {
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        var vertices = mesh.vertices;

        vertexList.Clear();
        foreach (var vertex in vertices)
        {
            vertexList.Add(vertex);

        }
        vertexList = vertexList.Distinct().ToList();
    }

    private List<Section> GenerateSections(FusionSpline spline)
    {
        var parallels = new Dictionary<int, Side>();
        foreach (var point in vertexList)
        {
            float t;
            bool isLeft;
            spline.GetNearestTAndDirection(point, out t, out isLeft);
            var t_norm = Mathf.FloorToInt(t * 6.0f);
            Side side;
            if (parallels.TryGetValue(t_norm, out side))
            {
                if (isLeft)
                {
                    side.left = point;
                } else
                {
                    side.right = point;
                }
            } else
            {
                side = new Side();
                if (isLeft)
                {
                    side.left = point;
                } else
                {
                    side.right = point;
                }
                parallels.Add(t_norm, side);
            }
        }
        var _sections = new List<Section>();
        var orderedParallels = parallels.OrderByDescending(x => -x.Key).ToList();
        for (int i = 0; i < orderedParallels.Count - 1; i++)
        {
            var startSide = orderedParallels[i].Value;
            var endSide = orderedParallels[i + 1].Value;
            var section = new Section(startSide, endSide);
            _sections.Add(section);
        }
        return _sections;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }
        foreach (var section in sections)
        {
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

}
