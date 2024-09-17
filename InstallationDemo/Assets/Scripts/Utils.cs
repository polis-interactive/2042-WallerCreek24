using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Edge
{
    public Vector3 v1;
    public Vector3 v2;

    private Edge(Vector3 v1, Vector3 v2)
    {
        if (v1.x < v2.x || (v1.x == v2.x && (v1.y < v2.y || (v1.y == v2.y && v1.z <= v2.z))))
        {
            this.v1 = v1;
            this.v2 = v2;
        }
        else
        {
            this.v1 = v2;
            this.v2 = v1;
        }
    }

    public override string ToString() => $"v1 {v1}; v2 {v2}";

    public override bool Equals(object obj)
    {
        if (!(obj is Edge))
            return false;
        Edge other = (Edge)obj;
        return (v1 == other.v1 && v2 == other.v2) || (v1 == other.v2 && v2 == other.v1);
    }

    public override int GetHashCode()
    {
        return v1.GetHashCode() ^ v2.GetHashCode();
    }

    public bool IsCollinearWith(Edge other)
    {
        Vector3 thisVector = v2 - v1;
        Vector3 otherVector = other.v2 - other.v1;
        var angle = Vector3.Angle(thisVector, otherVector);
        // Debug.Log($"{thisVector} x {otherVector} gives angle {angle}");
        return angle < 0.1f;
    }

    public static List<Edge> GetMeshEdges(Mesh mesh)
    {
        var edges = new List<Edge>();
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            var v1 = mesh.vertices[mesh.triangles[i]];
            var v2 = mesh.vertices[mesh.triangles[i + 1]];
            var v3 = mesh.vertices[mesh.triangles[i + 2]];
            edges.Add(new Edge(v1, v2));
            edges.Add(new Edge(v2, v3));
            edges.Add(new Edge(v3, v1));
        }
        edges = edges.GroupBy(x => x).Where(g => g.Count() == 1).Select(g => g.Key).ToList();
        return edges.ToList();
    }

    public static List<Vector3> GetMeshCorners(Mesh mesh)
    {
        var edges = GetMeshEdges(mesh);
        Dictionary<Vector3, List<Edge>> attachmentDict = new Dictionary<Vector3, List<Edge>>();
        var corners = new List<Vector3>(); 
        foreach (var edge in edges)
        {
            if (attachmentDict.TryGetValue(edge.v1, out List<Edge> v1Connections))
            {
                v1Connections.Add(edge);
            } else
            {
                attachmentDict.Add(edge.v1, new List<Edge>() { edge });
            }
            if (attachmentDict.TryGetValue(edge.v2, out List<Edge> v2Connections))
            {
                v2Connections.Add(edge);
            } else
            {
                attachmentDict.Add(edge.v2, new List<Edge>() { edge });
            }
        }
        foreach (var entry in attachmentDict)
        {
            var connections = entry.Value;
            if (connections.Count != 2)
            {
                throw new System.Exception("Shits not going to work");
            }
            if (!connections[0].IsCollinearWith(connections[1]))
            {
                corners.Add(entry.Key);
            }
        }
        return corners.Distinct().ToList();
    }
}

public class SectionFinal
{
    public SectionFinal(List<Vector3> points)
    {
        this.points = points;
    }

    public  List<Vector3> points;

    public bool isTriangle
    {
        get
        {
            return points.Count == 3;
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
                return TriangleArea(points[0], points[1], points[2]);
            }
            else
            {
                var area1 = TriangleArea(points[0], points[1], points[2]);
                var area2 = TriangleArea(points[0], points[2], points[3]);
                return area1 + area2;
            }
        }
    }

    public Vector3 GetRandomPoint(float xSpacing, float ySpacing)
    {
        Vector3 randomPoint;
        if (isTriangle)
        {
            randomPoint = RandomPointInTriangle(points[0], points[1], points[2]);
        }
        else if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f)
        {
            randomPoint = RandomPointInTriangle(points[0], points[1], points[2]);
        }
        else
        {
            randomPoint = RandomPointInTriangle(points[0], points[2], points[3]);
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
            point.z
        );
    }
}

[System.Serializable]
public struct FishStringConfigFinal : IComparable<FishStringConfigFinal>
{
    public static float fishHeightInM = 0.211f;
    public int fishCount;
    public float stringSpacingInCm;
    public float stringTailInCm;
    public float stringLength
    {
        get
        {

            return fishCount * fishHeightInM + stringSpacingInCm * 0.01f * fishCount + stringTailInCm * 0.01f;
        }
    }

    public float GetIthFishDistance(int i)
    {
        return i * fishHeightInM + stringSpacingInCm * 0.01f * i;
    }

    public int CompareTo(FishStringConfigFinal other)
    {
        return stringLength.CompareTo(other.stringLength);
    }

}

