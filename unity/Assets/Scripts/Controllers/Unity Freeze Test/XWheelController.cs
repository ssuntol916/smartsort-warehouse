// ============================================================
// 파일명  : XWheelController.cs
// 역할    : xwheel1~4 를 회전시키고 셔틀 오브젝트를 Z축 방향으로 이동시킨다.
//           3초에 1바퀴 기준으로 _duration 초 동안 동작한다.
//           회전축은 RevoluteJointComponent 에서 가져오고 실제 회전은 Transform 직접 제어한다.
// 작성자  : 이현화
// 작성일  : 2026-04-22
// ============================================================

using UnityEngine;

public class XWheelController : MonoBehaviour
{
    [SerializeField] private Transform _shuttle;                        // 셔틀 최상위 부모 (Z축 이동 대상)
    [SerializeField] private RevoluteJointComponent _xwheel1Joint;     // 바퀴 1 조인트
    [SerializeField] private RevoluteJointComponent _xwheel2Joint;     // 바퀴 2 조인트
    [SerializeField] private RevoluteJointComponent _xwheel3Joint;     // 바퀴 3 조인트
    [SerializeField] private RevoluteJointComponent _xwheel4Joint;     // 바퀴 4 조인트
    [SerializeField] private float _duration;                          // 이동 지속 시간 (초) — 3초당 1바퀴 기준
    [SerializeField] private bool _isSignal = false;                   // 신호 on/off (테스트용)
    [SerializeField] private bool _isForward = true;                   // 이동 방향 (true: 정방향, false: 역방향)

    private const float SecondsPerRotation = 3f;                           // 1바퀴당 걸리는 시간 (초)
    private const float DegreesPerSecond = 360f / SecondsPerRotation;     // 초당 회전 각도

    private float _wheelRadius;                 // 바퀴 반지름 (메쉬에서 자동 계산)
    private float _elapsedTime;                 // 경과 시간 (초)
    private Vector3 _initialShuttlePosition;    // 셔틀 초기 위치 (이동량 계산용)

    // LineBCenter 기준 각 바퀴의 초기 offset (자전 피벗 보정용)
    private Vector3 _centerOffset1;  // LineBCenter 기준 xwheel1 위치 offset
    private Vector3 _centerOffset2;  // LineBCenter 기준 xwheel2 위치 offset
    private Vector3 _centerOffset3;  // LineBCenter 기준 xwheel3 위치 offset
    private Vector3 _centerOffset4;  // LineBCenter 기준 xwheel4 위치 offset

    /**
     * @brief  유효성 검사 → 반지름 계산 → offset 초기화 → 셔틀 초기 위치 저장 순서로 실행한다.
     */
    private void Start()
    {
        if (!ValidateComponents()) return;
        InitializeWheelRadius();
        InitializeCenterOffsets();
        _initialShuttlePosition = _shuttle.position;  // 셔틀 초기 위치 저장 (이동량 계산용)

        Debug.Log($"XWheelController: wheelRadius = {_wheelRadius}");
    }

    /**
     * @brief  매 프레임 바퀴를 자전시키고 셔틀 오브젝트를 Z축 방향으로 이동시킨다.
     *         _isSignal 이 true 일 때만 동작하며 _duration 초 동안 실행된다.
     *         _isForward 로 이동 방향을 결정한다 (true: 정방향, false: 역방향).
     *         모든 바퀴에 동일한 회전 Quaternion 을 적용하며 월드 기준으로 회전한다.
     *         LineBCenter 기준 offset 과 셔틀 이동량을 반영하여 자전을 구현한다.
     */
    private void Update()
    {
        if (!_isSignal) return;          // 신호 없으면 실행 안 함
        if (_duration <= 0) return;      // duration 이 없으면 실행 안 함

        _elapsedTime += Time.deltaTime;  // 경과 시간 누적

        if (_elapsedTime >= _duration)   // duration 초과 시 신호 초기화
        {
            _isSignal = false;
            _elapsedTime = 0f;
            return;
        }

        // 이동 방향 보정 (정방향: 1, 역방향: -1)
        float direction = _isForward ? 1f : -1f;

        // 이번 프레임 회전 각도 계산
        float anglePerFrame = DegreesPerSecond * Time.deltaTime * direction;

        // 월드 기준 회전 Quaternion (모든 바퀴에 동일하게 적용)
        Quaternion rotation = Quaternion.AngleAxis(-anglePerFrame, _xwheel1Joint.ObjectBRotationAxis);

        // 셔틀 이동량 계산 (초기 위치 기준)
        Vector3 shuttleOffset = _shuttle.position - _initialShuttlePosition;

        // 모든 바퀴에 동일한 회전 적용
        RotateWheel(_xwheel1Joint, rotation, ref _centerOffset1, shuttleOffset);
        RotateWheel(_xwheel2Joint, rotation, ref _centerOffset2, shuttleOffset);
        RotateWheel(_xwheel3Joint, rotation, ref _centerOffset3, shuttleOffset);
        RotateWheel(_xwheel4Joint, rotation, ref _centerOffset4, shuttleOffset);

        // 이동 거리 계산: 바퀴 반지름 × 회전각(rad)
        float moveDistance = _wheelRadius * anglePerFrame * Mathf.Deg2Rad;

        // 셔틀 Z축 방향으로 이동
        _shuttle.position += Vector3.forward * moveDistance;
    }

