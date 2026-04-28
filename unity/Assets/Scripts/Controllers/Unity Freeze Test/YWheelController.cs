// ============================================================
// 파일명  : YWheelController.cs
// 역할    : ywheel1~4 를 자전시키고 셔틀을 X축 방향으로 이동시킨다.
//           3초에 1바퀴 기준으로 _duration 초 동안 동작한다.
//           회전축은 RevoluteJointComponent 에서 가져오고
//           실제 회전은 Transform 을 직접 제어한다.
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// ============================================================

using UnityEngine;

public class YWheelController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private Transform _shuttle;       // 셔틀 최상위 Transform (X축 이동 대상)
    [SerializeField] private RevoluteJointComponent _ywheel1Joint; // ywheel1 조인트
    [SerializeField] private RevoluteJointComponent _ywheel2Joint; // ywheel2 조인트
    [SerializeField] private RevoluteJointComponent _ywheel3Joint; // ywheel3 조인트
    [SerializeField] private RevoluteJointComponent _ywheel4Joint; // ywheel4 조인트

    [Header("파라미터")]
    [SerializeField] private float _duration = 0f;   // 이동 지속 시간 (초) — 3초당 1바퀴 기준
    [SerializeField] private bool _isSignal = false; // 동작 신호 (true 시 회전 시작)
    [SerializeField] private bool _isForward = true;  // 이동 방향 (true: 정방향, false: 역방향)

    // ============================================================
    // 상수
    // ============================================================

    private const float SecondsPerRotation = 3f;
    private const float DegreesPerSecond = 360f / SecondsPerRotation;

    // ============================================================
    // 런타임 상태
    // ============================================================

    private float _wheelRadius; // 바퀴 반지름 (ywheel1 메쉬에서 자동 계산)
    private float _elapsedTime; // 경과 시간 (초)

    // LineBCenter 기준 각 바퀴의 위치 오프셋 (자전 피벗 보정용)
    private Vector3 _centerOffset1;
    private Vector3 _centerOffset2;
    private Vector3 _centerOffset3;
    private Vector3 _centerOffset4;

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;
        InitializeWheelRadius();
        InitializeCenterOffsets();

        Debug.Log($"[YWheel] 초기화 완료 | wheelRadius={_wheelRadius}");
    }

    /**
     * @brief  매 프레임 ywheel1~4 를 자전시키고 셔틀을 X축으로 이동시킨다.
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

        float direction = _isForward ? 1f : -1f;
        float deltaAngle = DegreesPerSecond * Time.deltaTime * direction;

        Quaternion rotation = Quaternion.AngleAxis(deltaAngle, _ywheel1Joint.ObjectBRotationAxis);

        RotateWheel(_ywheel1Joint, rotation, ref _centerOffset1);
        RotateWheel(_ywheel2Joint, rotation, ref _centerOffset2);
        RotateWheel(_ywheel3Joint, rotation, ref _centerOffset3);
        RotateWheel(_ywheel4Joint, rotation, ref _centerOffset4);

        float moveDistance = _wheelRadius * deltaAngle * Mathf.Deg2Rad;
        _shuttle.position += Vector3.right * moveDistance;
    }

    // ============================================================
    // 초기화
    // ============================================================

    /**
     * @brief  ywheel1 메쉬 바운딩 박스에서 바퀴 반지름을 자동 계산한다.
     *         extents.y = Y축 절반 크기 = 반지름 (로컬 좌표)
     *         lossyScale.y 를 곱해 월드 기준 실제 반지름으로 변환한다.
     */
    private void InitializeWheelRadius()
    {
        Bounds bounds = _ywheel1Joint.ObjectB.GetComponent<MeshFilter>().mesh.bounds;
        _wheelRadius = bounds.extents.y * _ywheel1Joint.ObjectB.lossyScale.y;
    }

    /**
     * @brief  LineBCenter 기준 각 바퀴의 초기 위치 오프셋을 저장한다.
     */
    private void InitializeCenterOffsets()
    {
        _centerOffset1 = _ywheel1Joint.ObjectB.position - _ywheel1Joint.LineBCenter;
        _centerOffset2 = _ywheel2Joint.ObjectB.position - _ywheel2Joint.LineBCenter;
        _centerOffset3 = _ywheel3Joint.ObjectB.position - _ywheel3Joint.LineBCenter;
        _centerOffset4 = _ywheel4Joint.ObjectB.position - _ywheel4Joint.LineBCenter;
    }

    // ============================================================
    // 회전 처리
    // ============================================================

    /**
     * @brief  바퀴 하나를 LineBCenter 기준으로 자전시킨다.
     *         오프셋을 회전시켜 피벗을 보정한 뒤 rotation 을 적용한다.
     * @param  joint     회전 대상 RevoluteJointComponent
     * @param  rotation  적용할 회전 Quaternion
     * @param  offset    LineBCenter 기준 바퀴 위치 오프셋 (ref 로 갱신됨)
     */
    private void RotateWheel(RevoluteJointComponent joint, Quaternion rotation, ref Vector3 offset)
    {
        offset = rotation * offset;
        joint.ObjectB.position = joint.LineBCenter + offset;
        joint.ObjectB.rotation = rotation * joint.ObjectB.rotation;
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

        if (_ywheel1Joint == null || _ywheel2Joint == null ||
            _ywheel3Joint == null || _ywheel4Joint == null)
        {
            Debug.LogError("[YWheel] ywheel 조인트가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_shuttle == null)
        {
            Debug.LogError("[YWheel] _shuttle 이 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}