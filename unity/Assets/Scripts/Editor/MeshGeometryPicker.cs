// ============================================================
// 파일명  : MeshGeometryPicker.cs
// 역할    : Scene View에서 메시 면·Edge 선택을 위한 지오메트리 유틸리티
//           - 레이캐스트로 삼각형 인덱스 탐색
//           - BFS 코플래너 확장으로 동일 Face 삼각형 추출
//           - Face → Plane(pointA, pointB, pointC) 변환
//           - 경계 Edge(Boundary Edge) 추출
//           - 마우스 근접 Edge 탐색
//           - Edge → Line(pointA, pointB) 변환
// 작성자  : 이건호
// 작성일  : 2026-03-30
// ============================================================

using System.Collections.Generic;
using UnityEngine;

/**
 * @brief   메시 면(Face)·Edge(Edge) 선택에 필요한 지오메트리 계산 유틸리티. (static)
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
     * @brief   동일면(coplanar) 확장.  법선이 유사한 인접삼각형을 BFS 방식으로 탐색, 삼각형 인덱스 반환.
     * @param   mesh                메시
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

        // 탐색하는 삼각형이 씨드 삼각형의 법선과 동일하면 반환에 추가
        // BFS 방식 탐색: 인접삼각형이 있으면 Queue 추가, 없으면 다음 Queue 로 넘어가며 소진
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
    // 원통 탐지 — Connectivity-based Detection
    // ============================================================
    // 탐지 흐름:
    //   1. [BFS 확장]  시드 삼각형에서 인접 삼각형을 BFS 로 확장.
    //                  인접 법선 각도가 CylinderBFSAngle(50°) 이내인 것만 수락.
    //                  → 날카로운 모서리에서 자동 정지, 완만한 곡면(원통) 전체 수집
    //   2. [최소 호 각도] 법선 분포가 충분히 퍼져 있어야 함 (평면 오탐 방지)
    //   3. [축 추정]   BFS 결과 법선 목록에서 층화 샘플링한 외적 평균으로 축 방향 추정
    //   4. [수직 검증] 모든 법선이 추정 축에 수직 (±15°) 이어야 함 (구·토러스 등 제외)
    //   5. [반경 검증] 버텍스들이 추정 축으로부터 일정 반경을 유지해야 함 (원통성 확인)
    //   6. [축 선분]   원형 중심 + 축 방향 최소·최대 투영으로 Line 반환

    // BFS 인접 법선 허용 각도 (도). 원통 분할 수에 따라 조정 필요
    private const float CylinderBFSAngle    = 50f;
    // 법선이 퍼져야 하는 최소 호 각도 (도). 이보다 작으면 평면으로 간주
    private const float CylinderMinArcAngle = 15f;
    // 법선-축 수직 허용 각도 (도)
    private const float CylinderPerpTol     = 15f;
    // 반경 상대 표준편차 허용치 (0~1). 클수록 불규칙한 원통도 허용
    private const float CylinderRadiusRelStdDev = 0.25f;
    // BFS 최대 삼각형 수 (성능 상한)
    private const int   CylinderMaxTriangles = 600;

    /**
     * @brief   시드 삼각형에서 Connectivity-based Detection 으로 원통 측면을 탐지하고,
     *          회전축 Line 과 면을 이루는 삼각형 인덱스 목록을 반환한다.
     *          일부 Face 만 원통을 이뤄도 해당 집합에서 축을 추정한다.
     * @param   mesh                대상 Mesh
     * @param   meshTransform       대상 Transform
     * @param   seedTriIdx          시드 삼각형 인덱스
     * @param[out] axisLine         추정된 원통 회전축 Line (실패 시 null)
     * @param[out] cylinderTriangles 원통 면을 이루는 삼각형 인덱스 목록 (실패 시 null)
     * @return  bool                원통 탐지 성공 여부
     */
    public static bool TryDetectCylinderFace(
        Mesh mesh, Transform meshTransform, int seedTriIdx,
        out Line axisLine, out List<int> cylinderTriangles)
    {
        axisLine          = null;
        cylinderTriangles = null;
        if (mesh == null) return false;

        int[] tris    = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        int triCount  = tris.Length / 3;
        if (seedTriIdx < 0 || seedTriIdx >= triCount) return false;

        // ── Phase 1: BFS 확장 (smooth-angle criterion) ──────────────────────
        var adjacency  = BuildAdjacency(tris, triCount);
        var triNormals = new Dictionary<int, Vector3>(CylinderMaxTriangles);
        var expanded   = new List<int>(CylinderMaxTriangles);
        var visited    = new HashSet<int>();
        var queue      = new Queue<int>();

        Vector3 seedNormal = GetTriangleNormal(verts, tris, seedTriIdx);
        triNormals[seedTriIdx] = seedNormal;
        visited.Add(seedTriIdx);
        queue.Enqueue(seedTriIdx);

        while (queue.Count > 0 && expanded.Count < CylinderMaxTriangles)
        {
            int cur = queue.Dequeue();
            expanded.Add(cur);

            if (!adjacency.TryGetValue(cur, out var neighbors)) continue;
            Vector3 curNormal = triNormals[cur];

            foreach (int nb in neighbors)
            {
                if (visited.Contains(nb)) continue;
                visited.Add(nb);

                Vector3 nbNormal = GetTriangleNormal(verts, tris, nb);
                // 인접 삼각형 법선이 CylinderBFSAngle 이내면 수락 (곡면 연속성)
                if (Vector3.Angle(curNormal, nbNormal) <= CylinderBFSAngle)
                {
                    triNormals[nb] = nbNormal;
                    queue.Enqueue(nb);
                }
            }
        }

        if (expanded.Count < 4) return false;

        // ── Phase 2: 월드 공간 법선 수집 ────────────────────────────────────
        var normals = new List<Vector3>(expanded.Count);
        foreach (int triIdx in expanded)
        {
            Vector3 a = meshTransform.TransformPoint(verts[tris[triIdx * 3]]);
            Vector3 b = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 1]]);
            Vector3 c = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 2]]);
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude > 1e-10f) normals.Add(n.normalized);
        }
        if (normals.Count < 4) return false;

        // ── Phase 3: 최소 호 각도 검사 (평면 오탐 방지) ─────────────────────
        // normals[0] 기준으로 가장 큰 각도를 구한다.
        float maxArc = 0f;
        for (int i = 1; i < normals.Count; i++)
        {
            float a = Vector3.Angle(normals[0], normals[i]);
            if (a > maxArc) maxArc = a;
        }
        if (maxArc < CylinderMinArcAngle) return false;

        // ── Phase 4: 축 추정 (층화 샘플링 외적 평균) ────────────────────────
        Vector3 axis = EstimateCylinderAxis(normals);
        if (axis.sqrMagnitude < 0.5f) return false;

        // ── Phase 5: 수직 검증 — 모든 법선이 축에 수직이어야 함 ─────────────
        float sinPerpTol = Mathf.Sin(CylinderPerpTol * Mathf.Deg2Rad);
        foreach (Vector3 n in normals)
        {
            if (Mathf.Abs(Vector3.Dot(n, axis)) > sinPerpTol) return false;
        }

        // ── Phase 6: 버텍스에서 원형 중심·범위·반경 분포 계산 ───────────────
        var vertexSet = new HashSet<int>();
        foreach (int triIdx in expanded)
        {
            vertexSet.Add(tris[triIdx * 3]);
            vertexSet.Add(tris[triIdx * 3 + 1]);
            vertexSet.Add(tris[triIdx * 3 + 2]);
        }

        float minT = float.MaxValue, maxT = float.MinValue;
        Vector3 radialSum = Vector3.zero;
        int vCount = 0;
        foreach (int vi in vertexSet)
        {
            Vector3 wp = meshTransform.TransformPoint(verts[vi]);
            float t    = Vector3.Dot(wp, axis);
            if (t < minT) minT = t;
            if (t > maxT) maxT = t;
            radialSum += wp - t * axis;
            vCount++;
        }
        Vector3 center = radialSum / vCount;

        // ── Phase 7: 반경 일관성 검증 ────────────────────────────────────────
        // 버텍스들이 모두 비슷한 반경 거리를 유지해야 원통
        float sumR = 0f, sumR2 = 0f;
        foreach (int vi in vertexSet)
        {
            Vector3 wp       = meshTransform.TransformPoint(verts[vi]);
            Vector3 fromCtr  = wp - center;
            float   projAxis = Vector3.Dot(fromCtr, axis);
            float   r        = (fromCtr - projAxis * axis).magnitude;
            sumR  += r;
            sumR2 += r * r;
        }
        float meanR  = sumR  / vCount;
        float varR   = sumR2 / vCount - meanR * meanR;
        // 상대 표준편차가 임계값 초과 → 원통이 아님
        if (meanR > 0.001f && Mathf.Sqrt(Mathf.Max(0f, varR)) / meanR > CylinderRadiusRelStdDev)
            return false;

        axisLine          = new Line(center + minT * axis, center + maxT * axis);
        cylinderTriangles = expanded;
        return true;
    }

    /**
     * @brief   법선 목록에서 원통 축 방향을 추정한다.
     *          BFS 순서의 앞 1/4 × 뒤 1/2 구간을 층화 샘플링하여 외적을 평균한다.
     *          이 구간은 공간적으로 가장 멀리 떨어져 있으므로 외적이 안정적이다.
     */
    private static Vector3 EstimateCylinderAxis(List<Vector3> normals)
    {
        int n       = normals.Count;
        int quarter = Mathf.Max(1, n / 4);
        int half    = Mathf.Max(1, n / 2);
        int step    = Mathf.Max(1, (n - half) / 8);

        Vector3 axisSum = Vector3.zero;
        Vector3 signRef = Vector3.zero;
        int     sampled = 0;

        for (int i = 0; i < quarter && sampled < 40; i++)
        {
            for (int j = half; j < n && sampled < 40; j += step)
            {
                Vector3 cross = Vector3.Cross(normals[i], normals[j]);
                float   mag   = cross.magnitude;
                if (mag < 0.01f) continue;

                Vector3 candidate = cross / mag;
                // 부호 정규화: 모든 후보를 동일 반구로
                if (signRef == Vector3.zero) signRef = candidate;
                if (Vector3.Dot(candidate, signRef) < 0f) candidate = -candidate;

                axisSum += candidate;
                sampled++;
            }
        }

        return sampled > 0 ? axisSum.normalized : Vector3.zero;
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
