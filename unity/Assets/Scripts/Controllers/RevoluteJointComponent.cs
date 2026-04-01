// ============================================================
// 파일명  : RevoluteJointComponent.cs
// 역할    : RevoluteJoint 를 씬 오브젝트에 연결하는 MonoBehaviour 컴포넌트
// 작성자  : 이현화
// 작성일  : 2026-03-30
// 수정이력: 
// ============================================================

using UnityEngine;
using UnityEngine.EventSystems;

public class RevoluteJointComponent : MonoBehaviour
{
    [SerializeField] private Transform _objectA;                         // 오브젝트 A (회전축 기준)
    [SerializeField] private Transform _objectB;                         // 오브젝트 B (회전 대상)
    [SerializeField] private float _minAngle = -90f;                     // 최소 회전 각도 (degree)
    [SerializeField] private float _maxAngle = 90f;                      // 최대 회전 각도 (degree)
    [SerializeField] private Vector3 _rotationAxis = Vector3.right;      // 회전축

    private RevoluteJoint _joint;                    // RevoluteJoint.cs 인스턴스
    private Rigidbody _rigidbodyA;                   // 오브젝트 A 리지드바디
    private Rigidbody _rigidbodyB;                   // 오브젝트 B 리지드바디
    private RigidbodyConstraints _freezeConstraint;  // 이동 방향 기준 프리즈 조건 (캐시)

    private bool _wasConstrained;                    // 이전 프레임 구속 상태
    private Vector3 _initialDirection;               // 초기 기준 방향 (각도 측정용)
    private Quaternion _initialRotation;             // 초기 회전값 (최종 회전 적용용)
    private const float ApplyTolerance = 0.001f;     // 물리 적용 판별 허용 오차

    public float CurrentAngle => _joint?.CurrentAngle ?? 0f;   // 현재 회전 각도
    public Vector3 RotationAxis => _rotationAxis;

    /**
     * @brief  오브젝트 A·B 의 Transform 을 기반으로 회전축 Line 을 생성하고
     *         RevoluteJoint 를 초기화한다.
     *         transform.position 을 선의 시작점,
     *         transform.position + _rotationAxis 를 선의 끝점으로 사용한다.
     */
    private void Awake()
    {
        // 오브젝트 리지드바디 생성 및 고정 오브젝트(Kinematic) 설정 확인
        _rigidbodyA = InitializeRigidbody(_objectA, true);
        _rigidbodyB = InitializeRigidbody(_objectB, false);

        _freezeConstraint = GetFreezeConstraintByDirection();

        // 절대적인 World X축이 아닌, _objectA가 회전한 만큼 _rotationAxis도 같이 회전시켜서 로컬 축으로 만듦
        Vector3 localAxisA = _objectA.rotation * _rotationAxis;
        Vector3 localAxisB = _objectB.rotation * _rotationAxis;

        // 오브젝트 A·B 의 Transform 에서 회전축 Line 생성
        Line lineA = new Line(_objectA.position, 
                              _objectA.position + localAxisA);
        Line lineB = new Line(_objectB.position, 
                              _objectB.position + localAxisB);

        // RevoluteJoint.cs 인스턴스 생성
        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);

        _initialRotation = _objectB.rotation;

        // 회전축과 수직인 벡터를 구해서 초기 기준 방향으로 저장
        Vector3 perpendicular = Vector3.Cross(_rotationAxis, Vector3.forward);

        if (perpendicular.magnitude < ApplyTolerance)
        {
            perpendicular = Vector3.Cross(_rotationAxis, Vector3.right);
        }

