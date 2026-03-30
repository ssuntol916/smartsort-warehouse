// ============================================================
// 파일명  : JointGeometrySelector.cs
// 역할    : Scene View에서 메쉬 면·엣지 선택 상태를 관리하는 에디터 시스템
//           - 면(Face) 선택: Raycast → 코플래너 병합 → Plane 생성 → 콜백
//           - 엣지(Edge) 선택: 경계 엣지 표시 → 근접 감지 → Line 생성 → 콜백
//           - Object A → Object B 순서로 2회 선택
//           - Esc 취소, Event.Use()로 다른 오브젝트 상호작용 차단
//           - Handles/GL 기반 반투명 오버레이로 선택 시각화
// 작성자  : 이건호
// 작성일  : 2026-03-30
// ============================================================

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/**
 * @brief   Scene View 에서 메쉬 면,엣지 선택 상태를 관리하는 싱글턴 에디터 시스템.
 */
public class JointGeometrySelector
{
    // 싱글턴 세션 인스턴스. Joint 를 선택하는 세션은 모두 해당 인스턴스를 통해 생성한다.
    private static JointGeometrySelector _instance;
    public static JointGeometrySelector Instance => _instance ??= new JointGeometrySelector();

    // 선택 모드 및 단계 타입
    public enum SelectionMode { None, Face, Edge }      // 현재 선택 모드
    public enum SelectionStep { None, WaitA, WaitB }    // 현재 선택 단계 (Object A → Object B 순서)

    // 현재 선택 모드 및 단계
    public SelectionMode Mode { get; private set; } = SelectionMode.None;
    public SelectionStep Step { get; private set; } = SelectionStep.None;

    // 선택 상태
    private MeshFilter[] _allMeshFilters;

    // 면(Plane) 선택 상태
    private List<int>   _hoveredFaceTriangles;      // 커서 올려진 면
    private MeshFilter  _hoveredFaceMeshFilter;
    private List<int>   _confirmedFaceTrianglesA;    // 확정된 면
    private MeshFilter  _confirmedFaceMeshFilterA;

    // 엣지(Line) 선택 상태
    private List<(Vector3, Vector3)>  _currentBoundaryEdges;
    private (Vector3, Vector3)?       _hoveredEdge;

    // 콜백
    private Action<Plane> _onFaceSelectedA;
    private Action<Plane> _onFaceSelectedB;
    private Action<Line>  _onEdgeSelectedA;
    private Action<Line>  _onEdgeSelectedB;

    private const float EdgeScreenRadius = 12f;  // 엣지 근접 감지 픽셀 반경

    // ============================================================
    // 공개 API
    // ============================================================
    /**
     * @brief   면(Plane) 선택 세션을 시작한다. Object A, B 순서로 두 면을 클릭하여 각각 Plane 콜백을 호출한다.
     * @param   Action<Plane>   Object A 면 선택 완료 콜백
     * @param   Action<Plane>   Object B 면 선택 완료 콜백
     */
    public void StartFaceSelection(Action<Plane> onFaceSelectedA, Action<Plane> onFaceSelectedB)
    {
        Reset();
        _allMeshFilters   = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        _onFaceSelectedA  = onFaceSelectedA;
        _onFaceSelectedB  = onFaceSelectedB;
        Mode = SelectionMode.Face;
        Step = SelectionStep.WaitA;
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }

    /**
     * @brief   엣지(Line) 선택 세션을 시작한다. Object A, B 순서로 두 엣지를 클릭하여 각각 Line 콜백을 호출한다.
     * @param   Action<Line>    Object A 엣지 선택 완료 콜백
     * @param   Action<Line>    Object B 엣지 선택 완료 콜백
     */
    public void StartEdgeSelection(Action<Line> onEdgeSelectedA, Action<Line> onEdgeSelectedB)
    {
        Reset();
        _allMeshFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        _onEdgeSelectedA = onEdgeSelectedA;
        _onEdgeSelectedB = onEdgeSelectedB;
        Mode = SelectionMode.Edge;
        Step = SelectionStep.WaitA;
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }

    /** 현재 세션 취소 메서드. 선택정보와 Scene View 를 초기화한다. */
    public void Cancel()
    {
        Reset();
        SceneView.RepaintAll();
    }

