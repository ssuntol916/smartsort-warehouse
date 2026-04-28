// ============================================================
// 파일명  : SpurGearController.cs
// 역할    : spurgear1 을 회전시킨다.
//           3초에 1바퀴 기준으로 _duration 초 동안 동작한다.
//           SetAngle() 을 통해 클램프를 포함한 회전을 적용한다.
//           LineBCenter 기준 position 보정으로 자전을 구현한다.
//           IsSignal 로 DirectionSwitchController 의 Phase 전환 감지를 활성화한다.
//           IsForward 로 현재 이동 방향을 외부에 노출한다.
//             true : 내려가는 방향 (슬라이더 먼저 내려감)
//             false: 올라가는 방향 (셔틀 먼저 내려감)
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 
// ============================================================

using UnityEngine;

public class SpurGearController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;        // 셔틀 최상위 Transform (이동량 계산용)
    [SerializeField] private RevoluteJointComponent _spurGear1Joint; // spurgear1 조인트

    [Header("파라미터")]
    [SerializeField] private float _duration = 0f;   // 회전 지속 시간 (초) — 3초당 1바퀴 기준
    [SerializeField] private bool _isSignal = false; // 동작 신호 (true 시 회전 시작)
    [SerializeField] private bool _isForward = true;  // 이동 방향
                                                      // true : 내려가는 방향 (슬라이더 먼저 내려감)
                                                      // false: 올라가는 방향 (셔틀 먼저 내려감)

    // ============================================================
    // 상수
    // ============================================================

    private const float SecondsPerRotation = 3f;                           // 1바퀴당 기준 시간 (초)
    private const float DegreesPerSecond = 360f / SecondsPerRotation;   // 초당 회전 각도

    // ============================================================
    // 프로퍼티
    // ============================================================

    /// DirectionSwitchController 에서 Phase 전환 감지 활성화 여부 확인용
    public bool IsSignal => _isSignal;

    /// DirectionSwitchController 에서 방향 분기 시 사용
    public bool IsForward => _isForward;

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _elapsedTime;    // 경과 시간 (초)
    private float _currentAngle;  // 현재 누적 각도
    private Vector3 _centerOffset;  // LineBCenter 기준 spurgear1 위치 오프셋 (자전 피벗 보정용)
    private Quaternion _prevRotation;  // 이전 프레임 회전값 (position 보정용)

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;
        Initialize();
    }

    /**
     * @brief  매 프레임 spurgear1 을 자전시킨다.
     *         IsSignal=true 이고 _duration > 0 인 경우에만 동작한다.
     *         _duration 경과 시 자동으로 신호를 해제한다.
     */
    private void Update()
    {
        if (!_isSignal) return;
        if (_duration <= 0f) return;

        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= _duration)
        {
            _isSignal = false;
            _elapsedTime = 0f;
            return;
        }

        RotateSpurGear();

        Debug.Log($"[SpurGear] LineBCenter={_spurGear1Joint.LineBCenter}");
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  초기 각도, 피벗 오프셋, 회전값을 저장한다.
     */
    private void Initialize()
    {
        _currentAngle = _spurGear1Joint.CurrentAngle;
        _centerOffset = _spurGear1Joint.ObjectB.position - _spurGear1Joint.LineBCenter;
        _prevRotation = _spurGear1Joint.ObjectB.rotation;

        Debug.Log($"[SpurGear] 초기화 완료 | initialAngle={_currentAngle}");
    }

    // ============================================================
    // 회전 처리
    // ============================================================

    /**
     * @brief  spurgear1 을 자전시키고 position 을 보정한다.
     *         SetAngle() 으로 클램프 포함 rotation 을 적용한 뒤
     *         rotation 변화량으로 centerOffset 을 회전시켜 자전 피벗을 보정한다.
     */
    private void RotateSpurGear()
    {
        float direction = _isForward ? 1f : -1f;
        float deltaAngle = DegreesPerSecond * Time.deltaTime * direction;
        _currentAngle += deltaAngle;

        _spurGear1Joint.SetAngle(_currentAngle);

        // rotation 변화량으로 centerOffset 갱신 → 자전 피벗 보정
        Quaternion delta = _spurGear1Joint.ObjectB.rotation * Quaternion.Inverse(_prevRotation);
        _centerOffset = delta * _centerOffset;

        _spurGear1Joint.ObjectB.position = _spurGear1Joint.LineBCenter + _centerOffset;
        _prevRotation = _spurGear1Joint.ObjectB.rotation;
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

        if (_spurGear1Joint == null)
        {
            Debug.LogError("[SpurGear] _spurGear1Joint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_shuttle == null)
        {
            Debug.LogError("[SpurGear] _shuttle 이 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}