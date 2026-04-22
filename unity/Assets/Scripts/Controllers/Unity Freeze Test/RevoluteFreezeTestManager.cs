// ============================================================
// 파일명  : RevoluteFreezeTestManager.cs
// 역할    : 구속 조건 테스트용 오브젝트 B 드래그 매니저
//           마우스 드래그로 오브젝트 B에 회전을 주어 구속 조건 및 클램프를 테스트한다.
// 작성자  : 이현화
// 작성일  : 2026-04-01
// 수정이력: 2026-04-22 - LineBCenter 기반 위치 보정 추가 (_centerPoint, _centerOffset)
//                      - _fixedRotation 추가 (누적 회전값)
//                      - OnClampedCallback 등록 (클램프 후 offset, rotation 동기화)
//                      - Update() @brief 주석 수정 (목표 위치 저장 내용 추가)
//                      - FixedUpdate() → Update() 로 변경, Rigidbody 제거
//                        (Transform 직접 제어 방식으로 리팩토링)
// ============================================================

using UnityEngine;

public class RevoluteFreezeTestManager : MonoBehaviour
{
    // Inspector 연결 필드
    [SerializeField] private Transform _objectB;                        // 테스트 대상 오브젝트 B 연결
    [SerializeField] private RevoluteJointComponent _jointComponent;    // 회전축 참조용 컴포넌트
    [SerializeField] private float _rotateSpeed = 10f;                  // 마우스 드래그 감도
    [SerializeField] private bool _useMouseY = true;                    // 마우스 입력 방향 (true: 위아래, false: 좌우)

    // 런타임 필드
    private Vector3 _centerPoint;           // [2026.04.22 추가] lineB 시작점 (위치 보정 기준)
    private Vector3 _centerOffset;          // [2026.04.22 추가] 중심점과 오브젝트 B position 의 차이
    private Quaternion _fixedRotation;      // [2026.04.22 추가] 누적 회전값

    private void Start()
    {
        if (_objectB == null)
        {
            Debug.LogWarning("RevoluteFreezeTestManager: 움직일 오브젝트(Object B)가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }

        if (_jointComponent == null)
        {
            Debug.LogWarning("RevoluteFreezeTestManager: JointComponent 가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }

        // [2026.04.22 추가] 중심점, offset, 초기 회전값 한 번만 계산
        _centerPoint = _jointComponent.LineBCenter;
        _centerOffset = _objectB.position - _centerPoint;
        _fixedRotation = _objectB.rotation;

        // [2026.04.22 추가] 클램프 후 offset, rotation 동기화 콜백 등록
        _jointComponent.OnClampedCallback = (clampedRot) =>
        {
            Quaternion delta = clampedRot * Quaternion.Inverse(_fixedRotation);
            _centerOffset = delta * _centerOffset;
            _fixedRotation = clampedRot;
        };
    }

    /**
     * @brief  마우스 오른쪽 버튼을 누르고 드래그하면 목표 회전값과 위치를 계산하여
     *         Transform 직접 제어로 즉시 적용한다.
     */
    private void Update()
    {
        if (_objectB == null) return;

        if (_jointComponent == null)
        {
            Debug.LogWarning("RevoluteFreezeTestManager: JointComponent 가 연결되지 않았습니다. \nInspector 에서 오브젝트를 연결해주세요.");
            return;
        }

        if (Input.GetMouseButton(1))
        {
            // 마우스 입력 방향에 따라 회전 변화량 계산
            float delta = Input.GetAxis(_useMouseY ? "Mouse Y" : "Mouse X") *
                _rotateSpeed * Time.deltaTime;

            // lineB.Direction 기준 회전축
            Vector3 rotationAxis = _jointComponent.ObjectBRotationAxis;

            // 회전 변화량을 Quaternion 으로 변환
            Quaternion rotation = Quaternion.AngleAxis(delta, rotationAxis);

            // [2026.04.22 수정] Transform 직접 제어로 즉시 적용
            // offset 을 회전시켜 중심점 기준 위치 보정
            _centerOffset = rotation * _centerOffset;
            _objectB.position = _centerPoint + _centerOffset;  // 위치 즉시 적용

            // 누적 회전값 업데이트 후 즉시 적용
            _fixedRotation = rotation * _fixedRotation;
            _objectB.rotation = _fixedRotation;  // 회전 즉시 적용
        }
    }
}