    // ============================================================
    // Scene View 핸들링
    // ============================================================
    /**
     * @brief   Scene View 이벤트를 핸들링하는 메서드.
     * @param   SceneView
     */
    private void OnSceneGUI(SceneView sceneView)
    {
        if (Mode == SelectionMode.None) return;

        Event e = Event.current;

        // Esc: 선택 취소
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            Cancel();
            e.Use();
            return;
        }

        // 기본 Scene View 컨트롤 차단 (오브젝트 클릭 선택 방지)
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // 선택 세션 
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Mode == SelectionMode.Face)
            HandleFaceMode(e, ray, sceneView.camera);
        else
            HandleEdgeMode(e, ray, sceneView.camera);

        DrawOverlay();
    }
    // ============================================================
    // 선택 세션 중 Ray 핸들링
    // ============================================================
    /**
     * @brief   커서가 올려진 면 표시 메서드. 커서 이동, 클릭 등의 Event 감지 시, Event, Camera, Ray 를 파라미터로 받아 호출된다.
     * @param   Event
     * @param   Ray
     * @param   Camera
     */
    private void HandleFaceMode(Event e, Ray ray, Camera camera)
    {
        // 마우스 이동: 호버 면 업데이트
        if (e.type == EventType.MouseMove)
        {
            UpdateFaceHover(ray);
            SceneView.RepaintAll();
        }

        // 좌클릭: 호버 면을 확정
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt &&
            _hoveredFaceTriangles != null && _hoveredFaceMeshFilter != null)
        {
            Plane plane = MeshGeometryPicker.FaceToPlane(
                _hoveredFaceMeshFilter.sharedMesh,
                _hoveredFaceMeshFilter.transform,
                _hoveredFaceTriangles);

            if (Step == SelectionStep.WaitA)
            {
                // Object A 확정 → Object B 대기
                _confirmedFaceTrianglesA  = new List<int>(_hoveredFaceTriangles);
                _confirmedFaceMeshFilterA = _hoveredFaceMeshFilter;
                _onFaceSelectedA?.Invoke(plane);
                Step = SelectionStep.WaitB;
                ClearHoverFace();
            }
            else if (Step == SelectionStep.WaitB)
            {
                // Object B 확정 → 세션 종료
                _onFaceSelectedB?.Invoke(plane);
                Reset();
            }
            e.Use();
            SceneView.RepaintAll();
        }
    }
    /** 면 호버 중 커서 업데이트 */
    private void UpdateFaceHover(Ray ray)
    {
        if (MeshGeometryPicker.TryRaycastTriangle(
                ray, _allMeshFilters,
                out MeshFilter hitFilter, out int triIdx))
        {
            _hoveredFaceTriangles  = MeshGeometryPicker.GetCoplanarFace(hitFilter.sharedMesh, triIdx);
            _hoveredFaceMeshFilter = hitFilter;
        }
        else
        {
            ClearHoverFace();
        }
    }
    /** 면 호버 해제 */
    private void ClearHoverFace()
    {
        _hoveredFaceTriangles  = null;
        _hoveredFaceMeshFilter = null;
    }
    /**
     * @brief   커서가 올려진 엣지 표시 메서드. 커서 이동, 클릭 등의 Event 감지 시, Event, Camera, Ray 를 파라미터로 받아 호출된다.
     * @param   Event
     * @param   Ray
     * @param   Camera
     */
    private void HandleEdgeMode(Event e, Ray ray, Camera camera)
    {
        // 마우스 이동: 면 호버 → 경계 엣지 계산 → 근접 엣지 갱신
        if (e.type == EventType.MouseMove)
        {
            UpdateEdgeHover(ray, e.mousePosition, camera);
            SceneView.RepaintAll();
        }

        // 좌클릭: 호버 엣지가 있으면 확정
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt &&
            _hoveredEdge.HasValue)
        {
            Line line = MeshGeometryPicker.EdgeToLine(_hoveredEdge.Value);

            if (Step == SelectionStep.WaitA)
            {
                // Object A 확정 → Object B 대기
                _onEdgeSelectedA?.Invoke(line);
                Step = SelectionStep.WaitB;
                _hoveredEdge          = null;
                _currentBoundaryEdges = null;
            }
            else if (Step == SelectionStep.WaitB)
            {
                // Object B 확정 → 세션 종료
                _onEdgeSelectedB?.Invoke(line);
                Reset();
            }
            e.Use();
            SceneView.RepaintAll();
        }
    }
    /** 엣지 호버 중 커서 업데이트 */
    private void UpdateEdgeHover(Ray ray, Vector2 mousePosition, Camera camera)
    {
        // 면 호버 → 경계 엣지 계산
        if (MeshGeometryPicker.TryRaycastTriangle(
                ray, _allMeshFilters,
                out MeshFilter hitFilter, out int triIdx))
        {
            var faceTriangles = MeshGeometryPicker.GetCoplanarFace(hitFilter.sharedMesh, triIdx);
            _currentBoundaryEdges = MeshGeometryPicker.GetBoundaryEdges(
                hitFilter.sharedMesh, hitFilter.transform, faceTriangles);
        }
        else
        {
            _currentBoundaryEdges = null;
        }

        // 경계 엣지 중 마우스에 가장 가까운 엣지 감지
        if (_currentBoundaryEdges != null &&
            MeshGeometryPicker.TryGetNearestEdge(
                mousePosition, _currentBoundaryEdges, camera,
                EdgeScreenRadius, out var nearest))
        {
            _hoveredEdge = nearest;
        }
        else
        {
            _hoveredEdge = null;
        }
    }
    // ============================================================
    // 선택 세션 중 Overlay 드로잉
    // ============================================================
    private void DrawOverlay()
    {
        // 안내 레이블 (SceneView 좌상단)
        Handles.BeginGUI();
        string modeStr = Mode == SelectionMode.Face ? "면(Face)" : "엣지(Edge)";
        string stepStr = Step == SelectionStep.WaitA ? "Object A" : "Object B";
        GUI.Label(
            new Rect(10, 30, 520, 22),
            $"[Joint Geometry Selector]  {modeStr} 선택 중  —  {stepStr}를 클릭하세요  (Esc: 취소)",
            EditorStyles.boldLabel);
        Handles.EndGUI();

        if (Mode == SelectionMode.Face)
            DrawFaceOverlay();
        else
            DrawEdgeOverlay();
    }

    private void DrawFaceOverlay()
    {
        // 확정된 Object A 면 (초록)
        if (_confirmedFaceTrianglesA != null && _confirmedFaceMeshFilterA != null)
        {
            DrawTriangles(
                _confirmedFaceTrianglesA,
                _confirmedFaceMeshFilterA.sharedMesh,
                _confirmedFaceMeshFilterA.transform,
                new Color(0.2f, 0.9f, 0.3f, 0.4f));
        }

        // 호버 중인 면 (노란색 반투명)
        if (_hoveredFaceTriangles != null && _hoveredFaceMeshFilter != null)
        {
            DrawTriangles(
                _hoveredFaceTriangles,
                _hoveredFaceMeshFilter.sharedMesh,
                _hoveredFaceMeshFilter.transform,
                new Color(1f, 0.85f, 0.1f, 0.35f));
        }
    }

    private void DrawEdgeOverlay()
    {
        // 경계 엣지 전체 (회색 반투명)
        if (_currentBoundaryEdges != null)
        {
            Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            foreach (var edge in _currentBoundaryEdges)
                Handles.DrawLine(edge.Item1, edge.Item2);
        }

        // 호버 엣지 강조 (주황색, 두께 3px)
        if (_hoveredEdge.HasValue)
        {
            Handles.color = new Color(1f, 0.55f, 0f, 1f);
            Handles.DrawLine(_hoveredEdge.Value.Item1, _hoveredEdge.Value.Item2, 3f);
        }
    }

    private static void DrawTriangles(
        List<int> triIndices, Mesh mesh, Transform meshTransform, Color color)
    {
        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Handles.color = color;

        foreach (int triIdx in triIndices)
        {
            Vector3 a = meshTransform.TransformPoint(verts[tris[triIdx * 3]]);
            Vector3 b = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 1]]);
            Vector3 c = meshTransform.TransformPoint(verts[tris[triIdx * 3 + 2]]);
            Handles.DrawAAConvexPolygon(a, b, c);
        }
    }
    // ============================================================
    // 리셋
    // ============================================================
    private void Reset()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Mode = SelectionMode.None;
        Step = SelectionStep.None;

        _allMeshFilters           = null;
        _hoveredFaceTriangles     = null;
        _hoveredFaceMeshFilter    = null;
        _confirmedFaceTrianglesA  = null;
        _confirmedFaceMeshFilterA = null;
        _currentBoundaryEdges     = null;
        _hoveredEdge              = null;

        _onFaceSelectedA = null;
        _onFaceSelectedB = null;
        _onEdgeSelectedA = null;
        _onEdgeSelectedB = null;
    }
}
