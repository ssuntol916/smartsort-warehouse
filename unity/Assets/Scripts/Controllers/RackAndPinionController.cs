// ============================================================
// 파일명  : RackAndPinionController.cs
// 역할    : 피니언 기어(RevoluteJoint)의 회전을 랙(SliderJoint)의 직선 이동으로 변환한다.
//           deltaAngle × gearRadius = deltaPosition 공식으로 슬라이더 위치를 갱신하고
//           OnPositionChanged 이벤트로 이동량을 외부에 전달한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — ValidateComponents 추가
//                        _gearRadius 하드코딩 제거 → ObjectB 메쉬에서 자동 계산으로 변경
// ============================================================

using UnityEngine;

public class RackAndPinionController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private RevoluteJointComponent _revoluteJoint;  // 피니언 기어 조인트
    [SerializeField] private SliderJointComponent _sliderJoint;      // 랙(슬라이더) 조인트

    [Header("파라미터")]
    [SerializeField] private float _motionDirection = -1f;           // 이동 방향 부호 (+1 또는 -1)

    // ============================================================
    // 이벤트
    // ============================================================

    /// <summary>
    /// 슬라이더 위치가 변경될 때마다 발행된다.
    /// 파라미터: deltaPosition (이번 프레임 이동량, m)
    /// </summary>
    public System.Action<float> OnPositionChanged;

    // ============================================================
    // 상수
    // ============================================================

    private const float AngleJumpThresholdRad = 0.1f;       // 프레임 간 각도 점프로 판단할 임계값 (rad). 초과 시 해당 프레임 무시

    private const float PositionNoiseTolerance = 0.0001f;   // 위치 변화량 노이즈 임계값 (m). 미만이면 이동 처리 생략

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _lastAngle;       // 직전 프레임의 피니언 각도 (도)

    private float _gearRadius;      // 피니언 기어 반지름 (m)

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;

        InitializeGearRadius();
        _lastAngle = _revoluteJoint.CurrentAngle;

        // 각도 클램프 발생 시 _lastAngle 을 동기화하여 점프 오감지를 방지한다.
        _revoluteJoint.OnClampedCallback += _ => _lastAngle = _revoluteJoint.CurrentAngle;
    }

    /**
     * @brief  매 프레임 피니언 각도 변화량을 랙 이동량으로 변환하여 적용한다.
     *         AngleJumpThresholdRad 초과 시 물리 불연속으로 판단하고 해당 프레임을 스킵한다.
     */
    private void Update()
    {
        float currentAngle = _revoluteJoint.CurrentAngle;
        float deltaAngleRad = (currentAngle - _lastAngle) * Mathf.Deg2Rad;

        // 프레임 간 각도 점프 감지 → 해당 프레임 무시
        if (Mathf.Abs(deltaAngleRad) > AngleJumpThresholdRad) return;

        float deltaPosition = _motionDirection * _gearRadius * deltaAngleRad;

        // 노이즈 수준의 이동량은 처리하지 않음
        if (Mathf.Abs(deltaPosition) < PositionNoiseTolerance)
        {
            _lastAngle = currentAngle;
            return;
        }

        _sliderJoint.SetPosition(_sliderJoint.CurrentPosition + deltaPosition);
        OnPositionChanged?.Invoke(deltaPosition);
        _lastAngle = currentAngle;
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  RevoluteJoint ObjectB 의 메쉬 바운딩 박스에서 피니언 반지름을 자동 계산한다.
     *         피니언은 Z축 회전이므로 회전축(Z)을 제외한 extents.x / extents.y 중
     *         큰 값에 lossyScale.x 를 곱해 월드 기준 실제 반지름으로 변환한다.
     */
    private void InitializeGearRadius()
    {
        MeshFilter meshFilter = _revoluteJoint.ObjectB.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogError("[RackAndPinion] ObjectB 에 MeshFilter 가 없습니다.");
            enabled = false;
            return;
        }

        Bounds bounds = meshFilter.mesh.bounds;
        float localRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
        _gearRadius = localRadius * _revoluteJoint.ObjectB.lossyScale.x;

        Debug.Log($"[RackAndPinion] 초기화 완료 | gearRadius={_gearRadius:F4}m");
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

        if (_revoluteJoint == null)
        {
            Debug.LogError("[RackAndPinion] RevoluteJoint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_sliderJoint == null)
        {
            Debug.LogError("[RackAndPinion] SliderJoint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}