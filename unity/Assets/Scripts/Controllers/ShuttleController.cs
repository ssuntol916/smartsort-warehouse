// ============================================================
// 파일명  : ShuttleController.cs
// 역할    : 슬라이더 하강 종점 도달 시 셔틀을 수직 이동시키고,
//           상승 종점 도달 시 이동을 종료한다.
//           RackAndPinion 의 위치 변화량을 받아 셔틀을 Y축으로 이동시키며
//           vslider 위치를 셔틀에 고정(잠금)하여 함께 이동시킨다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — OnDestroy 이벤트 해제, 불필요 초기화 제거, ValidateComponents 추가
//                        HandleRackMovement 방향 일치 조건 복원
//                        (제거 시 Slider2LockController 추종 보정이 퐁 튀는 현상 발생)
// ============================================================

using UnityEngine;

public class ShuttleController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;                         // 셔틀 최상위 Transform
    [SerializeField] private SliderJointComponent _vsliderJoint;         // 수직 슬라이더 조인트
    [SerializeField] private RackAndPinionController _rackAndPinion;     // 랙앤피니언 컨트롤러
    [SerializeField] private SliderFloorDetector _sliderFloorDetector;   // 바닥 감지기
    [SerializeField] private SpurGearController _spurGearController;     // 스퍼 기어 컨트롤러

    // ============================================================
    // 이벤트
    // ============================================================

    public System.Action OnShuttleMovingEnd;       // 셔틀 이동이 완전히 종료될 때 발행된다.

    // ============================================================
    // 상수
    // ============================================================

    private const float NoiseTolerance = 0.0001f;  // 랙 이동량 노이즈 임계값(m)

    // ============================================================
    // 런타임 상태
    // ============================================================

    private bool _isShuttleMoving;          // 셔틀 이동 활성 여부
    private Vector3 _lockedSliderLocalXZ;   // 셔틀 로컬 좌표계 기준 슬라이더 XZ 오프셋 (잠금 시 캡처)
    private float _lockedSliderWorldY;      // 슬라이더 잠금 시점의 월드 Y 좌표
    private bool _prevSignal;               // 직전 프레임 SpurGear 신호 (상승 엣지 감지용)

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;

        // 이벤트 구독
        _sliderFloorDetector.OnFloorReached += OnSliderFloorReached;
        _sliderFloorDetector.OnShuttleFloorReached += OnShuttleFloorReached;
        _rackAndPinion.OnPositionChanged += HandleRackMovement;
    }

    /**
     * @brief  이벤트 구독을 해제한다.
     *         해제하지 않으면 오브젝트 파괴 후에도 콜백이 호출되어 메모리 누수가 발생한다.
     */
    private void OnDestroy()
    {
        if (_sliderFloorDetector != null)
        {
            _sliderFloorDetector.OnFloorReached -= OnSliderFloorReached;
            _sliderFloorDetector.OnShuttleFloorReached -= OnShuttleFloorReached;
        }

        if (_rackAndPinion != null)
            _rackAndPinion.OnPositionChanged -= HandleRackMovement;
    }

    /**
     * @brief  SpurGear 신호 상승 엣지에서 역방향(상승) 시작 시 셔틀 이동을 준비한다.
     *         정방향(하강) 시작은 OnSliderFloorReached 이벤트에서 처리한다.
     */
    private void Update()
    {
        bool currentSignal = _spurGearController.IsSignal;

        // 신호 상승 엣지 감지
        if (!_prevSignal && currentSignal)
        {
            // 역방향(상승): 즉시 셔틀 이동 시작 및 슬라이더 위치 잠금
            if (!_spurGearController.IsForward)
            {
                _isShuttleMoving = true;
                LockSliderPosition();
            }
        }

        _prevSignal = currentSignal;
    }

    // ============================================================
    // 이벤트 핸들러
    // ============================================================

    /**
     * @brief  하강 종점 도달 시 셔틀 이동을 시작하고 슬라이더 위치를 잠근다.
     */
    private void OnSliderFloorReached()
    {
        _isShuttleMoving = true;
        LockSliderPosition();
        Debug.Log("[Shuttle] 하강 종점 도달 → 셔틀 이동 시작");
    }

    /**
     * @brief  상승 종점 도달 시 셔틀 이동을 종료하고 OnShuttleMovingEnd 를 발행한다.
     */
    private void OnShuttleFloorReached()
    {
        _isShuttleMoving = false;
        OnShuttleMovingEnd?.Invoke();
        Debug.Log("[Shuttle] 상승 종점 도달 → 셔틀 이동 종료");
    }

    // ============================================================
    // 이동 처리
    // ============================================================

    /**
     * @brief  RackAndPinion 위치 변화량을 받아 셔틀을 Y축으로 이동시킨다.
     *         노이즈 필터, 이동 활성 여부, 방향 일치 조건을 모두 통과해야 이동한다.
     *
     *         방향 일치 조건: SliderFloorDetector.IsGoingDown == SpurGear.IsForward
     *         이 조건이 없으면 슬라이더2 상승 중 ShuttleController 가 셔틀을 잘못 이동시켜
     *         Slider2LockController 의 추종 보정이 퐁 튀는 현상이 발생한다.
     * @param  deltaPosition  랙 이동량 (m)
     */
    private void HandleRackMovement(float deltaPosition)
    {
        if (Mathf.Abs(deltaPosition) < NoiseTolerance) return;
        if (!_isShuttleMoving) return;
        if (_sliderFloorDetector.IsGoingDown != _spurGearController.IsForward) return;

        MoveShuttleAndLockSlider(deltaPosition);
    }

    /**
     * @brief  셔틀을 Y축으로 이동시키고, 잠긴 슬라이더 위치를 셔틀에 추종시킨다.
     * @param  deltaPosition  이번 프레임 이동량 (m)
     */
    private void MoveShuttleAndLockSlider(float deltaPosition)
    {
        // deltaPosition 이 양수이면 랙이 내려가므로 셔틀은 위로 이동 (부호 반전)
        _shuttle.position += Vector3.up * -deltaPosition;

        // 슬라이더를 셔틀 로컬 XZ + 잠금 월드 Y 로 고정
        Vector3 shuttleWorldXZ = _shuttle.TransformPoint(_lockedSliderLocalXZ);
        _vsliderJoint.ObjectB.position = new Vector3(
            shuttleWorldXZ.x,
            _lockedSliderWorldY,
            shuttleWorldXZ.z
        );
    }

    /**
     * @brief  현재 슬라이더 위치를 셔틀 로컬 XZ + 월드 Y 로 분리하여 캡처한다.
     *         이후 MoveShuttleAndLockSlider 에서 슬라이더가 셔틀을 따라 이동할 때 기준으로 사용한다.
     */
    private void LockSliderPosition()
    {
        Vector3 sliderWorldPos = _vsliderJoint.ObjectB.position;
        Vector3 sliderLocalPos = _shuttle.InverseTransformPoint(sliderWorldPos);
        _lockedSliderLocalXZ = new Vector3(sliderLocalPos.x, 0f, sliderLocalPos.z);
        _lockedSliderWorldY = sliderWorldPos.y;
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

        if (_shuttle == null)
        {
            Debug.LogError("[Shuttle] _shuttle 이 할당되지 않았습니다.");
            isValid = false;
        }

        if (_vsliderJoint == null)
        {
            Debug.LogError("[Shuttle] VsliderJoint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_rackAndPinion == null)
        {
            Debug.LogError("[Shuttle] RackAndPinionController 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_sliderFloorDetector == null)
        {
            Debug.LogError("[Shuttle] SliderFloorDetector 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_spurGearController == null)
        {
            Debug.LogError("[Shuttle] SpurGearController 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}