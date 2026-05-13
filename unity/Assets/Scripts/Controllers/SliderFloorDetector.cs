// ============================================================
// 파일명  : SliderFloorDetector.cs
// 역할    : 슬라이더 동작 중 xwheel / ywheel 의 Y 최솟값을 기준으로
//           "바닥 도달" 여부를 감지하여 이벤트를 발행한다.
//           IsGoingDown=true  → ywheel 기준 → OnFloorReached
//           IsGoingDown=false → xwheel 기준 → OnShuttleFloorReached
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — ValidateComponents 추가
// ============================================================

using UnityEngine;

public class SliderFloorDetector : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private SpurGearController _spurGearController;    // 감시 대상 SpurGear
    [SerializeField] private RevoluteJointComponent _xwheel1Joint;      // X 바퀴 조인트 (상승 종점 판정)
    [SerializeField] private RevoluteJointComponent _ywheelJoint;       // Y 바퀴 조인트 (하강 종점 판정)

    [Header("파라미터")]
    [SerializeField] private float _wheelRadius = 0.1399504f;           // 바퀴 반지름 (m) — Y 최솟값 보정
    [SerializeField] private float _returnTolerance = 0.002f;           // 도달 판정 허용 오차 (m)

    [Header("슬라이더2 설정")]
    [SerializeField] private bool _isSlider2 = false;                   // true 이면 IsGoingDown 방향 반전

    // ============================================================
    // 이벤트
    // ============================================================

    public System.Action OnFloorReached;            // 하강 종점 도달 시 발행

    public System.Action OnShuttleFloorReached;     // 상승 종점 도달 시 발행

    // ============================================================
    // 공개 프로퍼티
    // ============================================================

    // 현재 하강 방향 여부. _isSlider2=true 이면 SpurGear 방향을 반전하여 반환한다.
    public bool IsGoingDown => _isSlider2 ? !_spurGearController.IsForward : _spurGearController.IsForward;

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _targetFloorY;   // 신호 시작 시점에 캡처한 기준 Y 최솟값
    private bool _prevSignal;      // 직전 프레임 IsSignal 값 (상승 엣지 감지용)
    private bool _floorReached;    // 이미 도달 이벤트를 발행했는지 여부

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        ValidateComponents();
    }

    /**
     * @brief  SpurGear 신호 상승 엣지에서 기준 Y 를 캡처하고,
     *         매 프레임 바퀴 Y 최솟값이 기준에 도달했는지 확인한다.
     */
    private void Update()
    {
        bool currentSignal = _spurGearController.IsSignal;

        // 신호 상승 엣지: 기준 Y 캡처 및 도달 플래그 초기화
        if (!_prevSignal && currentSignal)
        {
            float xLow = GetWheelWorldYMin(_xwheel1Joint);
            float yLow = GetWheelWorldYMin(_ywheelJoint);
            _targetFloorY = Mathf.Min(xLow, yLow);
            _floorReached = false;
        }
        _prevSignal = currentSignal;

        // 신호 없음 또는 이미 도달했으면 검사 생략
        if (!_spurGearController.IsSignal || _floorReached) return;

        CheckFloorReached();
    }

    // ============================================================
    // 도달 판정
    // ============================================================

    /**
     * @brief  이동 방향에 따라 적절한 바퀴의 Y 최솟값이 기준에 도달했는지 판정한다.
     *         도달 시 _floorReached 를 true 로 설정하고 해당 이벤트를 발행한다.
     */
    private void CheckFloorReached()
    {
        if (IsGoingDown)
        {
            // 하강 중: ywheel 이 기준 Y 에 도달했는지 확인
            if (GetWheelWorldYMin(_ywheelJoint) <= _targetFloorY + _returnTolerance)
            {
                _floorReached = true;
                OnFloorReached?.Invoke();
                Debug.Log("[SliderFloorDetector] 하강 종점 도달");
            }
        }
        else
        {
            // 상승 중: xwheel 이 기준 Y 에 도달했는지 확인
            if (GetWheelWorldYMin(_xwheel1Joint) <= _targetFloorY + _returnTolerance)
            {
                _floorReached = true;
                OnShuttleFloorReached?.Invoke();
                Debug.Log("[SliderFloorDetector] 상승 종점 도달");
            }
        }
    }

    // ============================================================
    // 헬퍼
    // ============================================================

    /**
     * @brief  조인트 LineBCenter.y 에서 바퀴 반지름을 뺀 월드 Y 최솟값을 반환한다.
     * @param  joint  대상 RevoluteJointComponent
     * @return float  바퀴 하단 월드 Y 좌표
     */
    private float GetWheelWorldYMin(RevoluteJointComponent joint)
    {
        return joint.LineBCenter.y - _wheelRadius;
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

        if (_spurGearController == null)
        {
            Debug.LogError("[SliderFloorDetector] SpurGearController 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_xwheel1Joint == null)
        {
            Debug.LogError("[SliderFloorDetector] xwheel1Joint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_ywheelJoint == null)
        {
            Debug.LogError("[SliderFloorDetector] ywheelJoint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}