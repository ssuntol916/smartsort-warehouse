// ============================================================
// 파일명  : RackAndPinionController.cs
// 역할    : RevoluteJoint 회전 각도를 SliderJoint 이동 거리로 변환한다.
//           랙 앤 피니언 수식: 이동 거리 = 기어 반지름 × 회전 각도(rad)
// 작성자  : 이현화
// 작성일  : 2026-04-22
// ============================================================

using UnityEngine;

public class RackAndPinionController : MonoBehaviour
{
    [SerializeField] private RevoluteJointComponent _revoluteJoint;  // spurgear1 회전 조인트
    [SerializeField] private SliderJointComponent _sliderJoint;      // vslider1 슬라이더 조인트
    [SerializeField] private float _gearRadius = 0.055f;             // 기어 반지름 (m)
    [SerializeField] private float _motionDirection = -1f;           // 기어-랙 맞물림 방향 (1 또는 -1)
                                                                     // 기어와 랙의 맞물리는 면에 따라 결정되는 설계 정보
                                                                     // Inspector 에서 직접 설정

    private float _lastAngle = 0f;  // 이전 프레임 각도 (delta 계산용)

    private void Start()
    {
        _lastAngle = _revoluteJoint.CurrentAngle;

        // 클램프 시 _lastAngle 동기화 (순간이동 방지)
        _revoluteJoint.OnClampedCallback += (clampedRot) =>
        {
            _lastAngle = _revoluteJoint.CurrentAngle;
        };
    }

    /**
     * @brief  매 프레임 RevoluteJoint 의 현재 각도와 이전 각도의 차이(delta)를 계산하여
     *         SliderJoint 의 이동 거리로 변환하여 적용한다.
     *         전체 각도가 아닌 delta 를 사용하는 이유:
     *         클램프 시 각도가 순간적으로 크게 변하는 것을 방지하기 위함
     *         delta 가 0.1f(rad) 초과이면 순간이동으로 판단하여 무시하고,
     *         deltaPosition 이 0.0001f 미만이면 각도 계산 노이즈로 판단하여 SetPosition() 호출을 생략한다.
     *         _motionDirection 으로 기어-랙 맞물림 방향을 보정한다.
     */
    private void Update()
    {
        if (_revoluteJoint == null || _sliderJoint == null) return;

        // 현재 각도와 이전 각도의 차이(delta) 계산
        float currentAngle = _revoluteJoint.CurrentAngle;
        float deltaAngleRad = (currentAngle - _lastAngle) * Mathf.Deg2Rad;

        // delta가 너무 크면 순간이동으로 판단하고 무시
        // _lastAngle 을 갱신하지 않아야 다음 프레임에서 누락된 delta 를 이어받을 수 있음
        if (Mathf.Abs(deltaAngleRad) > 0.1f)
        {
            return;  // _lastAngle 갱신 없이 return
        }

        // 랙 앤 피니언 수식: 이동 거리 = 기어 반지름 × 회전 각도(rad)
        // _motionDirection: 기어와 랙의 맞물리는 면에 따라 결정되는 방향 보정값 (1 또는 -1)
        float deltaPosition = _motionDirection * _gearRadius * deltaAngleRad;

        // delta가 너무 작으면 각도 계산 노이즈로 판단하고 SetPosition() 호출 생략
        // 이유: _objectBRotationAxis 미세 흔들림으로 인해 currentAngle 이 매 프레임
        //       조금씩 변하는 현상이 있어, 그 오차가 누적되어 vslider1 이 서서히
        //       이동하는 문제를 방지하기 위함
        if (Mathf.Abs(deltaPosition) < 0.0001f)
        {
            _lastAngle = currentAngle;
            return;
        }

        // SliderJoint 에 현재 위치 + delta 이동 거리 적용
        _sliderJoint.SetPosition(_sliderJoint.CurrentPosition + deltaPosition);

        // 이전 프레임 각도 업데이트
        _lastAngle = currentAngle;
    }
}