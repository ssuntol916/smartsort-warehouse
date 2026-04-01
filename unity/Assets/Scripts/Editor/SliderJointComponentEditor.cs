// ============================================================
// 파일명  : SliderJointComponentEditor.cs
// 역할    : SliderJointComponent 커스텀 Inspector
//           "SliderJoint 생성" 버튼으로 이동축·기준 Face 선택 세션을 시작하고,
//           확인 클릭 시 선택된 Line 방향을 _moveDirection 에,
//           선택된 메시의 GameObject Transform 을 _objectA·B 에 반영한다.
//           취소 클릭 시 선택 상태를 초기화하고 Component는 수정하지 않는다.
// 작성자  : 이건호
// 작성일  : 2026-03-30
// ============================================================

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SliderJointComponent))]
public class SliderJointComponentEditor : Editor
{
    private SerializedProperty _objectA;
    private SerializedProperty _objectB;
    private SerializedProperty _minPosition;
    private SerializedProperty _maxPosition;
    private SerializedProperty _moveDirection;

    // 에디터 임시 선택 상태 (직렬화되지 않음)
    private Line      _axisLineA;
    private Line      _axisLineB;
    private Plane     _guidePlaneA;
    private Plane     _guidePlaneB;
    private Transform _selectedTransformA;   // Object A 선택 시 해당 메시의 Transform
    private Transform _selectedTransformB;   // Object B 선택 시 해당 메시의 Transform
    private bool      _isCreating;

    // ObjectA·B 변경 감지용 캐시
    private UnityEngine.Object _cachedObjectA;
    private UnityEngine.Object _cachedObjectB;

    private void OnEnable()
    {
        _objectA       = serializedObject.FindProperty("_objectA");
        _objectB       = serializedObject.FindProperty("_objectB");
        _minPosition   = serializedObject.FindProperty("_minPosition");
        _maxPosition   = serializedObject.FindProperty("_maxPosition");
        _moveDirection = serializedObject.FindProperty("_moveDirection");

        // Inspector 가 활성화될 때 현재 참조를 캐시
        _cachedObjectA = _objectA.objectReferenceValue;
        _cachedObjectB = _objectB.objectReferenceValue;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- 기본 설정 ---
        EditorGUILayout.LabelField("SliderJoint 설정", EditorStyles.boldLabel);

        // ObjectA·B 변경 감지: 변경되면 스테이징 상태 초기화
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_objectA, new GUIContent("Object A (이동 기준)"));
        EditorGUILayout.PropertyField(_objectB, new GUIContent("Object B (이동 대상)"));
        if (EditorGUI.EndChangeCheck())
        {
            var newA = _objectA.objectReferenceValue;
            var newB = _objectB.objectReferenceValue;
            if (newA != _cachedObjectA || newB != _cachedObjectB)
            {
                _cachedObjectA      = newA;
                _cachedObjectB      = newB;
                _axisLineA          = null;
                _axisLineB          = null;
                _guidePlaneA        = null;
                _guidePlaneB        = null;
                _selectedTransformA = null;
                _selectedTransformB = null;
            }
        }

        EditorGUILayout.PropertyField(_minPosition,   new GUIContent("최소 위치 (mm)"));
        EditorGUILayout.PropertyField(_maxPosition,   new GUIContent("최대 위치 (mm)"));
        EditorGUILayout.PropertyField(_moveDirection, new GUIContent("이동 방향"));

        EditorGUILayout.Space(10);

        // --- 선택 세션 GUI ---
        var selector       = JointGeometrySelector.Instance;
        bool isSelecting     = selector.Mode != JointGeometrySelector.SelectionMode.None;
        bool isSelectingEdge = selector.Mode == JointGeometrySelector.SelectionMode.Edge;
        bool isSelectingFace = selector.Mode == JointGeometrySelector.SelectionMode.Face;

        if (!_isCreating)
        {
            // 선택 세션 전
            // --- "생성" 버튼: 클릭하면 선택 UI 표시 ---
            if (GUILayout.Button("SliderJoint 생성"))
            {
                _isCreating         = true;
                _axisLineA          = null;
                _axisLineB          = null;
                _guidePlaneA        = null;
                _guidePlaneB        = null;
                _selectedTransformA = null;
                _selectedTransformB = null;
            }
        }
        else
        {
            // 선택 세션 중
            // --- 이동축 Edge 선택 섹션 ---
            EditorGUILayout.LabelField("공통 Edge 선택", EditorStyles.boldLabel);
            DrawLineField("Object A 기준 Edge", _axisLineA);
            DrawLineField("Object B 이동 Edge", _axisLineB);

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(isSelecting))
            {
                if (GUILayout.Button("Scene View에서 Edge 선택  (A → B 순서)"))
                {
                    selector.StartEdgeSelection(
                        (line, t) => { _axisLineA = line; _selectedTransformA = t; Repaint(); },
                        (line, t) => { _axisLineB = line; _selectedTransformB = t; Repaint(); });
                }
            }

