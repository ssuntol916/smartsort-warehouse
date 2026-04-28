// ============================================================
// 파일명  : ConstraintAssemblyWindow.cs
// 역할    : 시각적 파트 조립 도구 EditorWindow
// 작성자  : 송준호
// 작성일  : 2026-04-14
// ============================================================

using UnityEditor;
using UnityEngine;

/**
 * @brief   파트 조립 EditorWindow.
 *          6단계 워크플로우 상태머신을 통해 두 오브젝트 간 Joint 를 생성한다.
 */
public class ConstraintAssemblyWindow : EditorWindow
{
    // ============================================================
    // 상태머신 정의
    // ============================================================

    /** 워크플로우 단계 */
    private enum WorkflowStep
    {
        SelectObjectA,      // ① 오브젝트 A 선택
        SelectGeometryA,    // ② A 의 선/면 선택
        SelectObjectB,      // ③ 오브젝트 B 선택
        SelectGeometryB,    // ④ B 의 선/면 선택
        SelectJointType,    // ⑤ 조인트 타입 선택
        Create              // ⑥ 생성
    }

    /** 기하학 선택 모드 (Edge 만, Face 만, 또는 둘 다) */
    private enum GeometryPickMode
    {
        EdgeOnly,           // Edge 1개 선택 (RevoluteJoint 용)
        EdgeAndFace         // Edge 1개 + Face 1개 선택 (SliderJoint 용)
    }

    /** 지원하는 조인트 타입 */
    private enum JointType
    {
        Revolute,           // 회전 조인트
        Slider              // 이동 조인트
    }

    // ============================================================
    // 상태 변수
    // ============================================================

    // 워크플로우
    private WorkflowStep       _currentStep = WorkflowStep.SelectObjectA;
    private GeometryPickMode   _pickMode    = GeometryPickMode.EdgeOnly;
    private JointType          _jointType   = JointType.Revolute;

    // 오브젝트 선택
    private GameObject _objectA;
    private GameObject _objectB;

    // 기하학 선택 결과
    private Line  _lineA;
    private Line  _lineB;
    private Plane _planeA;
    private Plane _planeB;

    // 기하학 선택 세션 진행 여부
    private bool _isGeometrySelecting;

    // 스크롤 위치
    private Vector2 _scrollPos;

    // 단계별 안내 텍스트
    private static readonly string[] StepLabels =
    {
        "\u2460  \uc624\ube0c\uc81d\ud2b8 A \uc120\ud0dd",
        "\u2461  A \uc758 \uc120/\uba74 \uc120\ud0dd",
        "\u2462  \uc624\ube0c\uc81d\ud2b8 B \uc120\ud0dd",
        "\u2463  B \uc758 \uc120/\uba74 \uc120\ud0dd",
        "\u2464  \uc870\uc778\ud2b8 \ud0c0\uc785 \uc120\ud0dd",
        "\u2465  \uc0dd\uc131"
    };

    private static readonly string[] StepDescriptions =
    {
        "\uad6c\uc18d \uc870\uac74\uc758 \uae30\uc900\uc774 \ub420 \uc624\ube0c\uc81d\ud2b8 A \ub97c Hierarchy \ub610\ub294 Scene View \uc5d0\uc11c \uc120\ud0dd\ud558\uc138\uc694.",
        "Scene View \uc5d0\uc11c \uc624\ube0c\uc81d\ud2b8 A \uc758 Edge(\uc120) \ub610\ub294 Face(\uba74)\ub97c \ud074\ub9ad\ud558\uc5ec \uc120\ud0dd\ud558\uc138\uc694.",
        "\uad6c\uc18d \ub300\uc0c1\uc774 \ub420 \uc624\ube0c\uc81d\ud2b8 B \ub97c Hierarchy \ub610\ub294 Scene View \uc5d0\uc11c \uc120\ud0dd\ud558\uc138\uc694.",
        "Scene View \uc5d0\uc11c \uc624\ube0c\uc81d\ud2b8 B \uc758 Edge(\uc120) \ub610\ub294 Face(\uba74)\ub97c \ud074\ub9ad\ud558\uc5ec \uc120\ud0dd\ud558\uc138\uc694.",
        "\uc120\ud0dd\ub41c \uae30\ud558\ud559 \uc870\ud569\uc5d0 \ub530\ub978 \uc870\uc778\ud2b8 \ud0c0\uc785\uc744 \ud655\uc778\ud558\uace0 \uc120\ud0dd\ud558\uc138\uc694.",
        "\uc124\uc815\uc744 \ud655\uc778\ud558\uace0 \"\uc0dd\uc131\" \ubc84\ud2bc\uc744 \ub20c\ub7ec \uad6c\uc18d \uc870\uac74\uc744 \uc0dd\uc131\ud558\uc138\uc694."
    };