public class BoundingCollisionFinal
{
    public bool doesCollide = false;
    public bool hitTwice = false;
    public float entryDistance = 0.0f;
    public float exitDistance = 0.0f;
}

public class LayoutHelperFinal
{
    public LayoutHelperFinal(List<FishStringConfigFinal> fishStrings)
    {
        _fishStrings = fishStrings;
        _fishStrings.Sort();
        _stringsUsed = new List<int>(Enumerable.Repeat(0, _fishStrings.Count));
    }
    public FishStringConfigFinal SelectFishString(BoundingCollisionFinal collision)
    {
        // assume if we get called, doesCollide is true
        maxEntryDistance = Math.Max(maxEntryDistance, collision.entryDistance);
        minEntryDistance = Math.Min(minEntryDistance, collision.entryDistance);
        if (!collision.hitTwice)
        {
            _stringsUsed[0]++;
            return _fishStrings[0];
        }
        var distance = collision.exitDistance;
        maxSpan = Math.Max(maxSpan, distance);
        var chosenString = chooseString(distance);
        if (chosenString != 0)
        {
            chosenString = UnityEngine.Random.Range(1, chosenString + 1);
        }
        _stringsUsed[chosenString]++;
        return _fishStrings[chosenString];
    }

    private int chooseString(float distance)
    {
        int left = 0;
        int right = _fishStrings.Count - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var midString = _fishStrings[mid];
            if (midString.stringLength == distance)
            {
                return mid;
            }
            if (midString.stringLength < distance)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        if (left < _fishStrings.Count)
        {
            return left;
        }
        return _fishStrings.Count - 1;
    }

    public void PrintStringResults()
    {
        for (int i = 0; i < _fishStrings.Count; i++)
        {
            var fishString = _fishStrings[i];
            var stringsUsed = _stringsUsed[i];
            var fishCount = fishString.fishCount * stringsUsed;
            var fishLabel = $"({fishString.fishCount}, {fishString.stringSpacingInCm}cm, {fishString.stringLength}m)";
            Debug.Log($"String {fishLabel} - Strings {stringsUsed}; Fish {fishCount}");
        }
        Debug.Log($"Total strings used: {_stringsUsed.Sum()}");
        // Debug.Log($"Longest entry : {maxEntryDistance}; shortest entry: {minEntryDistance} Longest span: {maxSpan}");
    }

    private List<FishStringConfigFinal> _fishStrings;
    private List<int> _stringsUsed;
    private float maxEntryDistance = 0.0f;
    private float minEntryDistance = 40.0f;
    private float maxSpan = 0.0f;
}

public class WeightHelperFinal
{
    public WeightHelperFinal(List<ScaffoldingSectionFinal> sections, int maxStringsPerSection)
    {
        _baseWeights = new List<float>();
        _fishCount = new List<int>();
        _strings = new List<int>();
        _deadWeight = new List<int>();
        _useWeights = new List<float>();
        _totalFish = 0;
        _maxStringsPerSection = maxStringsPerSection;
        foreach (var section in sections)
        {
            _totalWeight += section.area;
            _baseWeights.Add(section.area);
            _strings.Add(0);
            _fishCount.Add(1);
            _deadWeight.Add(0);
            _useWeights.Add(_totalWeight);
        }
    }
    private int _maxStringsPerSection;
    private float _totalWeight;
    private List<float> _baseWeights;
    private int _totalFish;
    private List<int> _fishCount;
    private List<int> _strings;
    private List<int> _deadWeight;
    private List<float> _useWeights;

    public int totalFish
    {
        get
        {
            return _totalFish;
        }
    }

    private void UpdateWeights()
    {
        _totalWeight = 0;
        for (int i = 0; i < _baseWeights.Count; i++)
        {
            _totalWeight += _baseWeights[i] / (_fishCount[i] + _deadWeight[i]);
            _useWeights[i] = _totalWeight;
        }
    }
    public void AddFish(int index, int count)
    {
        _fishCount[index] += count;
        _strings[index] += 1;
        if (_strings[index] >= _maxStringsPerSection)
        {
            _deadWeight[index] = (int)_totalWeight;
        }
        _totalFish += count;
        UpdateWeights();
    }
    public void AddDeadWeight(int index)
    {
        _deadWeight[index] += 1;
        UpdateWeights();
    }
    public int GetSection()
    {
        var weight = UnityEngine.Random.Range(0.0f, _totalWeight);
        int low = 0;
        int high = _useWeights.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (_useWeights[mid] == weight)
            {
                return mid;
            }
            else if (_useWeights[mid] < weight)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        return low;
    }

    public void PrintFishArea()
    {
        for (int i = 0; i < _baseWeights.Count; i++)
        {
            Debug.Log($"Section {i + 1} - Area: {_baseWeights[i]}; Strings {_strings[i]} Fish {_fishCount[i] - 1}");
        }
        Debug.Log($"Total fish: {_totalFish}");
    }
}
