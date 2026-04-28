// ============================================================
// 파일명  : DirectionSwitchController.cs
// 역할    : 래크 앤 피니언 구동 시 슬라이더와 셔틀의 이동 주체를 전환한다.
//
//           [내려가는 방향] SpurGearController.IsForward = true
//             Phase 1 (SliderMoving)  : 슬라이더 하강, 셔틀 고정
//             Phase 2                 : ywheel3(슬라이더 바퀴) Y최저점이
//                                       신호 시작 시 저장한 _targetFloorY 에 도달 → 전환
//             Phase 3 (ShuttleMoving) : 셔틀 상승, 슬라이더 월드 위치 고정
//
//           [올라가는 방향] SpurGearController.IsForward = false
//             Phase 1 (ShuttleMoving) : 셔틀 하강, 슬라이더 월드 위치 고정
//             Phase 2                 : xwheel1(셔틀 바퀴) Y최저점이
//                                       신호 시작 시 저장한 _targetFloorY 에 도달 → 전환
//             Phase 3 (SliderMoving)  : 슬라이더 상승, 셔틀 고정
//
//           _targetFloorY:
//             신호 시작 시 xwheel1 과 ywheel3 의 Y최저점 중 더 낮은 값을 저장한다.
//             내려가는 방향: 슬라이더가 아직 내려가지 않았으므로 xwheel1.Y 가 낮음
//                            → xwheel1.Y 가 트리거 기준이 됨
//             올라가는 방향: 슬라이더가 이미 내려가 있으므로 ywheel3.Y 가 낮음
//                            → ywheel3.Y 가 트리거 기준이 됨
//             셔틀이 X/Z 이동 후에도 신호 시작 시 재계산되므로 위치 무관하게 동작한다.
//
//           슬라이더 고정:
//             vslider1 은 셔틀의 자식이므로 셔틀 Y 이동 시 로컬 좌표로 함께 이동한다.
//             ShuttleMoving 진입 시 슬라이더의 셔틀 기준 X/Z 로컬 오프셋과
//             월드 Y 를 분리 저장하고, 매 프레임 셔틀 X/Z 위치 + 저장된 월드 Y 로
//             복원하여 슬라이더를 고정한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// ============================================================

using UnityEngine;

