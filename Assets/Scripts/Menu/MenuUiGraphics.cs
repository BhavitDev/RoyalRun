using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws one lightweight antialiased-in-UI-space polyline from normalized points.</summary>
public sealed class MenuLineGraphic : Graphic
{
    [SerializeField] private List<Vector2> normalizedPoints = new List<Vector2>();
    [SerializeField] private bool closed;
    [SerializeField, Min(0.5f)] private float thickness = 2f;

    public void Configure(IEnumerable<Vector2> points, bool closePath, float lineThickness)
    {
        normalizedPoints = new List<Vector2>(points);
        closed = closePath;
        thickness = Mathf.Max(0.5f, lineThickness);
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        if (normalizedPoints == null || normalizedPoints.Count < 2)
            return;

        Rect rect = GetPixelAdjustedRect();
        int segmentCount = closed ? normalizedPoints.Count : normalizedPoints.Count - 1;
        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 from = ToLocal(rect, normalizedPoints[i]);
            Vector2 to = ToLocal(rect, normalizedPoints[(i + 1) % normalizedPoints.Count]);
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f)
                continue;

            Vector2 side = new Vector2(-direction.y, direction.x).normalized * (thickness * 0.5f);
            int first = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, from - side);
            AddVertex(vertexHelper, from + side);
            AddVertex(vertexHelper, to + side);
            AddVertex(vertexHelper, to - side);
            vertexHelper.AddTriangle(first, first + 1, first + 2);
            vertexHelper.AddTriangle(first, first + 2, first + 3);
        }
    }

    private static Vector2 ToLocal(Rect rect, Vector2 normalized)
    {
        return new Vector2(rect.xMin + normalized.x * rect.width, rect.yMin + normalized.y * rect.height);
    }

    private void AddVertex(VertexHelper helper, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = position;
        helper.AddVert(vertex);
    }
}

/// <summary>Small filled triangle used for the play icon and decorative background marks.</summary>
public sealed class MenuTriangleGraphic : Graphic
{
    [SerializeField] private bool pointsRight;

    public void PointRight(bool value = true)
    {
        pointsRight = value;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 a;
        Vector2 b;
        Vector2 c;

        if (pointsRight)
        {
            a = new Vector2(rect.xMin + rect.width * 0.18f, rect.yMin + rect.height * 0.08f);
            b = new Vector2(rect.xMax - rect.width * 0.08f, rect.center.y);
            c = new Vector2(rect.xMin + rect.width * 0.18f, rect.yMax - rect.height * 0.08f);
        }
        else
        {
            a = new Vector2(rect.center.x, rect.yMax - rect.height * 0.08f);
            b = new Vector2(rect.xMax - rect.width * 0.08f, rect.yMin + rect.height * 0.08f);
            c = new Vector2(rect.xMin + rect.width * 0.08f, rect.yMin + rect.height * 0.08f);
        }

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = a;
        vertexHelper.AddVert(vertex);
        vertex.position = b;
        vertexHelper.AddVert(vertex);
        vertex.position = c;
        vertexHelper.AddVert(vertex);
        vertexHelper.AddTriangle(0, 1, 2);
    }
}