            if (isSelectingEdge)
            {
                string msg = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                    ? "Object A  기준 Edge를 Scene View에서 클릭하세요."
                    : "Object B  이동 Edge를 Scene View에서 클릭하세요.";
                EditorGUILayout.HelpBox(msg, MessageType.Info);

                if (GUILayout.Button("Edge 선택 취소"))
                    selector.Cancel();
            }

            EditorGUILayout.Space(10);

            // --- 기준 Face 선택 섹션 ---
            EditorGUILayout.LabelField("접할 Face 선택", EditorStyles.boldLabel);
            DrawPlaneField("Object A 기준 Face", _guidePlaneA);
            DrawPlaneField("Object B 이동 Face", _guidePlaneB);

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(isSelecting))
            {
                if (GUILayout.Button("Scene View에서 Face 선택  (A → B 순서)"))
                {
                    selector.StartFaceSelection(
                        // Plane 선택에서도 Transform 을 받아 반영 (Edge 선택이 없을 경우 대비)
                        (plane, t) => { _guidePlaneA = plane; if (_selectedTransformA == null) _selectedTransformA = t; Repaint(); },
                        (plane, t) => { _guidePlaneB = plane; if (_selectedTransformB == null) _selectedTransformB = t; Repaint(); });
                }
            }

            if (isSelectingFace)
            {
                string msg = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                    ? "Object A  기준 Face를 Scene View에서 클릭하세요."
                    : "Object B  기준 Face를 Scene View에서 클릭하세요.";
                EditorGUILayout.HelpBox(msg, MessageType.Info);

                if (GUILayout.Button("Face 선택 취소"))
                    selector.Cancel();
            }

            EditorGUILayout.Space(8);

            // --- 확인 / 취소 버튼 ---
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_axisLineA == null))
                {
                    if (GUILayout.Button("확인"))
                    {
                        // 이동 방향 반영
                        _moveDirection.vector3Value = _axisLineA.Direction;

                        // 선택된 메시의 GameObject 를 ObjectA·B 에 반영
                        if (_selectedTransformA != null)
                            _objectA.objectReferenceValue = _selectedTransformA;
                        if (_selectedTransformB != null)
                            _objectB.objectReferenceValue = _selectedTransformB;

                        serializedObject.ApplyModifiedProperties();

                        selector.Cancel();
                        _isCreating         = false;
                        _axisLineA          = null;
                        _axisLineB          = null;
                        _guidePlaneA        = null;
                        _guidePlaneB        = null;
                        _selectedTransformA = null;
                        _selectedTransformB = null;
                    }
                }

                if (GUILayout.Button("취소"))
                {
                    // 선택기만 초기화, Component는 수정하지 않음
                    selector.Cancel();
                    _isCreating         = false;
                    _axisLineA          = null;
                    _axisLineB          = null;
                    _guidePlaneA        = null;
                    _guidePlaneB        = null;
                    _selectedTransformA = null;
                    _selectedTransformB = null;
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ============================================================
    // 내부 유틸리티
    // ============================================================
    private static void DrawLineField(string label, Line line)
    {
        if (line == null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField("미선택", EditorStyles.miniLabel);
            }
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("  Point A", FormatVec(line.PointA));
            EditorGUILayout.LabelField("  Point B", FormatVec(line.PointB));
            EditorGUILayout.LabelField("  방향",    FormatVec(line.Direction));
        }
    }

    private static void DrawPlaneField(string label, Plane plane)
    {
        if (plane == null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField("미선택", EditorStyles.miniLabel);
            }
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("  Point A", FormatVec(plane.PointA));
            EditorGUILayout.LabelField("  Point B", FormatVec(plane.PointB));
            EditorGUILayout.LabelField("  Point C", FormatVec(plane.PointC));
            EditorGUILayout.LabelField("  법선",    FormatVec(plane.Normal));
        }
    }

    private static string FormatVec(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
}
