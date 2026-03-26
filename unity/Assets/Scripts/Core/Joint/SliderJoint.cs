// ============================================================
// 파일명  : SliderJoint.cs
// 역할    : 슬라이더 조인트 클래스
// 작성자  : 이현화
// 작성일  : 2026-03-
// 수정이력: 
// ============================================================

using System.Threading.Tasks;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public class SliderJoint : Joint
{
    private Line _lineA;     // 오브젝트 A 의 이동축 선
    private Line _lineB;     // 오브젝트 B 의 이동축 선
    private Plane _planeA;   // 오브젝트 A 의 기준 면 (회전 방지)
    private Plane _planeB;   // 오브젝트 B 의 기준 면 (회전 방지)

    private float _currentPosition;   // 현재 슬라이더 위치 (mm)
    private float _minPosition;       // 최소 이동 범위 (mm)
    private float _maxPosition;       // 최대 이동 범위 (mm)

    private bool _isLineConstrained;    // lineA ↔ lineB 구속 등록됐는지
    private bool _isPlaneConstrained;   // planeA ↔ planeB 구속 등록됐는지

    public float CurrentPosition => _currentPosition;   // 현재 슬라이더 위치
    public float MinPosition => _minPosition;           // 최소 이동 범위
    public float MaxPosition => _maxPosition;           // 최대 이동 범위

    /**
     * @brief  두 선(이동축)과 두 면(기준면)으로 슬라이더 조인트를 생성한다.
     *         lineA 와 lineB 가 일치(Coincident)하고,
     *         planeA 와 planeB 가 평행(Parallel)할 때 유효한 조인트가 된다.
     * @param  lineA         오브젝트 A 의 이동축 Line
     * @param  lineB         오브젝트 B 의 이동축 Line
     * @param  planeA        오브젝트 A 의 기준 Plane (회전 방지)
     * @param  planeB        오브젝트 B 의 기준 Plane (회전 방지)
     * @param  minPosition   최소 이동 범위 (mm)
     * @param  maxPosition   최대 이동 범위 (mm)
     */
    public SliderJoint(Line lineA, Line lineB,
                       Plane planeA, Plane planeB,
                       float minPosition, float maxPosition) : base(lineA, lineB)
    {
        _lineA = lineA;
        _lineB = lineB;
        _planeA = planeA;
        _planeB = planeB;
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
        _currentPosition = Mathf.Clamp(position, _minPosition, _maxPosition);
    }

    /**
     * @brief  슬라이더 구속 조건을 적용한다.
     *         아래 두 조건을 동시에 만족하도록 오브젝트 B 를 보정한다.
     *         ① lineA 와 lineB 일치 또는 평행 → Line.IsCoincident() 로 검증
     *            Line.Project() 를 활용하여 축 방향 이동 보정
     *         ② planeA 와 planeB 평행 → Plane.IsParallel() 로 검증
     *            Plane.Project() 를 활용하여 회전 방향 보정
     */

    /**
    * @brief  슬라이더 구속 조건을 등록한다.
    *         아래 두 조건을 확인하여 구속 상태를 저장한다.
    *         ① lineA 와 lineB 가 평행 또는 일치 → _isLineConstrained 에 저장
    *         ② planeA 와 planeB 가 평행 → _isPlaneConstrained 에 저장
    */
    public override void ApplyConstraint()
    {
        _isLineConstrained = _lineA.IsCoincident(_lineB) || _lineA.IsParallel(_lineB);
        _isPlaneConstrained = _planeA.IsParallel(_planeB);
    }

    /**
     * @brief  슬라이더 조인트가 유효한 상태인지 검증한다.
     *         아래 세 조건을 모두 만족하면 유효하다.
     *         ① lineA 와 lineB 가 일치 또는 평행한지 확인 → Line.IsParallel()
     *         ② planeA 와 planeB 가 평행한지 확인 → Plane.IsParallel()
     *         ③ min/max 범위가 올바른지 확인 (minPosition < maxPosition)
     * @return bool  유효하면 true, 아니면 false
     */
    public override bool IsValid()
    {
        bool isValid = true;

        if (!_lineA.IsCoincident(_lineB) && !_lineA.IsParallel(_lineB))
        {
            isValid = false;
            Debug.LogWarning("슬라이더 조인트가 유효하지 않은 상태입니다. (Line 불일치)");
        }

        if (!_planeA.IsParallel(_planeB))
        {
            isValid = false;
            Debug.LogWarning("슬라이더 조인트가 유효하지 않은 상태입니다. (Plane 불일치)");
        }

        if (_minPosition > _maxPosition)
        {
            isValid = false;
            Debug.LogWarning("슬라이더 조인트가 유효하지 않은 상태입니다. (min/max 범위 불일치)");
        }

        return isValid;
    }
}