public class DirectionSwitchController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;           // 셔틀 최상위 Transform (Y축 이동 대상)
    [SerializeField] private RackAndPinionController _rackAndPinion;     // deltaPosition 이벤트 소스
    [SerializeField] private SliderJointComponent _vslider1Joint;     // vslider1 (ObjectB.position 접근용)
    [SerializeField] private SpurGearController _spurGearController;// IsSignal, IsForward 참조

    [Header("바퀴 메쉬")]
    [SerializeField] private MeshFilter _xwheel1Mesh; // 셔틀 바퀴 MeshFilter (올라가는 방향 트리거 감지)
    [SerializeField] private MeshFilter _ywheel3Mesh; // 슬라이더 바퀴 MeshFilter (내려가는 방향 트리거 감지)

    [Header("파라미터")]
    [SerializeField] private float _cooldownDuration = 0.5f;  // Phase 전환 후 재전환 방지 시간 (초)
    [SerializeField] private float _returnTolerance = 0.002f;// 도달 판정 허용 오차 (m)

    // ============================================================
    // 상수
    // ============================================================

    private const float NoiseTolerance = 0.0001f; // deltaPosition 노이즈 필터 (m)

    // ============================================================
    // Phase 정의
    // ============================================================

    private enum Phase
    {
        SliderMoving,  // 슬라이더 이동 구간 (셔틀 고정)
        ShuttleMoving  // 셔틀 이동 구간 (슬라이더 월드 위치 고정)
    }

    // ============================================================
    // 런타임 상태
    // ============================================================

    private Phase _currentPhase;
    private bool _isCooldown;
    private float _cooldownTimer;

    private float _targetFloorY;        // 신호 시작 시 저장한 트리거 기준 Y 최저점
                                        // xwheel1 과 ywheel3 중 낮은 값으로 설정됨
    private Vector3 _lockedSliderLocalXZ; // ShuttleMoving 진입 시 저장한 슬라이더의 셔틀 기준 로컬 X/Z
    private float _lockedSliderWorldY;  // ShuttleMoving 진입 시 저장한 슬라이더의 월드 Y (고정 대상)
    private bool _prevSignal;          // 이전 프레임 IsSignal — 신호 시작(false→true) 감지용

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;
        Initialize();
    }

    private void Update()
    {
        TickCooldown();
        HandleSignalState();
        DetectPhaseTransition();
    }

    private void OnDestroy()
    {
        if (_rackAndPinion != null)
            _rackAndPinion.OnPositionChanged -= HandleRackMovement;
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  이벤트 구독 및 초기 Phase 를 설정한다.
     *         내려가는 방향(IsForward=true)  → SliderMoving 으로 시작
     *         올라가는 방향(IsForward=false) → ShuttleMoving 으로 시작,
     *                                          슬라이더 월드 위치 즉시 저장
     */
    private void Initialize()
    {
        _isCooldown = false;
        _cooldownTimer = 0f;

        if (_spurGearController.IsForward)
            _currentPhase = Phase.SliderMoving;
        else
        {
            _currentPhase = Phase.ShuttleMoving;
            LockSliderPosition();
        }

        _rackAndPinion.OnPositionChanged += HandleRackMovement;
    }

    // ============================================================
    // 신호 처리
    // ============================================================

    /**
     * @brief  IsSignal 의 false → true 전환(신호 시작)을 감지하여
     *         _targetFloorY 와 Phase 를 현재 위치 기준으로 재초기화한다.
     *
     *         _targetFloorY 설정 원리:
     *           내려가는 방향: 슬라이더가 아직 내려가지 않았으므로 xwheel1.Y < ywheel3.Y
     *                          → _targetFloorY = xwheel1.Y (슬라이더가 도달해야 할 목표)
     *           올라가는 방향: 슬라이더가 이미 내려가 있으므로 ywheel3.Y < xwheel1.Y
     *                          → _targetFloorY = ywheel3.Y (셔틀이 복귀해야 할 목표)
     *
     *         쿨다운 중 신호 재입력은 직전 Phase 전환이 아직 진행 중임을 의미하므로
     *         중복 신호로 판단하여 무시한다.
     */
    private void HandleSignalState()
    {
        bool currentSignal = _spurGearController.IsSignal;

        if (!_prevSignal && currentSignal)
        {
            // 쿨다운 중이면 직전 Phase 전환이 아직 진행 중 → 중복 신호로 무시
            if (_isCooldown)
            {
                Debug.LogWarning($"[DirectionSwitch] 신호 중복 감지 — 무시 | Phase={_currentPhase}");
                _prevSignal = currentSignal;
                return;
            }

            float xLow = GetMeshWorldYMin(_xwheel1Mesh);
            float yLow = GetMeshWorldYMin(_ywheel3Mesh);

            _targetFloorY = Mathf.Min(xLow, yLow);

            if (_spurGearController.IsForward)
                _currentPhase = Phase.SliderMoving;
            else
            {
                _currentPhase = Phase.ShuttleMoving;
                LockSliderPosition();
            }

            Debug.Log($"[DirectionSwitch] 신호 시작 | Phase={_currentPhase} targetFloorY={_targetFloorY:F4} (xLow={xLow:F4} yLow={yLow:F4})");
        }

        _prevSignal = currentSignal;
    }

    // ============================================================
    // Phase 전환 감지
    // ============================================================

    /**
     * @brief  매 프레임 Phase 전환 조건을 감지한다.
     *         IsSignal=true 이고 쿨다운이 해제된 경우에만 실행한다.
     */
    private void DetectPhaseTransition()
    {
        if (!_spurGearController.IsSignal || _isCooldown) return;

        if (_spurGearController.IsForward)
            CheckSliderReachedFloor();
        else
            CheckShuttleReachedFloor();
    }

    /**
     * @brief  내려가는 방향 전환 감지.
     *         ywheel3(슬라이더 바퀴) Y최저점이 _targetFloorY 에 도달하면
     *         SliderMoving → ShuttleMoving 으로 전환한다.
     *         슬라이더 바퀴가 셔틀 바퀴 높이까지 내려왔음을 의미한다.
     */
    private void CheckSliderReachedFloor()
    {
        if (_currentPhase != Phase.SliderMoving) return;

        float currentSliderY = GetMeshWorldYMin(_ywheel3Mesh);

        if (currentSliderY <= _targetFloorY + _returnTolerance)
        {
            _currentPhase = Phase.ShuttleMoving;
            LockSliderPosition();
            StartCooldown();
            Debug.Log($"[DirectionSwitch] Slider → Shuttle | 현재 높이={currentSliderY:F4}, 목표 바닥={_targetFloorY:F4}");
        }
    }

    /**
     * @brief  올라가는 방향 전환 감지.
     *         xwheel1(셔틀 바퀴) Y최저점이 _targetFloorY 에 도달하면
     *         ShuttleMoving → SliderMoving 으로 전환한다.
     *         셔틀 바퀴가 슬라이더 바퀴 높이까지 내려왔음을 의미한다.
     */
    private void CheckShuttleReachedFloor()
    {
        if (_currentPhase != Phase.ShuttleMoving) return;

        float currentShuttleY = GetMeshWorldYMin(_xwheel1Mesh);

        if (currentShuttleY <= _targetFloorY + _returnTolerance)
        {
            _currentPhase = Phase.SliderMoving;
            StartCooldown();
            Debug.Log($"[DirectionSwitch] Shuttle → Slider | 현재 높이={currentShuttleY:F4}, 목표 바닥={_targetFloorY:F4}");
        }
    }

    // ============================================================
    // 이동 처리
    // ============================================================

    /**
     * @brief  RackAndPinionController 의 deltaPosition 이벤트 핸들러.
     *         ShuttleMoving Phase 에서만 셔틀 이동 + 슬라이더 위치 고정을 수행한다.
     *         SliderMoving Phase 에서는 RackAndPinionController 가 슬라이더를 처리 → 무처리.
     * @param  deltaPosition  래크 이동량 (m)
     */
    private void HandleRackMovement(float deltaPosition)
    {
        if (Mathf.Abs(deltaPosition) < NoiseTolerance) return;

        if (_currentPhase == Phase.ShuttleMoving)
            MoveShuttleAndLockSlider(deltaPosition);
    }

    /**
     * @brief  셔틀을 deltaPosition 반대 방향으로 이동시키고
     *         슬라이더를 셔틀 기준 로컬 X/Z + 저장된 월드 Y 로 강제 복원한다.
     *
     *         셔틀 Y 이동 시: 슬라이더 Y 는 _lockedSliderWorldY 로 고정되어 변하지 않음
     *         셔틀 X/Z 이동 시: _lockedSliderLocalXZ 를 현재 셔틀 기준으로 변환하므로
     *                            슬라이더가 셔틀과 함께 따라옴
     *
     *         내려가는 방향(Phase 3): deltaPosition < 0 → 셔틀 상승
     *         올라가는 방향(Phase 1): deltaPosition > 0 → 셔틀 하강
     *
     * @param  deltaPosition  래크 이동량 (m)
     */
    private void MoveShuttleAndLockSlider(float deltaPosition)
    {
        _shuttle.position += Vector3.up * -deltaPosition;

        // 셔틀 기준 로컬 X/Z → 현재 셔틀 위치 기준 월드 X/Z 로 변환
        // Y 는 저장된 월드 Y 로 고정하여 셔틀 Y 이동이 슬라이더에 전파되지 않도록 함
        Vector3 shuttleWorldXZ = _shuttle.TransformPoint(_lockedSliderLocalXZ);
        _vslider1Joint.ObjectB.position = new Vector3(shuttleWorldXZ.x, _lockedSliderWorldY, shuttleWorldXZ.z);
    }

    /**
     * @brief  슬라이더 위치를 셔틀 기준 로컬 X/Z 오프셋과 월드 Y 로 분리하여 저장한다.
     *         ShuttleMoving 진입 시 및 올라가는 방향 신호 시작 시 호출한다.
     *         Y 성분을 월드 좌표로 별도 저장하여 셔틀 Y 이동 시 슬라이더가
     *         함께 올라가는 문제를 방지한다.
     */
    private void LockSliderPosition()
    {
        Vector3 sliderWorldPos = _vslider1Joint.ObjectB.position;
        Vector3 sliderLocalPos = _shuttle.InverseTransformPoint(sliderWorldPos);

        // X/Z 로컬 오프셋 저장 (셔틀 X/Z 이동 추적용, Y 는 0 으로 제외)
        _lockedSliderLocalXZ = new Vector3(sliderLocalPos.x, 0f, sliderLocalPos.z);

        // 월드 Y 별도 저장 (셔틀 Y 이동과 무관하게 고정)
        _lockedSliderWorldY = sliderWorldPos.y;
    }

    // ============================================================
    // 쿨다운
    // ============================================================

    /**
     * @brief  Phase 전환 후 쿨다운 타이머를 감산한다.
     *         타이머가 0 이하가 되면 쿨다운을 해제한다.
     */
    private void TickCooldown()
    {
        if (!_isCooldown) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            _isCooldown = false;
            _cooldownTimer = 0f;
        }
    }

    /**
     * @brief  쿨다운을 시작한다.
     */
    private void StartCooldown()
    {
        _isCooldown = true;
        _cooldownTimer = _cooldownDuration;
    }

    // ============================================================
    // 유틸리티
    // ============================================================

    /**
     * @brief  MeshFilter.sharedMesh.bounds 를 이용하여 월드 기준 Y 최솟값을 반환한다.
     *         Renderer.bounds(AABB) 는 바퀴 회전 시 크기가 변하여 오차가 발생하므로
     *         sharedMesh.bounds 기준으로 로컬 최저점을 계산하고 월드 좌표로 변환한다.
     * @param  meshFilter  대상 MeshFilter
     * @return float       월드 기준 Y 최솟값
     */
    private float GetMeshWorldYMin(MeshFilter meshFilter)
    {
        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 localLowest = bounds.center - new Vector3(0f, bounds.extents.y, 0f);
        return meshFilter.transform.TransformPoint(localLowest).y;
    }

    // ============================================================
    // 유효성 검사
    // ============================================================

    /**
     * @brief  Inspector 할당 필수 오브젝트의 유효성을 검사한다.
     *         하나라도 미할당 시 컴포넌트를 비활성화하고 false 를 반환한다.
     * @return bool  모두 유효하면 true
     */
    private bool ValidateComponents()
    {
        bool isValid = true;

        isValid &= AssertNotNull(_shuttle, nameof(_shuttle));
        isValid &= AssertNotNull(_rackAndPinion, nameof(_rackAndPinion));
        isValid &= AssertNotNull(_vslider1Joint, nameof(_vslider1Joint));
        isValid &= AssertNotNull(_spurGearController, nameof(_spurGearController));
        isValid &= AssertNotNull(_xwheel1Mesh, nameof(_xwheel1Mesh));
        isValid &= AssertNotNull(_ywheel3Mesh, nameof(_ywheel3Mesh));

        if (!isValid) enabled = false;
        return isValid;
    }

    /**
     * @brief  오브젝트가 null 이면 에러 로그를 출력하고 false 를 반환한다.
     * @param  obj   검사할 오브젝트
     * @param  name  필드명 (로그 출력용)
     * @return bool  null 이 아니면 true
     */
    private bool AssertNotNull(Object obj, string name)
    {
        if (obj != null) return true;

        Debug.LogError($"[DirectionSwitch] {name}이(가) 할당되지 않았습니다.");
        return false;
    }
}