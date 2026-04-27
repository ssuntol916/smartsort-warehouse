// ============================================================
// 파일명  : RevoluteJointComponentEditor.cs
// 역할    : RevoluteJointComponent 커스텀 Inspector
//           Scene View 에서 회전축 Axis 선택으로 Line 을 생성하는 UI를 제공한다.
//           선택된 Line 정보(pointA, pointB, 방향)를 Inspector에 표시한다.
// 작성자  : 이건호
// 작성일  : 2026-04-02
// ============================================================

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RevoluteJointComponent))]
/**
 * @ brief  'RevoluteJointComponent'를 Editor 에서 편집하기 위한 스크립트. Component 에 표시할 정보 출력.
 */
public class RevoluteJointComponentEditor : Editor
{
    private SerializedProperty _objectA;
    private SerializedProperty _objectB;
    private SerializedProperty _minAngle;
    private SerializedProperty _maxAngle;

    // 에디터 세션 중 선택된 회전축 Line (직렬화되지 않음 - 에디터 임시 저장)
    private Line _axisLineA;
    private Line _axisLineB;

    private void OnEnable()
    {
        _objectA  = serializedObject.FindProperty("_objectA");
        _objectB  = serializedObject.FindProperty("_objectB");
        _minAngle = serializedObject.FindProperty("_minAngle");
        _maxAngle = serializedObject.FindProperty("_maxAngle");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // GUI 설정
        // - 'RevoluteJoint' 정보
        EditorGUILayout.LabelField("RevoluteJoint 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_objectA,  new GUIContent("Object A (기준 요소)"));
        EditorGUILayout.PropertyField(_objectB,  new GUIContent("Object B (이동 요소)"));
        EditorGUILayout.PropertyField(_minAngle, new GUIContent("최소 각도 (°)"));
        EditorGUILayout.PropertyField(_maxAngle, new GUIContent("최대 각도 (°)"));

        EditorGUILayout.Space(10);

        // - Axis 지정상태 GUI
        EditorGUILayout.LabelField("메시 Axis(회전축) 선택으로 회전축 지정", EditorStyles.boldLabel);
        DrawLineField("Object A - Axis", _axisLineA);
        DrawLineField("Object B - Axis", _axisLineB);

        EditorGUILayout.Space(6);

        // - Axis 선택 버튼
        var selector = JointGeometrySelector.Instance;
        bool isSelectingEdge = selector.Mode == JointGeometrySelector.SelectionMode.Edge;
        bool isSelecting     = selector.Mode != JointGeometrySelector.SelectionMode.None;

        using (new EditorGUI.DisabledScope(isSelecting))
        {
            // Button 이 선택되면 'JointGeometrySelector' 호출. Scene View 에서 Axis 를 선택하도록 한다.
            if (GUILayout.Button("Scene View에서 Axis 선택"))
            {
                selector.StartEdgeSelection(
                    line => { _axisLineA = line; Repaint(); },
                    line => { _axisLineB = line; Repaint(); });
            }
        }
        // - Axis 선택 세션 중 표시할 버튼
        if (isSelectingEdge)
        {
            string msg = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                ? "Object A - Axis를 Scene View에서 클릭하세요."
                : "Object B - Axis를 Scene View에서 클릭하세요.";
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
    * @brief    Line이 선택되었는 지 확인하고 Inpsector에 표시하는 메서드.
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
     * @brief   벡터 값 출력 메서드.
     * @param   Vector3
     * @return  string 으로 반환
     */
    private static string FormatVec(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
}
