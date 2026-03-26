// ============================================================
// 파일명  : SliderJoint.cs
// 역할    : 슬라이더 조인트 클래스
// 작성자  : 
// 작성일  : 
// 수정이력: 
// ============================================================

using UnityEngine;

public class SliderJoint : Joint
{
    private float _currentPosition;   // 현재 슬라이더 위치 (mm)
    private float _minPosition;       // 최소 이동 범위 (mm)
    private float _maxPosition;       // 최대 이동 범위 (mm)

    public float CurrentPosition => _currentPosition;   // 현재 슬라이더 위치
    public float MinPosition => _minPosition;           // 최소 이동 범위
    public float MaxPosition => _maxPosition;           // 최대 이동 범위

    /**
     * @brief  슬라이더 조인트를 생성한다.
     * @param  position      조인트 위치 (Vector3)
     * @param  axis          이동 축 방향 벡터 (Vector3)
     * @param  minPosition   최소 이동 범위 (mm)
     * @param  maxPosition   최대 이동 범위 (mm)
     */
    public SliderJoint(Vector3 position, Vector3 axis,
                       float minPosition, float maxPosition) : base(position, axis)
    {
        _minPosition = minPosition;
        _maxPosition = maxPosition;
        _currentPosition = 0f;
    }

    /**
     * @brief  슬라이더 위치를 설정한다.
     *         min/max 범위를 초과하면 클램프 처리한다.
     * @param  position    설정할 슬라이더 위치 (mm)
     */
    public void SetPosition(float position)
    {

    }

    /**
     * @brief  슬라이더 구속 조건을 적용한다.
     *         현재 위치를 기준으로 이동 축 방향의 이동 변환을 계산한다.
     */
    public override void ApplyConstraint()
    {

    }

    /**
     * @brief  슬라이더 조인트가 유효한 상태인지 검증한다.
     *         min/max 범위가 올바르고 축 벡터가 영벡터가 아니면 유효하다.
     * @return bool  유효하면 true, 아니면 false
     */
    public override bool IsValid()
    {

    }
}