    // ============================================================
    // 메뉴 등록 및 윈도우 열기
    // ============================================================

    [MenuItem("Window/파트 조립")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConstraintAssemblyWindow>();
        window.titleContent = new GUIContent("파트 조립");
        window.minSize = new Vector2(360, 480);
        window.Show();
    }

    // ============================================================
    // EditorWindow 라이프사이클 — SceneView 이벤트 등록/해제
    // ============================================================

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUIObjectPick;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUIObjectPick;
    }

    // ============================================================
    // SceneView 클릭으로 오브젝트 선택 (① / ③ 단계)
    // ============================================================

    /**
     * @brief   오브젝트 선택 단계에서 SceneView 좌클릭을 가로채어
     *          클릭한 메시 오브젝트를 등록하고 다음 단계로 자동 전이한다.
     *          오브젝트 선택 단계가 아닐 때는 아무것도 하지 않는다.
     */
    private void OnSceneGUIObjectPick(SceneView sceneView)
    {
        // 오브젝트 선택 단계가 아니면 무시
        if (_currentStep != WorkflowStep.SelectObjectA &&
            _currentStep != WorkflowStep.SelectObjectB)
            return;

        Event e = Event.current;

        // 안내 레이블 (SceneView 좌상단)
        Handles.BeginGUI();
        string target = _currentStep == WorkflowStep.SelectObjectA ? "Object A" : "Object B";
        GUI.Label(
            new Rect(10, 30, 500, 22),
            $"[파트 조립]  {target} 를 클릭하세요  (Esc: 취소)",
            EditorStyles.boldLabel);
        Handles.EndGUI();

        // 기본 Scene View 오브젝트 선택 차단
        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        // Esc: 워크플로우 초기화
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ResetWorkflow();
            e.Use();
            sceneView.Repaint();
            return;
        }

        // 좌클릭 감지
        if (e.type != EventType.MouseDown || e.button != 0 || e.alt) return;

        // Raycast 로 메시 오브젝트 탐지
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            // Physics Raycast 실패 시 HandleUtility 로 재시도
            GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
            if (picked == null) return;

            if (!TryRegisterPickedObject(picked)) return;
        }
        else
        {
            if (!TryRegisterPickedObject(hit.transform.gameObject)) return;
        }

        e.Use();
        sceneView.Repaint();
        Repaint();
    }

    /**
     * @brief   클릭된 GameObject 를 현재 단계에 등록하고 다음 단계로 전이한다.
     * @param   picked  클릭된 GameObject
     * @return  true 이면 등록 성공, false 이면 무시됨
     */
    private bool TryRegisterPickedObject(GameObject picked)
    {
        // MeshFilter 검증
        if (picked.GetComponent<MeshFilter>() == null) return false;

        if (_currentStep == WorkflowStep.SelectObjectA)
        {
            _objectA = picked;
            _currentStep = WorkflowStep.SelectGeometryA;
            return true;
        }

        if (_currentStep == WorkflowStep.SelectObjectB)
        {
            // A 와 동일한 오브젝트 방지
            if (picked == _objectA) return false;
            _objectB = picked;
            _currentStep = WorkflowStep.SelectGeometryB;
            return true;
        }

        return false;
    }

    // ============================================================
    // OnGUI — 메인 드로잉
    // ============================================================

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        DrawProgressBar();
        EditorGUILayout.Space(8);
        DrawCurrentStepPanel();
        EditorGUILayout.Space(12);
        DrawSelectionSummary();
        EditorGUILayout.Space(8);
        DrawNavigationButtons();

        EditorGUILayout.EndScrollView();
    }

    // ============================================================
    // 진행 표시줄 (6단계 시각화)
    // ============================================================

    private void DrawProgressBar()
    {
        EditorGUILayout.LabelField("워크플로우 진행 상황", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < StepLabels.Length; i++)
        {
            var step = (WorkflowStep)i;
            bool isCurrent   = step == _currentStep;
            bool isCompleted = (int)step < (int)_currentStep;

            // 색상 지정: 완료(초록), 현재(노랑), 미완료(회색)
            var prevBg = GUI.backgroundColor;
            if (isCompleted)
                GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            else if (isCurrent)
                GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            else
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);

            string symbol = isCompleted ? "\u2713" : $"{i + 1}";
            GUILayout.Box(symbol, GUILayout.Width(36), GUILayout.Height(24));
            GUI.backgroundColor = prevBg;
        }
        EditorGUILayout.EndHorizontal();
    }

    // ============================================================
    // 현재 단계별 패널 드로잉
    // ============================================================

    private void DrawCurrentStepPanel()
    {
        int idx = (int)_currentStep;
        EditorGUILayout.LabelField(StepLabels[idx], EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(StepDescriptions[idx], MessageType.Info);
        EditorGUILayout.Space(6);

        switch (_currentStep)
        {
            case WorkflowStep.SelectObjectA:  DrawObjectSelector(ref _objectA, "Object A (기준)"); break;
            case WorkflowStep.SelectGeometryA: DrawGeometrySelector(isObjectA: true);               break;
            case WorkflowStep.SelectObjectB:   DrawObjectSelector(ref _objectB, "Object B (대상)"); break;
            case WorkflowStep.SelectGeometryB: DrawGeometrySelector(isObjectA: false);              break;
            case WorkflowStep.SelectJointType: DrawJointTypeSelector();                             break;
            case WorkflowStep.Create:          DrawCreatePanel();                                   break;
        }
    }

    // ============================================================
    // ① / ③ 오브젝트 선택 패널
    // ============================================================

    /**
     * @brief   오브젝트 필드를 그리고, MeshFilter 가 있는지 검증한다.
     *          SceneView 클릭으로 자동 등록되지만, Inspector 드래그도 여전히 가능하다.
     * @param   obj     선택 대상 GameObject 참조
     * @param   label   표시 라벨
     */
    private void DrawObjectSelector(ref GameObject obj, string label)
    {
        EditorGUILayout.HelpBox(
            "Scene View 에서 메시 오브젝트를 클릭하면 자동으로 등록되고 다음 단계로 넘어갑니다.\n"
            + "또는 아래 필드에 직접 드래그하여 등록할 수도 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        obj = (GameObject)EditorGUILayout.ObjectField(
            label, obj, typeof(GameObject), true);

        if (obj != null)
        {
            if (obj.GetComponent<MeshFilter>() == null)
            {
                EditorGUILayout.HelpBox(
                    "선택한 오브젝트에 MeshFilter 가 없습니다.\n메시가 있는 오브젝트를 선택하세요.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"\"{obj.name}\" 선택됨.  \"다음\" 버튼을 눌러 진행하세요.",
                    MessageType.None);
            }
        }
    }

    // ============================================================
    // ② / ④ 기하학(Edge·Face) 선택 패널
    // ============================================================

    /**
     * @brief   기하학 선택 모드를 결정하고, JointGeometrySelector 세션을 시작하는 UI.
     * @param   isObjectA   true 이면 Object A 측 기하학 선택
     */
    private void DrawGeometrySelector(bool isObjectA)
    {
        // 기하학 선택 모드 선택
        _pickMode = (GeometryPickMode)EditorGUILayout.EnumPopup(
            "선택 모드", _pickMode);

        string modeDesc = _pickMode == GeometryPickMode.EdgeOnly
            ? "Edge(선) 1개만 선택합니다. (회전 조인트용)"
            : "Edge(선) 1개 + Face(면) 1개를 선택합니다. (이동 조인트용)";
        EditorGUILayout.LabelField(modeDesc, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space(6);

        // 현재 선택 상태 표시
        if (isObjectA)
        {
            DrawLineInfo("A 의 Edge", _lineA);
            if (_pickMode == GeometryPickMode.EdgeAndFace)
                DrawPlaneInfo("A 의 Face", _planeA);
        }
        else
        {
            DrawLineInfo("B 의 Edge", _lineB);
            if (_pickMode == GeometryPickMode.EdgeAndFace)
                DrawPlaneInfo("B 의 Face", _planeB);
        }

        EditorGUILayout.Space(6);

        // 선택 세션 시작 / 취소 버튼
        var selector = JointGeometrySelector.Instance;
        bool isSelecting = selector.Mode != JointGeometrySelector.SelectionMode.None;

        if (!_isGeometrySelecting)
        {
            // Edge 선택 버튼
            Line currentLine = isObjectA ? _lineA : _lineB;
            string edgeBtnLabel = currentLine != null
                ? "Edge 재선택 (Scene View)"
                : "Edge 선택 시작 (Scene View)";

            using (new EditorGUI.DisabledScope(isSelecting))
            {
                if (GUILayout.Button(edgeBtnLabel))
                {
                    StartEdgeSelectionSession(isObjectA);
                }
            }

            // Face 선택 버튼 (EdgeAndFace 모드일 때만)
            if (_pickMode == GeometryPickMode.EdgeAndFace)
            {
                Plane currentPlane = isObjectA ? _planeA : _planeB;
                string faceBtnLabel = currentPlane != null
                    ? "Face 재선택 (Scene View)"
                    : "Face 선택 시작 (Scene View)";

                using (new EditorGUI.DisabledScope(isSelecting))
                {
                    if (GUILayout.Button(faceBtnLabel))
                    {
                        StartFaceSelectionSession(isObjectA);
                    }
                }
            }
        }
        else
        {
            // 선택 세션 진행 중 안내
            string modeStr = selector.Mode == JointGeometrySelector.SelectionMode.Edge
                ? "Edge" : "Face";
            string stepStr = selector.Step == JointGeometrySelector.SelectionStep.WaitA
                ? "첫 번째 요소" : "두 번째 요소";
            EditorGUILayout.HelpBox(
                $"{modeStr} 선택 중 — {stepStr}를 Scene View 에서 클릭하세요. (Esc: 취소)",
                MessageType.Info);

            if (GUILayout.Button("선택 취소"))
            {
                selector.Cancel();
                _isGeometrySelecting = false;
            }
        }
    }

    // ============================================================
    // Edge / Face 선택 세션 시작
    // ============================================================

    /**
     * @brief   Edge 선택 세션을 시작한다.
     *          현재 단계의 오브젝트 측(A or B)에 따라 콜백이 다르다.
     *          양쪽 오브젝트 Edge 를 연속으로 선택하는 것이 아니라,
     *          현재 단계의 오브젝트 한쪽만 선택한다.
     * @param   isObjectA   true 이면 Object A 측
     */
    private void StartEdgeSelectionSession(bool isObjectA)
    {
        _isGeometrySelecting = true;
        var selector = JointGeometrySelector.Instance;

        // 단일 오브젝트 워크플로우 — 한 번 클릭으로 세션 종료한다.
        // 첫 번째 콜백에서 결과를 저장하고 즉시 selector.Cancel() 로 세션을 종료하여,
        // EdgeAndFace 모드에서 Edge 선택 후 "선택 취소" 없이 바로 Face 선택으로 넘어갈 수 있게 한다.
        if (isObjectA)
        {
            selector.StartEdgeSelection(
                lineA =>
                {
                    _lineA = lineA;
                    selector.Cancel();
                    _isGeometrySelecting = false;
                    Repaint();
                },
                _ => { /* 두 번째 콜백은 호출되지 않는다 (Cancel 로 종료됨) */ });
        }
        else
        {
            selector.StartEdgeSelection(
                lineA =>
                {
                    _lineB = lineA;
                    selector.Cancel();
                    _isGeometrySelecting = false;
                    Repaint();
                },
                _ => { /* 두 번째 콜백은 호출되지 않는다 (Cancel 로 종료됨) */ });
        }
    }

    /**
     * @brief   Face 선택 세션을 시작한다.
     * @param   isObjectA   true 이면 Object A 측
     */
    private void StartFaceSelectionSession(bool isObjectA)
    {
        _isGeometrySelecting = true;
        var selector = JointGeometrySelector.Instance;

        // 단일 오브젝트 워크플로우 — 한 번 클릭으로 세션 종료한다.
        if (isObjectA)
        {
            selector.StartFaceSelection(
                planeA =>
                {
                    _planeA = planeA;
                    selector.Cancel();
                    _isGeometrySelecting = false;
                    Repaint();
                },
                _ => { /* 두 번째 콜백은 호출되지 않는다 (Cancel 로 종료됨) */ });
        }
        else
        {
            selector.StartFaceSelection(
                planeA =>
                {
                    _planeB = planeA;
                    selector.Cancel();
                    _isGeometrySelecting = false;
                    Repaint();
                },
                _ => { /* 두 번째 콜백은 호출되지 않는다 (Cancel 로 종료됨) */ });
        }
    }

    // ============================================================
    // ⑤ 조인트 타입 선택 패널
    // ============================================================

    private void DrawJointTypeSelector()
    {
        // 자동 추천 (표시 전용, _jointType 을 덮어쓰지 않는다)
        JointType recommended = GetRecommendedJointType();
        EditorGUILayout.LabelField(
            $"추천 조인트:  {GetJointTypeName(recommended)}",
            EditorStyles.boldLabel);

        string reason = recommended == JointType.Revolute
            ? "Line + Line 조합이 감지되어 RevoluteJoint(회전 조인트)를 추천합니다."
            : "Line + Line + Plane + Plane 조합이 감지되어 SliderJoint(이동 조인트)를 추천합니다.";
        EditorGUILayout.HelpBox(reason, MessageType.Info);

        EditorGUILayout.Space(6);

        // 수동 변경 드롭다운
        _jointType = (JointType)EditorGUILayout.EnumPopup(
            "조인트 타입", _jointType);

        // SliderJoint 선택 시 Plane 누락 경고
        if (_jointType == JointType.Slider && (_planeA == null || _planeB == null))
        {
            EditorGUILayout.HelpBox(
                "SliderJoint 에는 양쪽 Face(면) 선택이 필요합니다.\n"
                + "이전 단계로 돌아가 EdgeAndFace 모드로 Face 를 선택하세요.",
                MessageType.Warning);
        }
    }

    /**
     * @brief   선택된 기하학 조합으로 조인트 타입을 추천한다.
     *          _jointType 을 변경하지 않는 읽기 전용 판별이다.
     * @return  추천 JointType
     */
    private JointType GetRecommendedJointType()
    {
        bool hasLines  = _lineA != null && _lineB != null;
        bool hasPlanes = _planeA != null && _planeB != null;

        return (hasLines && hasPlanes) ? JointType.Slider : JointType.Revolute;
    }

    // ============================================================
    // ⑥ 생성 패널 (미리보기 + 생성 버튼)
    // ============================================================

    private void DrawCreatePanel()
    {
        EditorGUILayout.LabelField("생성 미리보기", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField($"Object A:      {(_objectA != null ? _objectA.name : "없음")}");
        EditorGUILayout.LabelField($"Object B:      {(_objectB != null ? _objectB.name : "없음")}");
        EditorGUILayout.LabelField($"조인트 타입:  {GetJointTypeName(_jointType)}");

        EditorGUILayout.Space(4);

        DrawLineInfo("A Edge", _lineA);
        DrawLineInfo("B Edge", _lineB);
        if (_jointType == JointType.Slider)
        {
            DrawPlaneInfo("A Face", _planeA);
            DrawPlaneInfo("B Face", _planeB);
        }

        EditorGUILayout.Space(12);

        // 유효성 검사
        string validation = ValidateForCreation();
        if (validation != null)
        {
            EditorGUILayout.HelpBox(validation, MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(validation != null))
        {
            if (GUILayout.Button("구속 조건 생성", GUILayout.Height(32)))
            {
                CreateJoint();
            }
        }
    }

    /**
     * @brief   생성 전 유효성 검사.
     * @return  오류 메시지 (null 이면 유효)
     */
    private string ValidateForCreation()
    {
        if (_objectA == null) return "Object A 가 선택되지 않았습니다.";
        if (_objectB == null) return "Object B 가 선택되지 않았습니다.";
        if (_objectA == _objectB) return "Object A 와 B 는 서로 다른 오브젝트여야 합니다.";
        if (_lineA == null) return "Object A 의 Edge 가 선택되지 않았습니다.";
        if (_lineB == null) return "Object B 의 Edge 가 선택되지 않았습니다.";

        if (_jointType == JointType.Slider)
        {
            if (_planeA == null) return "SliderJoint 에는 Object A 의 Face 선택이 필요합니다.";
            if (_planeB == null) return "SliderJoint 에는 Object B 의 Face 선택이 필요합니다.";
        }

        return null;
    }

    /**
     * @brief   구속 조건을 생성한다.
     *          별도의 Manager GameObject 를 생성하고, JointComponent 를 부착한 뒤
     *          Object A, B 를 자식으로 배치한다.
     *          (기존 KinematicsDev 씬 및 PlayMode 테스트와 동일한 계층 구조)
     *          Undo 를 지원한다.
     */
    private void CreateJoint()
    {
        string typeName = _jointType == JointType.Revolute
            ? "RevoluteJointManager"
            : "SliderJointManager";

        // ① Manager GameObject 생성
        var manager = new GameObject($"{typeName} ({_objectA.name} - {_objectB.name})");
        Undo.RegisterCreatedObjectUndo(manager, "구속 조건 생성");

        // ② 스냅: Object B 를 구속 조건이 충족되는 위치로 이동
        Undo.RecordObject(_objectB.transform, "구속 조건 생성 — Object B 스냅");
        SnapObjectB();

        // ③ Manager 를 두 오브젝트의 중간 위치에 배치 (스냅 후 위치 기준)
        manager.transform.position =
            (_objectA.transform.position + _objectB.transform.position) * 0.5f;

        // ④ Object A, B 를 Manager 의 자식으로 재배치
        Undo.SetTransformParent(
            _objectA.transform, manager.transform, "구속 조건 생성 — Object A 재배치");
        Undo.SetTransformParent(
            _objectB.transform, manager.transform, "구속 조건 생성 — Object B 재배치");

        // ⑤ JointComponent 를 Manager 에 부착하고 _objectA, _objectB 직렬화 필드 설정
        if (_jointType == JointType.Revolute)
        {
            var comp = Undo.AddComponent<RevoluteJointComponent>(manager);
            SetSerializedField(comp, "_objectA", _objectA.transform);
            SetSerializedField(comp, "_objectB", _objectB.transform);
            SetSerializedField(comp, "_rotationAxis", _lineA.Direction);
            // [2026.04.21 추가] 선택된 Line 점 좌표 저장
            SetSerializedField(comp, "_lineAPointA", _lineA.PointA);
            SetSerializedField(comp, "_lineAPointB", _lineA.PointB);
            SetSerializedField(comp, "_lineBPointA", _lineB.PointA);
            SetSerializedField(comp, "_lineBPointB", _lineB.PointB);
        }
        else
        {
            var comp = Undo.AddComponent<SliderJointComponent>(manager);
            SetSerializedField(comp, "_objectA", _objectA.transform);
            SetSerializedField(comp, "_objectB", _objectB.transform);
            SetSerializedField(comp, "_moveDirection", _lineA.Direction);
            // [2026.04.21 추가] 선택된 Line, Plane 점 좌표 저장
            SetSerializedField(comp, "_lineAPointA", _lineA.PointA);
            SetSerializedField(comp, "_lineAPointB", _lineA.PointB);
            SetSerializedField(comp, "_lineBPointA", _lineB.PointA);
            SetSerializedField(comp, "_lineBPointB", _lineB.PointB);
            SetSerializedField(comp, "_planeAPointA", _planeA.PointA);
            SetSerializedField(comp, "_planeAPointB", _planeA.PointB);
            SetSerializedField(comp, "_planeAPointC", _planeA.PointC);
            SetSerializedField(comp, "_planeBPointA", _planeB.PointA);
            SetSerializedField(comp, "_planeBPointB", _planeB.PointB);
            SetSerializedField(comp, "_planeBPointC", _planeB.PointC);
        }

        // ⑤ 생성된 Manager 를 씬에서 선택
        Selection.activeGameObject = manager;

        Debug.Log($"[구속 조건 조립] {typeName} 생성 완료: {_objectA.name} ↔ {_objectB.name}");

        EditorUtility.DisplayDialog(
            "구속 조건 생성 완료",
            $"{GetJointTypeName(_jointType)} Manager 가 생성되었습니다.\n"
            + $"  Object A: {_objectA.name}\n"
            + $"  Object B: {_objectB.name}",
            "확인");

        // 워크플로우 초기화
        ResetWorkflow();
    }

    // ============================================================
    // 스냅 로직 — Object B 를 구속 위치로 이동
    // ============================================================

    /**
     * @brief   선택된 기하학 정보를 기반으로 Object B 를 구속이 충족되는 위치·회전으로 스냅한다.
     *
     *          RevoluteJoint (Line + Line → 일치):
     *            B 의 회전축(LineB)이 A 의 회전축(LineA) 위에 오도록
     *            B 를 이동시킨다. LineB 의 시작점을 LineA 에 투영하여 최단 거리 이동.
     *
     *          SliderJoint (Line 평행/일치 + Plane 일치):
     *            1) B 의 이동축이 A 의 이동축과 평행하도록 위치 보정 (수직 오프셋 제거)
     *            2) B 의 기준면이 A 의 기준면과 일치하도록 법선 방향 보정
     */
    private void SnapObjectB()
    {
        Transform tA = _objectA.transform;
        Transform tB = _objectB.transform;

        if (_jointType == JointType.Revolute)
        {
            SnapRevoluteJoint(tA, tB);
        }
        else
        {
            SnapSliderJoint(tA, tB);
        }
    }

    /**
     * @brief   RevoluteJoint 스냅: B 의 회전축 시작점을 A 의 회전축 위로 이동시킨다.
     *          LineB.PointA 를 LineA 에 투영한 점으로 B 를 평행이동한다.
     */
    private void SnapRevoluteJoint(Transform tA, Transform tB)
    {
        if (_lineA == null || _lineB == null) return;

        Vector3 projectedPoint = _lineA.Project(_lineB.PointA);
        Vector3 offset = projectedPoint - _lineB.PointA;
        tB.position += offset;

        // [2026.04.21 추가] 스냅 후 lineB 좌표 업데이트 및 방향 일치
        if (Vector3.Dot(_lineA.Direction, _lineB.Direction) < 0)
            _lineB = new Line(_lineB.PointB + offset, _lineB.PointA + offset);
        else
            _lineB = new Line(_lineB.PointA + offset, _lineB.PointB + offset);

        Debug.Log($"[구속 조건 조립] RevoluteJoint 스냅: {tB.name} 을 회전축 위로 이동 (offset: {FormatVec(offset)})");
    }

    /**
     * @brief   SliderJoint 스냅:
     *          1) 이동축 정렬 — B 의 이동축 시작점을 A 의 이동축 위로 투영하여 수직 오프셋 제거
     *          2) 기준면 정렬 — A 의 기준면 위로 B 를 법선 방향 이동시켜 면을 일치시킴
     */
    private void SnapSliderJoint(Transform tA, Transform tB)
    {
        if (_lineA == null || _lineB == null) return;

        // ① 이동축 정렬: LineB 시작점을 LineA 위로 투영
        Vector3 projOnLine = _lineA.Project(_lineB.PointA);
        Vector3 lineOffset = projOnLine - _lineB.PointA;
        tB.position += lineOffset;

        // ② 기준면 정렬 (Plane 이 모두 선택된 경우)
        if (_planeA != null && _planeB != null)
        {
            // 스냅 이동 후의 B 위치로 PlaneB 의 기준점을 갱신
            Vector3 updatedBPoint = _planeB.PointA + lineOffset;

            // PlaneA 의 법선 방향으로 B 기준점과 PlaneA 사이의 거리를 계산
            // 거리 = (B점 - A점) · 법선  → 부호 있는 거리
            float signedDist = Vector3.Dot(updatedBPoint - _planeA.PointA, _planeA.Normal);

            // 법선 방향 오프셋 보정
            Vector3 planeOffset = -_planeA.Normal * signedDist;
            tB.position += planeOffset;

            Debug.Log($"[구속 조건 조립] SliderJoint 스냅: {tB.name} 이동축 + 기준면 정렬 완료"
                + $"\n  이동축 오프셋: {FormatVec(lineOffset)}"
                + $"\n  기준면 오프셋: {FormatVec(planeOffset)}");
        }
        else
        {
            Debug.Log($"[구속 조건 조립] SliderJoint 스냅: {tB.name} 이동축 정렬 완료 (offset: {FormatVec(lineOffset)})");
        }
    }

    // ============================================================
    // 직렬화 필드 설정 헬퍼
    // ============================================================

    /**
     * @brief   SerializedObject 를 통해 JointComponent 의 private 직렬화 필드를 설정한다. (Object 참조)
     * @param   component   대상 컴포넌트
     * @param   fieldName   직렬화 필드명
     * @param   value       설정할 값
     */
    private static void SetSerializedField(Component component, string fieldName, Object value)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[구속 조건 조립] 직렬화 필드를 찾을 수 없음: {fieldName}");
        }
    }

    /**
     * @brief   SerializedObject 를 통해 Vector3 직렬화 필드를 설정한다.
     * @param   component   대상 컴포넌트
     * @param   fieldName   직렬화 필드명
     * @param   value       설정할 Vector3 값
     */
    private static void SetSerializedField(Component component, string fieldName, Vector3 value)
    {
        var so = new SerializedObject(component);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.vector3Value = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"[구속 조건 조립] 직렬화 필드를 찾을 수 없음: {fieldName}");
        }
    }

    // ============================================================
    // 선택 요약 (하단 고정 패널)
    // ============================================================

    private void DrawSelectionSummary()
    {
        EditorGUILayout.LabelField("현재 선택 상태", EditorStyles.boldLabel);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawSummaryRow("Object A", _objectA != null ? _objectA.name : "미선택");
            DrawSummaryRow("A Edge",   _lineA != null  ? FormatVec(_lineA.Direction) : "미선택");
            DrawSummaryRow("A Face",   _planeA != null ? FormatVec(_planeA.Normal)   : "미선택");

            EditorGUILayout.Space(2);

            DrawSummaryRow("Object B", _objectB != null ? _objectB.name : "미선택");
            DrawSummaryRow("B Edge",   _lineB != null  ? FormatVec(_lineB.Direction) : "미선택");
            DrawSummaryRow("B Face",   _planeB != null ? FormatVec(_planeB.Normal)   : "미선택");

            EditorGUILayout.Space(2);

            DrawSummaryRow("조인트",   GetJointTypeName(_jointType));
        }
    }

    // ============================================================
    // 네비게이션 버튼 (이전 / 다음 / 초기화)
    // ============================================================

    private void DrawNavigationButtons()
    {
        EditorGUILayout.BeginHorizontal();

        // 초기화
        if (GUILayout.Button("초기화", GUILayout.Height(28)))
        {
            ResetWorkflow();
        }

        GUILayout.FlexibleSpace();

        // 이전 단계
        using (new EditorGUI.DisabledScope(_currentStep == WorkflowStep.SelectObjectA))
        {
            if (GUILayout.Button("< 이전", GUILayout.Width(80), GUILayout.Height(28)))
            {
                CancelActiveSession();
                _currentStep = (WorkflowStep)((int)_currentStep - 1);
            }
        }

        // 다음 단계
        using (new EditorGUI.DisabledScope(!CanAdvance()))
        {
            if (GUILayout.Button("다음 >", GUILayout.Width(80), GUILayout.Height(28)))
            {
                CancelActiveSession();
                _currentStep = (WorkflowStep)((int)_currentStep + 1);

                // ⑤ 조인트 타입 단계 진입 시, 추천값을 초기값으로 1회 설정
                if (_currentStep == WorkflowStep.SelectJointType)
                    _jointType = GetRecommendedJointType();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /**
     * @brief   현재 단계에서 다음 단계로 진행 가능한지 검사.
     * @return  true 이면 진행 가능
     */
    private bool CanAdvance()
    {
        switch (_currentStep)
        {
            case WorkflowStep.SelectObjectA:
                return _objectA != null && _objectA.GetComponent<MeshFilter>() != null;

            case WorkflowStep.SelectGeometryA:
                if (_lineA == null) return false;
                if (_pickMode == GeometryPickMode.EdgeAndFace && _planeA == null) return false;
                return true;

            case WorkflowStep.SelectObjectB:
                return _objectB != null && _objectB.GetComponent<MeshFilter>() != null
                    && _objectA != _objectB;

            case WorkflowStep.SelectGeometryB:
                if (_lineB == null) return false;
                if (_pickMode == GeometryPickMode.EdgeAndFace && _planeB == null) return false;
                return true;

            case WorkflowStep.SelectJointType:
                return true;    // 타입 선택은 항상 진행 가능

            case WorkflowStep.Create:
                return false;   // 마지막 단계 — 다음 없음

            default:
                return false;
        }
    }

    // ============================================================
    // 초기화 / 유틸리티
    // ============================================================

    /** 워크플로우 전체 초기화 */
    private void ResetWorkflow()
    {
        CancelActiveSession();
        _currentStep = WorkflowStep.SelectObjectA;
        _objectA     = null;
        _objectB     = null;
        _lineA       = null;
        _lineB       = null;
        _planeA      = null;
        _planeB      = null;
        _jointType   = JointType.Revolute;
        _pickMode    = GeometryPickMode.EdgeOnly;
        SceneView.RepaintAll();
    }

    /** 진행 중인 기하학 선택 세션이 있으면 취소 */
    private void CancelActiveSession()
    {
        if (_isGeometrySelecting)
        {
            JointGeometrySelector.Instance.Cancel();
            _isGeometrySelecting = false;
        }
    }

    /** 조인트 타입 한글 표시명 */
    private static string GetJointTypeName(JointType type) =>
        type == JointType.Revolute ? "RevoluteJoint (회전)" : "SliderJoint (이동)";

    // ============================================================
    // GUI 헬퍼
    // ============================================================

    /** Line 정보 표시 */
    private static void DrawLineInfo(string label, Line line)
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

    /** Plane 정보 표시 */
    private static void DrawPlaneInfo(string label, Plane plane)
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

    /** 요약 행 표시 (라벨 + 값) */
    private static void DrawSummaryRow(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(70));
            EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
        }
    }

    /** Vector3 포맷팅 */
    private static string FormatVec(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
}