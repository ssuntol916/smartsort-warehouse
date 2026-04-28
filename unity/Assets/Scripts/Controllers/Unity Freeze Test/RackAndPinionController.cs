// ============================================================
// 파일명  : RackAndPinionController.cs
// 역할    : RevoluteJoint 회전 각도를 SliderJoint 이동 거리로 변환한다.
//           랙 앤 피니언 수식: 이동 거리 = 기어 반지름 × 회전 각도(rad)
//           변환된 deltaPosition 을 OnPositionChanged 이벤트로 발행한다.
//           DirectionSwitchController 가 이를 구독하여 Phase 에 따라
//           셔틀 이동 또는 슬라이더 고정을 처리한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 
// ============================================================

using UnityEngine;

public class RackAndPinionController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private RevoluteJointComponent _revoluteJoint; // spurgear1 회전 조인트
    [SerializeField] private SliderJointComponent _sliderJoint;   // vslider1 슬라이더 조인트

    [Header("파라미터")]
    [SerializeField] private float _gearRadius = 0.055f; // 기어 반지름 (m)
    [SerializeField] private float _motionDirection = -1f;    // 기어-랙 맞물림 방향 (1 또는 -1)

    // ============================================================
    // 상수
    // ============================================================

    private const float NoiseThreshold = 0.1f;    // 노이즈로 간주할 최대 각도 변화량 (rad)
    private const float MinDeltaPosition = 0.0001f; // 이벤트 발행 최소 이동량 (m)

    // ============================================================
    // 이벤트
    // ============================================================

    /// deltaPosition 계산 후 발행되는 이벤트.
    /// DirectionSwitchController 가 구독하여 Phase 에 따라 셔틀 이동 또는 슬라이더 고정을 처리한다.
    public System.Action<float> OnPositionChanged;

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _lastAngle; // 이전 프레임 회전 각도

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        _lastAngle = _revoluteJoint.CurrentAngle;

        _revoluteJoint.OnClampedCallback += _ => _lastAngle = _revoluteJoint.CurrentAngle;
    }

    /**
     * @brief  매 프레임 회전 각도 변화량을 이동 거리로 변환하고
     *         SliderJoint 위치를 업데이트한 뒤 OnPositionChanged 이벤트를 발행한다.
     *         노이즈(0.1 rad 초과) 또는 최소 이동량(0.0001 m) 미만이면 무시한다.
     */
    private void Update()
    {
        if (_revoluteJoint == null || _sliderJoint == null) return;

        float currentAngle = _revoluteJoint.CurrentAngle;
        float deltaAngleRad = (currentAngle - _lastAngle) * Mathf.Deg2Rad;

        if (Mathf.Abs(deltaAngleRad) > NoiseThreshold) return;

        float deltaPosition = _motionDirection * _gearRadius * deltaAngleRad;

        if (Mathf.Abs(deltaPosition) < MinDeltaPosition)
        {
            _lastAngle = currentAngle;
            return;
        }

        _sliderJoint.SetPosition(_sliderJoint.CurrentPosition + deltaPosition);

        Debug.Log($"[RackAndPinion] currentPos={_sliderJoint.CurrentPosition:F4}");

        // SliderMoving Phase : 슬라이더가 실제로 이동함
        // ShuttleMoving Phase: DirectionSwitchController 가 Transform 강제 복원으로 슬라이더를 고정함
        OnPositionChanged?.Invoke(deltaPosition);

        _lastAngle = currentAngle;
    }
}