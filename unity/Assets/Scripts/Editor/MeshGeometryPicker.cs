// ============================================================
// 파일명  : MeshGeometryPicker.cs
// 역할    : Scene View에서 Face·Edge 선택을 위한 지오메트리 유틸리티
//           - Raycast로 삼각형 인덱스 탐색
//           - BFS 방식으로 코플래너 확장하여 동일 Face 삼각형 추출
//           - Face → Plane(pointA, pointB, pointC) 변환
//           - 경계 Edge(Boundary Edge) 추출
//           - 마우스 근접 Edge 탐색
//           - Edge → Line(pointA, pointB) 변환
// 작성자  : 이건호
// 작성일  : 2026-04-02
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/**
 * @brief   Face·Edge 선택에 필요한 지오메트리 계산 유틸리티. (static)
 *          모두 Editor 전용.
 */
public static class MeshGeometryPicker
{
    // 동일면(coplanar) 판단 기준 각도 허용 오차 (도)
    private const float CoplanarAngleTolerance = 0.01f;

    // ============================================================
    // Raycast 및 MeshFilter 탐색
    // ============================================================
    /**
     * @brief   Raycast 후 삼각형 찾기. Ray 와 충돌한 오브젝트의 MeshFilter 를 가장 가까운 삼각형 인덱스를 반환한다.
     * @param   ray                 월드 공간 Ray
     * @param   candidates          대상 MeshFilter 배열
     * @param[out]  hitFilter       충돌한 MeshFilter (없으면 null)
     * @param[out]  triangleIndex   충돌한 삼각형 인덱스 (없으면 -1)
     * @return  bool                충돌여부
     */
    public static bool TryRaycastTriangle(
        Ray ray,
        MeshFilter[] candidates,
        out MeshFilter hitFilter,
        out int triangleIndex)
    {
        hitFilter = null;
        triangleIndex = -1;
        float nearestDist = float.MaxValue;
        // 가장 가까운 MeshFilter 찾고 출력
        foreach (MeshFilter mf in candidates)
        {
            // 충돌한 MeshFilter, 또는 가지고 있는 Mesh 가 없을경우 return 검사 (false)
            if (mf == null || mf.sharedMesh == null) continue;

            // MeshFilter 의 Transform, vertices 을 기준으로 가까운 MeshFilter 를 hitFilter 로 출력
            Mesh mesh = mf.sharedMesh;
            Transform t = mf.transform;
            int[] tris = mesh.triangles;
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < tris.Length; i += 3)
            {
                // Mesh의 삼각형이 Ray와 교차하는 지 판별하고 가장 가까운 삼각형 hitFilter 로 출력
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
        // MeshFilter 여부 반환
        return hitFilter != null;
    }
    // ============================================================
    // Face(Plane) 탐색
    // ============================================================
    /**
     * @brief   동일면(coplanar) 확장. BFS 방식으로 법선이 유사한 인접삼각형을 확장하여 목록 반환. 법선은 CoplanarAngleTolerance 로 검사.
     * @param   mesh                메쉬
     * @param   seedTriangleIndex   시작할 씨드 삼각형
     * @return  List<int>           동일평면 삼각형 목록
     */
    public static List<int> GetCoplanarFace(Mesh mesh, int seedTriangleIndex)
    {
        // Mesh 없으면 빈 리스트 반환
        if (mesh == null) return new List<int>();

        // 동일면 찾을 Mesh, 씨드 삼각형 획득
        int[] tris = mesh.triangles;        // Mesh 의 삼각형 인덱스 (Mesh 자체의 정보)
        Vector3[] verts = mesh.vertices;
        int triCount = tris.Length / 3;     // 인접면 인덱싱을 위한 삼각형 개수
        Vector3 seedNormal = GetTriangleNormal(verts, tris, seedTriangleIndex);

        // Mesh 로 인접 삼각형 매핑, 탐색을 위한 Generic 생성
        var adjacency = BuildAdjacency(tris, triCount);
        var result = new List<int>();
        var visited = new HashSet<int>();       // 이전에 탐색한 삼각형
        var queue = new Queue<int>();           // 탐색할 전체 삼각형 (초기값은 씨드삼각형 밖에 없다)
        visited.Add(seedTriangleIndex);
        queue.Enqueue(seedTriangleIndex);

        // BFS 방식 탐색
        // 인접삼각형이 있으면 Queue 추가, 없으면 다음 Queue 로 넘어가며 소진
        // 탐색하는 삼각형이 씨드 삼각형의 법선과 동일하면 반환에 추가
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            result.Add(current);

            // 인접 삼각형 없으면 다음 queue
            if (!adjacency.TryGetValue(current, out List<int> neighbors)) continue;

            // 탐색
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
    /**
     * @brief   Mesh, Transform, 삼각형 인덱스목록으로 해당 삼각형의 Plane 반환.
     *          (삼각형 인덱스 리스트를 사용하는 이유는 coplanar 로 찾은 Face 를 사용하기 위함)
     * @param   mesh            대상 Mesh
     * @param   meshTransform   대상 Transform
     * @param   triangleIndices 삼각형 인덱스 목록
     * @reutrn  Plane
     */
    public static Plane FaceToPlane(Mesh mesh, Transform meshTransform, List<int> triangleIndices)
    {
        // 삼각형 없으면 null, trash Plane 반환
        if (mesh == null || triangleIndices == null || triangleIndices.Count == 0)
            return new Plane(Vector3.zero, Vector3.right, Vector3.forward);

        // Face 중 대표값 [인덱스 0] 획득, 좌표 생성
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        int triIdx = triangleIndices[0];
        Vector3 a = meshTransform.TransformPoint(verts[tris[triIdx * 3]]);
        Vector3 b = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 1]]);
        Vector3 c = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 2]]);

        // Plane 생성
        return new Plane(a, b, c);
    }
    // ============================================================
    // Edge(Line) 탐색
    // ============================================================
    /**
     * @brief   Mesh, Transform, 삼각형 인덱스목록으로 해당 삼각형 Edge의 Line 좌표 리스트 반환.
     *          (Line 의 리스트가 아니므로 EdgeToLine 메서드 사용)
     * @param   mesh            대상 Mesh
     * @param   meshTransform   대상 Transform
     * @param   triangleIndices 삼각형 인덱스 목록
     * @reutrn  List<(Vector3, Vector3)>    Line 좌표의 리스트
     */
    public static List<(Vector3, Vector3)> GetBoundaryEdges(
        Mesh mesh,
        Transform meshTransform,
        List<int> triangleIndices)
    {
        // Mesh, 삼각형 인덱스 없으면 빈 List 반환
        if (mesh == null || triangleIndices == null)
            return new List<(Vector3, Vector3)>();

        // 삼각형 Edge 에 따라 
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

        // 경계 Edge 리스트 반환
        var boundary = new List<(Vector3, Vector3)>();
        foreach (var kv in edgeCount)
        {
            if (kv.Value == 1)  // 경계 Edge: 한 삼각형에만 소속
            {
                Vector3 a = meshTransform.TransformPoint(verts[kv.Key.Item1]);
                Vector3 b = meshTransform.TransformPoint(verts[kv.Key.Item2]);
                boundary.Add((a, b));
            }
        }
        return boundary;
    }
    /**
     * @brief   마우스 위치에 가장 가까운 Edge 출력.
     * @param   mousePosition   GUI 좌표계 마우스 위치 (SceneView Event.mousePosition)
     * @param   edges           월드 좌표 경계 Edge 목록
     * @param   camera          Scene View 카메라
     * @param   screenRadius    픽셀 단위 근접 감지 반경
     * @param   nearestEdge     가장 가까운 Edge (못 찾으면 default)
     * @return  bool        Edge 여부
     */
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
    /**
     * @brief   Edge 두 점 좌표를 Line(pointA, pointB)으로 변환.
     * @param   edge    두 점 Vector3 튜플
     * @return  Line
     */
    public static Line EdgeToLine((Vector3, Vector3) edge)
    {
        return new Line(edge.Item1, edge.Item2);
    }
    // ============================================================
    // 내부 유틸리티
    // ============================================================
    /**
     * @brief   Möller–Trumbore 알고리즘으로 레이-삼각형 교차 판별.
     *          (TryRaycastTriangle -- 삼각형 충돌 판별)
     * @param   ray     레이
     * @param   a       삼각형 점 A
     * @param   b       삼각형 점 b
     * @param   c       삼각형 점 C
     * @param[out]  dist    거리
     * @return  bool    교차 여부
     */
    private static bool RayIntersectsTriangle(
        Ray ray, Vector3 a, Vector3 b, Vector3 c, out float dist)
    {
        // 값 초기화
        dist = 0f;
        const float epsilon = 1e-8f;

        // 삼각형 두 변에 대한 행렬식, 즉 2*(삼각형 넓이)
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 h = Vector3.Cross(ray.direction, ac);
        float det = Vector3.Dot(ab, h);
        if (Mathf.Abs(det) < epsilon) return false;

        // barycentric 좌표 계산
        // 1. u<0, u>1 이면 외부
        float invDet = 1f / det;
        Vector3 s = ray.origin - a;
        float u = invDet * Vector3.Dot(s, h);
        if (u < 0f || u > 1f) return false;

        // 2. v<0, u+v>1 이면 외부
        Vector3 q = Vector3.Cross(s, ab);
        float v = invDet * Vector3.Dot(ray.direction, q);
        if (v < 0f || u + v > 1f) return false;

        // 3. (u>=0,v>=0 이고 u+v<0) 삼각형 내부 이므로, 거리 반환
        dist = invDet * Vector3.Dot(ac, q);
        return dist > epsilon;
    }

    /**
     * @brief   로컬 좌표계 삼각형의 법선 벡터를 반환.
     *          (GetCoplanarFace -- 동일면 판별 법선 생성)
     */
    private static Vector3 GetTriangleNormal(Vector3[] verts, int[] tris, int triIdx)
    {
        Vector3 a = verts[tris[triIdx * 3]];
        Vector3 b = verts[tris[triIdx * 3 + 1]];
        Vector3 c = verts[tris[triIdx * 3 + 2]];
        return Vector3.Cross(b - a, c - a).normalized;
    }

    /**
     * @brief   공유 Edge 기준으로 삼각형 인접 맵을 구성.
     *          (GetCoplanarFace -- 인접 삼각형 탐색)
     */
    private static Dictionary<int, List<int>> BuildAdjacency(int[] tris, int triCount)
    {
        // (Edge 좌표, 삼각형 인덱스) Dict. 삼각형 인덱스를 바탕으로 
        var edgeToTris = new Dictionary<(int, int), List<int>>();
        for (int i = 0; i < triCount; i++)
        {
            RegisterEdge(edgeToTris, tris[i * 3],     tris[i * 3 + 1], i);
            RegisterEdge(edgeToTris, tris[i * 3 + 1], tris[i * 3 + 2], i);
            RegisterEdge(edgeToTris, tris[i * 3 + 2], tris[i * 3],     i);
        }

        // 인접 삼각형 인덱스 Dict 생성
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
    /**
     * @brief   (BuildAdjacency -- (Edge 좌표, 삼각형 인덱스) Dict 에 인덱스 추가)
     */
    private static void RegisterEdge(
        Dictionary<(int, int), List<int>> map, int a, int b, int triIdx)
    {
        // Edge 좌표쌍을 키로 받아, 해당 키가 있는지 확인하고 인덱스를 넣는다
        var key = a < b ? (a, b) : (b, a);
        if (!map.ContainsKey(key)) map[key] = new List<int>();
        map[key].Add(triIdx);
    }

    /**
     * @brief   (GetBoundaryEdges -- (Edge 좌표, 삼각형 개수) Dict 에 개수 추가)
     */
    private static void AddEdge(Dictionary<(int, int), int> map, int a, int b)
    {
        // Edge 좌표쌍을 키로 받아, 해당 키에 개수를 추가한다
        var key = a < b ? (a, b) : (b, a);
        map[key] = map.TryGetValue(key, out int count) ? count + 1 : 1;
    }

    /**
     * @brief   2D 스크린 공간에서 점-선분 사이 최단 거리를 계산.
     */
    private static float DistancePointToSegment2D(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + t * ab);
    }
}
