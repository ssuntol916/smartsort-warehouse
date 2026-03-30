// ============================================================
// 파일명  : MeshGeometryPicker.cs
// 역할    : Scene View에서 메쉬 면·엣지 선택을 위한 지오메트리 유틸리티
//           - 레이캐스트로 삼각형 인덱스 탐색
//           - BFS 코플래너 확장으로 동일 Face 삼각형 추출
//           - Face → Plane(pointA, pointB, pointC) 변환
//           - 경계 엣지(Boundary Edge) 추출
//           - 마우스 근접 엣지 탐색
//           - Edge → Line(pointA, pointB) 변환
// 작성자  :
// 작성일  : 2026-03-30
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메쉬 면(Face)·엣지(Edge) 선택에 필요한 지오메트리 계산 유틸리티.
/// Editor 전용이며, Runtime 코드에 의존하지 않는다.
/// </summary>
public static class MeshGeometryPicker
{
    // 동일 평면(코플래너) 판단 기준 각도 허용 오차 (도)
    private const float CoplanarAngleTolerance = 1f;

    // ─── 레이캐스트 ───────────────────────────────────────────

    /// <summary>
    /// 월드 레이를 MeshFilter 배열에 레이캐스트하여 가장 가까운 삼각형 인덱스를 반환한다.
    /// </summary>
    /// <param name="ray">월드 공간 레이</param>
    /// <param name="candidates">대상 MeshFilter 배열</param>
    /// <param name="hitFilter">충돌한 MeshFilter (없으면 null)</param>
    /// <param name="triangleIndex">충돌한 삼각형 인덱스 (없으면 -1)</param>
    /// <returns>충돌했으면 true</returns>
    public static bool TryRaycastTriangle(
        Ray ray,
        MeshFilter[] candidates,
        out MeshFilter hitFilter,
        out int triangleIndex)
    {
        hitFilter = null;
        triangleIndex = -1;
        float nearestDist = float.MaxValue;

        foreach (MeshFilter mf in candidates)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Transform t = mf.transform;
            int[] tris = mesh.triangles;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = t.TransformPoint(verts[tris[i]]);
                Vector3 b = t.TransformPoint(verts[tris[i + 1]]);
                Vector3 c = t.TransformPoint(verts[tris[i + 2]]);

                if (RayIntersectsTriangle(ray, a, b, c, out float dist) &&
                    dist < nearestDist)
                {
                    nearestDist = dist;
                    hitFilter = mf;
                    triangleIndex = i / 3;
                }
            }
        }
        return hitFilter != null;
    }

    // ─── 코플래너 면 확장 ─────────────────────────────────────

    /// <summary>
    /// 씨드 삼각형으로부터 법선이 유사한(≤CoplanarAngleTolerance°) 인접 삼각형을
    /// BFS로 확장하여 동일 Face의 삼각형 인덱스 목록을 반환한다.
    /// </summary>
    /// <param name="mesh">대상 메쉬</param>
    /// <param name="seedTriangleIndex">시작 삼각형 인덱스</param>
    /// <returns>동일 Face에 속하는 삼각형 인덱스 목록</returns>
    public static List<int> GetCoplanarFace(Mesh mesh, int seedTriangleIndex)
    {
        if (mesh == null) return new List<int>();

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        int triCount = tris.Length / 3;
        Vector3 seedNormal = GetTriangleNormal(verts, tris, seedTriangleIndex);

        var adjacency = BuildAdjacency(tris, triCount);
        var result = new List<int>();
        var visited = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(seedTriangleIndex);
        visited.Add(seedTriangleIndex);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            result.Add(current);

            if (!adjacency.TryGetValue(current, out List<int> neighbors)) continue;
            foreach (int neighbor in neighbors)
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);

                Vector3 neighborNormal = GetTriangleNormal(verts, tris, neighbor);
                if (Vector3.Angle(seedNormal, neighborNormal) < CoplanarAngleTolerance)
                    queue.Enqueue(neighbor);
            }
        }
        return result;
    }

    // ─── 면 → Plane 변환 ─────────────────────────────────────

    /// <summary>
    /// 삼각형 인덱스 목록으로부터 Plane(pointA, pointB, pointC)을 생성한다.
    /// 첫 번째 삼각형의 세 꼭짓점(월드 좌표)을 사용한다.
    /// </summary>
    /// <param name="mesh">대상 메쉬</param>
    /// <param name="meshTransform">메쉬 오브젝트의 Transform (로컬 → 월드 변환용)</param>
    /// <param name="triangleIndices">Face를 구성하는 삼각형 인덱스 목록</param>
    /// <returns>생성된 Plane 객체</returns>
    public static Plane FaceToPlane(Mesh mesh, Transform meshTransform, List<int> triangleIndices)
    {
        if (mesh == null || triangleIndices == null || triangleIndices.Count == 0)
            return new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        int triIdx = triangleIndices[0];

        Vector3 a = meshTransform.TransformPoint(verts[tris[triIdx * 3]]);
        Vector3 b = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 1]]);
        Vector3 c = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 2]]);

        return new Plane(a, b, c);
    }

    // ─── 경계 엣지 추출 ──────────────────────────────────────

    /// <summary>
    /// 삼각형 목록의 경계 엣지(한 삼각형에만 속하는 엣지)를 월드 좌표 쌍으로 반환한다.
    /// </summary>
    /// <param name="mesh">대상 메쉬</param>
    /// <param name="meshTransform">메쉬 오브젝트의 Transform</param>
    /// <param name="triangleIndices">Face를 구성하는 삼각형 인덱스 목록</param>
    /// <returns>경계 엣지 목록 (월드 좌표 두 점 쌍)</returns>
    public static List<(Vector3, Vector3)> GetBoundaryEdges(
        Mesh mesh,
        Transform meshTransform,
        List<int> triangleIndices)
    {
        if (mesh == null || triangleIndices == null)
            return new List<(Vector3, Vector3)>();

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        var edgeCount = new Dictionary<(int, int), int>();

        foreach (int triIdx in triangleIndices)
        {
            int i0 = tris[triIdx * 3];
            int i1 = tris[triIdx * 3 + 1];
            int i2 = tris[triIdx * 3 + 2];
            AddEdge(edgeCount, i0, i1);
            AddEdge(edgeCount, i1, i2);
            AddEdge(edgeCount, i2, i0);
        }

        var boundary = new List<(Vector3, Vector3)>();
        foreach (var kv in edgeCount)
        {
            if (kv.Value == 1)  // 경계 엣지: 한 삼각형에만 소속
            {
                Vector3 a = meshTransform.TransformPoint(verts[kv.Key.Item1]);
                Vector3 b = meshTransform.TransformPoint(verts[kv.Key.Item2]);
                boundary.Add((a, b));
            }
        }
        return boundary;
    }

    // ─── 엣지 근접 탐색 ──────────────────────────────────────

    /// <summary>
    /// 마우스 위치(GUI 좌표계)에 가장 가까운 경계 엣지를 반환한다.
    /// </summary>
    /// <param name="mousePosition">GUI 좌표계 마우스 위치 (SceneView Event.mousePosition)</param>
    /// <param name="edges">월드 좌표 경계 엣지 목록</param>
    /// <param name="camera">Scene View 카메라</param>
    /// <param name="screenRadius">픽셀 단위 근접 감지 반경</param>
    /// <param name="nearestEdge">가장 가까운 엣지 (못 찾으면 default)</param>
    /// <returns>근접 엣지를 찾았으면 true</returns>
    public static bool TryGetNearestEdge(
        Vector2 mousePosition,
        List<(Vector3, Vector3)> edges,
        Camera camera,
        float screenRadius,
        out (Vector3, Vector3) nearestEdge)
    {
        nearestEdge = default;
        if (edges == null || edges.Count == 0) return false;

        // GUI 좌표계(y 하향)를 스크린 좌표계(y 상향)로 변환
        Vector2 mouseScreen = new Vector2(mousePosition.x,
                                          camera.pixelHeight - mousePosition.y);
        float nearestDist = float.MaxValue;
        bool found = false;

        foreach (var edge in edges)
        {
            Vector2 sA = (Vector2)camera.WorldToScreenPoint(edge.Item1);
            Vector2 sB = (Vector2)camera.WorldToScreenPoint(edge.Item2);
            float dist = DistancePointToSegment2D(mouseScreen, sA, sB);

            if (dist < screenRadius && dist < nearestDist)
            {
                nearestDist = dist;
                nearestEdge = edge;
                found = true;
            }
        }
        return found;
    }

    // ─── 엣지 → Line 변환 ────────────────────────────────────

    /// <summary>엣지(두 점 쌍)를 Line(pointA, pointB)으로 변환한다.</summary>
    /// <param name="edge">월드 좌표 두 점 쌍</param>
    /// <returns>생성된 Line 객체</returns>
    public static Line EdgeToLine((Vector3, Vector3) edge)
    {
        return new Line(edge.Item1, edge.Item2);
    }

    // ─── 내부 유틸리티 ───────────────────────────────────────

    /// <summary>Möller–Trumbore 알고리즘으로 레이-삼각형 교차 판별.</summary>
    private static bool RayIntersectsTriangle(
        Ray ray, Vector3 a, Vector3 b, Vector3 c, out float dist)
    {
        dist = 0f;
        const float epsilon = 1e-8f;

        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 h = Vector3.Cross(ray.direction, ac);
        float det = Vector3.Dot(ab, h);
        if (Mathf.Abs(det) < epsilon) return false;

        float invDet = 1f / det;
        Vector3 s = ray.origin - a;
        float u = invDet * Vector3.Dot(s, h);
        if (u < 0f || u > 1f) return false;

        Vector3 q = Vector3.Cross(s, ab);
        float v = invDet * Vector3.Dot(ray.direction, q);
        if (v < 0f || u + v > 1f) return false;

        dist = invDet * Vector3.Dot(ac, q);
        return dist > epsilon;
    }

    /// <summary>로컬 좌표계 삼각형의 법선 벡터를 반환한다.</summary>
    private static Vector3 GetTriangleNormal(Vector3[] verts, int[] tris, int triIdx)
    {
        Vector3 a = verts[tris[triIdx * 3]];
        Vector3 b = verts[tris[triIdx * 3 + 1]];
        Vector3 c = verts[tris[triIdx * 3 + 2]];
        return Vector3.Cross(b - a, c - a).normalized;
    }

    /// <summary>공유 엣지 기준으로 삼각형 인접 맵을 구성한다.</summary>
    private static Dictionary<int, List<int>> BuildAdjacency(int[] tris, int triCount)
    {
        var edgeToTris = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < triCount; i++)
        {
            RegisterEdge(edgeToTris, tris[i * 3],     tris[i * 3 + 1], i);
            RegisterEdge(edgeToTris, tris[i * 3 + 1], tris[i * 3 + 2], i);
            RegisterEdge(edgeToTris, tris[i * 3 + 2], tris[i * 3],     i);
        }

        var adjacency = new Dictionary<int, List<int>>();
        foreach (var kv in edgeToTris)
        {
            if (kv.Value.Count != 2) continue;
            int ta = kv.Value[0], tb = kv.Value[1];
            if (!adjacency.ContainsKey(ta)) adjacency[ta] = new List<int>();
            if (!adjacency.ContainsKey(tb)) adjacency[tb] = new List<int>();
            adjacency[ta].Add(tb);
            adjacency[tb].Add(ta);
        }
        return adjacency;
    }

    private static void RegisterEdge(
        Dictionary<(int, int), List<int>> map, int a, int b, int triIdx)
    {
        var key = a < b ? (a, b) : (b, a);
        if (!map.ContainsKey(key)) map[key] = new List<int>();
        map[key].Add(triIdx);
    }

    private static void AddEdge(Dictionary<(int, int), int> map, int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        map[key] = map.TryGetValue(key, out int count) ? count + 1 : 1;
    }

    /// <summary>2D 스크린 공간에서 점-선분 사이 최단 거리를 계산한다.</summary>
    private static float DistancePointToSegment2D(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + t * ab);
    }
}