        _initialDirection = _objectB.rotation * perpendicular;
    }

    /**
     * @brief  매 프레임 RevoluteJoint.ApplyConstraint() 를 호출하여
     *         구속 조건을 오브젝트 B 의 Transform 에 반영한다.
     */
    private void Update()
    {
        if (_joint == null || !_joint.IsValid()) return;

        // 구속 조건 등록 상태 확인
        bool isConstrained = _joint.ApplyConstraint();

        // 구속 상태가 변경됐을 때만 로그 출력
        if (isConstrained != _wasConstrained)
        {
            Debug.Log($"RevoluteJoint 구속 상태 변경: {(isConstrained ? "구속됨" : "해제됨")}");
            _wasConstrained = isConstrained;
        }

        // 구속 조건이 참이면 프리즈, 아니면 해제
        if (isConstrained)
        {
            _rigidbodyB.constraints = _freezeConstraint;

            // 절대적인 World 축이 아닌, _objectA가 회전한 만큼 _rotationAxis도 같이 회전시켜서 로컬 축으로 만듦
            Vector3 localAxis = _objectA.rotation * _rotationAxis;

            // 회전축과 수직인 벡터를 구한다
            Vector3 perpendicular = Vector3.Cross(_rotationAxis, Vector3.forward);

            // 회전축과 forward가 평행할 때 forward 대신 right 로 대체
            if (perpendicular.magnitude < ApplyTolerance)
            {
                perpendicular = Vector3.Cross(_rotationAxis, Vector3.right);
            }

            // 오브젝트 B의 현재 방향 벡터를 구한다
            Vector3 currentDirection = _objectB.rotation * perpendicular;

            // 현재 회전값을 읽어서 min/max 범위 안으로 클램프된 회전값을 구한다
            Quaternion clampedRot = _joint.GetClampedRotation(
                _initialDirection, currentDirection, localAxis, _initialRotation);

            // 현재 회전값과 클램프된 회전값에 차이가 있다면 회전을 적용합니다.
            if (Quaternion.Angle(_objectB.rotation, clampedRot) > ApplyTolerance)
            {
                _rigidbodyB.MoveRotation(clampedRot);
            }
        }
        else
        {
            _rigidbodyB.constraints = RigidbodyConstraints.None;
        }
    }

    /**
     * @brief  외부에서 회전 각도를 설정한다.
     *         ShuttleController.cs 에서 임펠러 각도 제어 시 호출한다.
     *         RevoluteJoint.SetAngle() 에 위임한다.
     * @param  angle    설정할 회전 각도 (degree)
     */
    public void SetAngle(float angle)
    {
        _joint?.SetAngle(angle);
    }

    /**
     * @brief  Inspector 에서 값 변경 시 Line 을 재생성하고 RevoluteJoint 를 재초기화한다.
     *         Play 모드에서 _objectA·B 가 바뀌면 Rigidbody 참조·프리즈 조건도 갱신한다.
     */
    private void OnValidate()
    {
        if (_objectA == null || _objectB == null) return;

        // Play 모드: ObjectA·B 변경 시 Rigidbody 참조·프리즈 조건·초기 회전 기준값 갱신
        if (Application.isPlaying)
        {
            _rigidbodyA       = _objectA.GetComponent<Rigidbody>();
            _rigidbodyB       = _objectB.GetComponent<Rigidbody>();
            _freezeConstraint = GetFreezeConstraintByDirection();

            // _initialDirection / _initialRotation 은 Update() 회전 클램프 계산의 기준이므로
            // ObjectB 가 바뀌면 새 ObjectB 기준으로 재설정해야 한다.
            _initialRotation = _objectB.rotation;
            Vector3 perp = Vector3.Cross(_rotationAxis, Vector3.forward);
            if (perp.magnitude < ApplyTolerance)
                perp = Vector3.Cross(_rotationAxis, Vector3.right);
            _initialDirection = _objectB.rotation * perp;
        }

        // 절대적인 World X축이 아닌, _objectA가 회전한 만큼 _rotationAxis도 같이 회전시켜서 로컬 축으로 만듦
        Vector3 localAxisA = _objectA.rotation * _rotationAxis;
        Vector3 localAxisB = _objectB.rotation * _rotationAxis;

        // 오브젝트 A·B 의 Transform 에서 회전축 Line 생성
        Line lineA = new Line(_objectA.position,
                              _objectA.position + localAxisA);
        Line lineB = new Line(_objectB.position,
                              _objectB.position + localAxisB);

        _joint = new RevoluteJoint(lineA, lineB, _minAngle, _maxAngle);
    }

    /**
     * @brief  오브젝트의 Rigidbody 를 가져온다.
     *         Rigidbody 가 없으면 자동으로 생성한다.
     *         shouldBeKinematic 이 true 이고 Rigidbody 가 Kinematic 이 아니면
     *         자동으로 Kinematic 으로 설정하고 로그를 출력한다.
     * @param  target              대상 오브젝트의 Transform
     * @param  shouldBeKinematic   true 이면 Rigidbody 를 Kinematic 으로 설정한다.
     *                             (오브젝트 A 고정용)
     * @return Rigidbody           가져오거나 생성한 Rigidbody
     */
    private Rigidbody InitializeRigidbody(Transform target, bool shouldBeKinematic)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.gameObject.AddComponent<Rigidbody>();
        }

        if(shouldBeKinematic && !rb.isKinematic)
        {
            rb.isKinematic = true;
            Debug.Log("RevoluteJointComponent: Object A 가 자동으로 고정되었습니다.\n고정 대상을 변경하려면 Inspector 에서 Is Kinematic 을 수동으로 설정해주세요.");
        }

        return rb;
    }

    /**
    * @brief  회전 축을 감지하여 해당 축의 회전만 열어두고
    *         나머지 이동 및 회전을 프리즈한 RigidbodyConstraints 를 반환한다.
    * @return RigidbodyConstraints  회전축의 회전만 열어둔 프리즈 조건
    */
    private RigidbodyConstraints GetFreezeConstraintByDirection()
    {
        // 지정된 회전축 방향과 각 축의 유사도(내적) 계산
        // Dot 결과값의 절댓값이 클수록 해당 축과 방향이 일치함을 의미
        // ex) _rotationAxis = (1,0,0) 이면 dotX = 1.0, dotY = 0.0, dotZ = 0.0
        float dotX = Mathf.Abs(Vector3.Dot(_rotationAxis, Vector3.right));
        float dotY = Mathf.Abs(Vector3.Dot(_rotationAxis, Vector3.up));
        float dotZ = Mathf.Abs(Vector3.Dot(_rotationAxis, Vector3.forward));

        RigidbodyConstraints moveConstraint;

        // 세 축 중 Dot 값이 가장 큰 축이 실제 회전축
        // FreezeAll(모두 고정) 상태에서 해당 축의 회전(FreezeRotation) 제한만 해제
        if (dotX >= dotY && dotX >= dotZ)
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationX;
        else if (dotY >= dotX && dotY >= dotZ)
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationY;
        else
            moveConstraint = RigidbodyConstraints.FreezeAll & ~RigidbodyConstraints.FreezeRotationZ;

        return moveConstraint;
    }
}