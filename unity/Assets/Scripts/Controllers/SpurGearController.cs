// ============================================================
// 파일명  : SpurGearController.cs
// 역할    : 스퍼 기어를 회전시켜 슬라이더를 수직 이동시킨다.
//           SetSignal() 호출로 외부에서 동작을 시작하며,
//           duration 경과 시 자동으로 신호를 해제한다.
//           _isSlider2 플래그로 슬라이더2의 방향 반전을 지원한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — 상수화, ValidateComponents 추가
//                        SetSignal 에서 ValidateComponents 호출 제거
//                        (외부 호출 시 enabled=false 부작용으로 컴포넌트가 꺼지는 버그 수정)
// ============================================================

using UnityEngine;

public class SpurGearController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;                     // 셔틀 Transform (현재 미사용, 확장용 예약)
    [SerializeField] private RevoluteJointComponent _spurGearJoint;  // 스퍼 기어 조인트

    [Header("파라미터 (읽기 전용 — SetSignal 로 자동 갱신)")]
    [SerializeField] private float _duration = 0f;      // 동작 지속 시간 (초)
    [SerializeField] private bool _isSignal = false;    // 동작 신호
    [SerializeField] private bool _isForward = true;    // 회전 방향

    [Header("슬라이더2 설정")]
    [SerializeField] private bool _isSlider2 = false;   // true 이면 IsForward 방향 반전

    // ============================================================
    // 공개 프로퍼티
    // ============================================================

    public bool IsSignal => _isSignal;                                 // 현재 동작 중 여부

    public bool IsForward => _isSlider2 ? !_isForward : _isForward;    // 유효 회전 방향. _isSlider2=true 이면 _isForward 를 반전하여 반환한다.

    // ============================================================
    // 상수
    // ============================================================

    private const float SecondsPerRotation = 3f;                       // 1바퀴 기준 시간 (초). 3초에 1회전

    private const float DegreesPerSecond = 360f / SecondsPerRotation;  // 초당 회전 각도 (도)

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _elapsedTime;          // 현재 동작 경과 시간 (초)
    private float _currentAngle;         // 누적 기어 각도 (도)
    private Vector3 _centerOffset;       // LineBCenter 기준 기어 위치 오프셋
    private Quaternion _prevRotation;    // 직전 프레임 기어 Rotation (delta 계산용)
    private bool _isInitialized;         // 초기화 완료 여부

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;
        EnsureInitialized();
    }

    /**
     * @brief  매 프레임 기어를 회전시킨다.
     *         _isSignal=false 또는 _duration<=0 이면 조기 종료한다.
     *         duration 경과 시 신호를 해제한다.
     */
    private void Update()
    {
        if (!_isSignal || _duration <= 0f) return;

        _elapsedTime += Time.deltaTime;
        if (_elapsedTime >= _duration)
        {
            _isSignal = false;
            return;
        }

        // 방향에 따른 이번 프레임 회전 각도 계산
        float direction = IsForward ? 1f : -1f;
        float deltaAngle = DegreesPerSecond * Time.deltaTime * direction;
        _currentAngle += deltaAngle;

        // 기어 각도 적용
        _spurGearJoint.SetAngle(_currentAngle);

        // 이번 프레임 회전 delta 로 피벗 오프셋 보정
        Quaternion delta = _spurGearJoint.ObjectB.rotation * Quaternion.Inverse(_prevRotation);
        _centerOffset = delta * _centerOffset;
        _spurGearJoint.ObjectB.position = _spurGearJoint.LineBCenter + _centerOffset;
        _prevRotation = _spurGearJoint.ObjectB.rotation;
    }

    // ============================================================
    // 공개 메서드
    // ============================================================

    /**
     * @brief  외부에서 기어 동작을 시작한다.
     *         Start() 이전에 호출되어도 EnsureInitialized() 로 안전하게 처리된다.
     * @param  isForward  회전 방향 (true = 정방향)
     * @param  duration   동작 지속 시간 (초)
     */
    public void SetSignal(bool isForward, float duration)
    {
        EnsureInitialized();

        _isForward = isForward;
        _duration = duration;
        _elapsedTime = 0f;
        _isSignal = true;
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  기어의 초기 상태(각도, 오프셋, 회전)를 캡처한다.
     *         이미 초기화된 경우 재실행을 건너뛴다.
     */
    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        _currentAngle = _spurGearJoint.CurrentAngle;
        _centerOffset = _spurGearJoint.ObjectB.position - _spurGearJoint.LineBCenter;
        _prevRotation = _spurGearJoint.ObjectB.rotation;
        _isInitialized = true;
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
        if (_spurGearJoint != null) return true;

        Debug.LogError("[SpurGear] SpurGearJoint 가 할당되지 않았습니다.");
        enabled = false;
        return false;
    }
}