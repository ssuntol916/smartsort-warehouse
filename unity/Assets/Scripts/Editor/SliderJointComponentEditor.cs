// ============================================================
// 파일명  : SliderJointComponentEditor.cs
// 역할    : SliderJointComponent 커스텀 Inspector
//           Scene View 에서 이동선 Edge 선택으로 Line을,
//           접촉 Face 선택으로 Plane을 생성하는 UI를 제공한다.
//           선택된 Line/Plane 정보를 Inspector에 표시한다.
// 작성자  : 이건호
// 작성일  : 2026-04-02
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

    // 에디터 세션 중 선택된 이동선 Edge (직렬화되지 않음 - 에디터 임시 저장)
    private Line  _axisLineA;
    private Line  _axisLineB;

    // 에디터 세션 중 선택된 접촉 Face (직렬화되지 않음 - 에디터 임시 저장)
    private Plane _guidePlaneA;
    private Plane _guidePlaneB;

    private void OnEnable()
    {
        _objectA     = serializedObject.FindProperty("_objectA");
        _objectB     = serializedObject.FindProperty("_objectB");
        _minPosition = serializedObject.FindProperty("_minPosition");
        _maxPosition = serializedObject.FindProperty("_maxPosition");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // GUI 설정
        // - 'SliderJoint' 정보
        EditorGUILayout.LabelField("SliderJoint 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_objectA,     new GUIContent("Object A (기준 요소)"));
        EditorGUILayout.PropertyField(_objectB,     new GUIContent("Object B (이동 요소)"));
        EditorGUILayout.PropertyField(_minPosition, new GUIContent("최소 위치"));
        EditorGUILayout.PropertyField(_maxPosition, new GUIContent("최대 위치"));

        EditorGUILayout.Space(10);

        // - Edge 지정상태 GUI
        EditorGUILayout.LabelField("메쉬 Edge 선택으로 이동축 지정", EditorStyles.boldLabel);
        DrawLineField("Object A - Edge", _axisLineA);
        DrawLineField("Object B - Edge", _axisLineB);

        EditorGUILayout.Space(6);

        // - Edge 선택 버튼, Line 생성
        var selector       = JointGeometrySelector.Instance;
        bool isSelectingEdge = selector.Mode == JointGeometrySelector.SelectionMode.Edge;
        bool isSelecting     = selector.Mode != JointGeometrySelector.SelectionMode.None;

        using (new EditorGUI.DisabledScope(isSelecting))
        {
            // Button 이 선택되면 'JointGeometrySelector' 호출. Scene View 에서 Edge 를 선택하도록 한다.
            if (GUILayout.Button("Scene View에서 이동축 Edge 선택"))
            {
                selector.StartEdgeSelection(
                    line => { _axisLineA = line; Repaint(); },
                    line => { _axisLineB = line; Repaint(); });
            }
        }
        // - Edge 선택 세션 중 표시할 버튼
        if (isSelectingEdge)
        {
            string msg = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                ? "Object A - Edge를 Scene View에서 클릭하세요."
                : "Object B - Edge를 Scene View에서 클릭하세요.";
            EditorGUILayout.HelpBox(msg, MessageType.Info);

            if (GUILayout.Button("선택 취소"))
                selector.Cancel();
        }

        EditorGUILayout.Space(10);

        // - Face 지정상태 GUI
        EditorGUILayout.LabelField("메쉬 면 선택으로 기준 평면 지정", EditorStyles.boldLabel);
        DrawPlaneField("Object A - Face", _guidePlaneA);
        DrawPlaneField("Object B - Face", _guidePlaneB);

        EditorGUILayout.Space(6);

        // - Face 선택 버튼
        bool isSelectingFace = selector.Mode == JointGeometrySelector.SelectionMode.Face;
        using (new EditorGUI.DisabledScope(isSelecting))
        {
            // Button 이 선택되면 JointGeometrySelector 호출. Scene View 에서 면 을 선택하도록 한다.
            if (GUILayout.Button("Scene View에서 Face 선택"))
            {
                selector.StartFaceSelection(
                    plane => { _guidePlaneA = plane; Repaint(); },
                    plane => { _guidePlaneB = plane; Repaint(); });
            }
        }
        // - Face 선택 세션 중 표시할 버튼
        if (isSelectingFace)
        {
            string msg = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                ? "Object A - Face를 Scene View에서 클릭하세요."
                : "Object B - Faca를 Scene View에서 클릭하세요.";
            EditorGUILayout.HelpBox(msg, MessageType.Info);

            if (GUILayout.Button("선택 취소"))
                selector.Cancel();
        }

        serializedObject.ApplyModifiedProperties();
    }
    // ============================================================
    // 헬퍼
    // ============================================================
    /** 
    * @brief    Line이 선택되었는 지 확인하고 Inspector에 표시하는 메서드.
    * @param    label   라벨로 표시할 이름
    * @param    Line    Line 개체
    */
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
    /** 
    * @brief    Face가 선택되었는 지 확인하고 Inspector에 표시하는 메서드.
    * @param    label   라벨로 표시할 이름
    * @param    Plane   Plane 개체
    */
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
    /**
     * @brief   벡터 값 출력 메서드.
     * @param   Vector3
     * @return  string 으로 반환
     */
    private static string FormatVec(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
}