    /**
     * @brief  필수 오브젝트 유효성 검사.
     *         wheel 조인트 또는 셔틀이 할당되지 않으면 false 를 반환하고 컴포넌트를 비활성화한다.
     * @return bool  유효하면 true, 아니면 false
     */
    private bool ValidateComponents()
    {
        if (_xwheel1Joint == null || _xwheel2Joint == null ||
            _xwheel3Joint == null || _xwheel4Joint == null)
        {
            Debug.LogError("XWheelController: wheel 조인트가 할당되지 않았습니다.");
            enabled = false;  // Update 호출 중단 (NullReferenceException 방지)
            return false;
        }

        if (_shuttle == null)
        {
            Debug.LogError("XWheelController: 셔틀 오브젝트가 할당되지 않았습니다.");
            enabled = false;
            return false;
        }

        return true;
    }

    /**
     * @brief  xwheel1 의 메쉬 바운딩 박스에서 바퀴 반지름을 자동 계산한다.
     *         extents.y = 바운딩 박스 Y축 절반 크기 = 반지름 (로컬 좌표 기준)
     *         lossyScale.y = 월드 기준 실제 스케일 → 실제 반지름으로 변환
     */
    private void InitializeWheelRadius()
    {
        MeshFilter meshFilter = _xwheel1Joint.ObjectB.GetComponent<MeshFilter>();
        Bounds bounds = meshFilter.mesh.bounds;
        _wheelRadius = bounds.extents.y * _xwheel1Joint.ObjectB.lossyScale.y;
    }

    /**
     * @brief  LineBCenter 기준 각 바퀴의 초기 offset 을 계산한다.
     *         RevoluteFreezeTestManager 와 동일한 방식으로 중심점과의 차이를 저장한다.
     */
    private void InitializeCenterOffsets()
    {
        _centerOffset1 = _xwheel1Joint.ObjectB.position - _xwheel1Joint.LineBCenter;
        _centerOffset2 = _xwheel2Joint.ObjectB.position - _xwheel2Joint.LineBCenter;
        _centerOffset3 = _xwheel3Joint.ObjectB.position - _xwheel3Joint.LineBCenter;
        _centerOffset4 = _xwheel4Joint.ObjectB.position - _xwheel4Joint.LineBCenter;
    }

    /**
     * @brief  LineBCenter 기준 offset 과 셔틀 이동량을 반영하여 바퀴를 자전시킨다.
     *         RevoluteFreezeTestManager 와 동일한 방식으로 중심점 기준 위치 보정 후 회전 적용한다.
     * @param  joint          회전 대상 RevoluteJointComponent
     * @param  rotation       적용할 회전 Quaternion
     * @param  offset         LineBCenter 기준 바퀴 위치 offset (ref 로 갱신됨)
     * @param  shuttleOffset  셔틀 이동량 (초기 위치 기준)
     */
    private void RotateWheel(RevoluteJointComponent joint, Quaternion rotation, ref Vector3 offset, Vector3 shuttleOffset)
    {
        // offset 을 회전시켜 중심점 기준 위치 보정
        offset = rotation * offset;

        // LineBCenter + 셔틀 이동량 + offset 으로 위치 보정
        joint.ObjectB.position = joint.LineBCenter + shuttleOffset + offset;

        // 회전 적용
        joint.ObjectB.rotation = rotation * joint.ObjectB.rotation;
    }
}