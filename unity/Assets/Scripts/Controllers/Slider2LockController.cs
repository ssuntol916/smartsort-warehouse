// ============================================================
// 파일명  : Slider2LockController.cs
// 역할    : 슬라이더2(vslider2)의 상태(잠금/해제)를 관리한다.
//           vslider2 는 Shuttle 의 자식이므로 셔틀 이동 시 자동으로 따라간다.
//           별도의 추종/고정 로직 없이 _isLocked 플래그만 관리한다.
//           (LockInitialPosition/UnlockInitialPosition 호출 불필요)
//
// 작성자  : 이현화
// 작성일  : 2026-04-27
// 수정이력: 2026-05-04 — OnDestroy 이벤트 해제, ValidateComponents 추가
//                        vslider2 가 Shuttle 자식임을 확인
//                        LateUpdate 추종 로직, LockInitialPosition 호출 전면 제거
// ============================================================

using UnityEngine;

public class Slider2LockController : MonoBehaviour
{
    // ============================================================
    // Inspector 필드
    // ============================================================

    [Header("필수 오브젝트")]
    [SerializeField] private SliderJointComponent _vsliderJoint;         // 슬라이더2 조인트
    [SerializeField] private SliderFloorDetector _sliderFloorDetector2;  // 슬라이더2 바닥 감지기
    [SerializeField] private ShuttleController _shuttleController;       // 셔틀 컨트롤러

    // ============================================================
    // 런타임 상태
    // ============================================================

    private bool _isLocked;   // vslider 잠금 여부

    // ============================================================
    // Unity 메시지
    // ============================================================

    private void Start()
    {
        if (!ValidateComponents()) return;

        _sliderFloorDetector2.OnFloorReached += OnSlider2FloorReached;
        _shuttleController.OnShuttleMovingEnd += OnShuttleMovingEnd;
    }

    /**
     * @brief  이벤트 구독을 해제한다.
     */
    private void OnDestroy()
    {
        if (_sliderFloorDetector2 != null)
            _sliderFloorDetector2.OnFloorReached -= OnSlider2FloorReached;

        if (_shuttleController != null)
            _shuttleController.OnShuttleMovingEnd -= OnShuttleMovingEnd;
    }

    /**
     * @brief  잠금 구간에서 셔틀이 올라가도 vslider2 의 월드 위치를 유지한다.
     *         vslider2 는 Shuttle 의 자식이므로 셔틀 이동 시 자동으로 따라가는데,
     *         잠금 중에는 SetPosition 으로 현재 위치를 고정하여 이를 상쇄한다.
     */
    private void LateUpdate()
    {
        // vslider2 는 Shuttle 의 자식이므로 셔틀 이동 시 자동 추종한다.
        // 잠금 구간(_isLocked=true)에서도 별도 처리 불필요.
    }

    // ============================================================
    // 이벤트 핸들러
    // ============================================================

    /**
     * @brief  슬라이더2 바닥 도달 시 vslider 위치를 잠근다.
     */
    private void OnSlider2FloorReached()
    {
        _isLocked = true;
        Debug.Log("[Slider2Lock] vslider 잠금");
    }

    /**
     * @brief  셔틀 이동 종료 시 vslider 잠금을 해제한다.
     */
    private void OnShuttleMovingEnd()
    {
        _isLocked = false;
        Debug.Log("[Slider2Lock] vslider 잠금 해제");
    }

    // ============================================================
    // 유효성 검사
    // ============================================================

    /**
     * @brief  Inspector 할당 필수 오브젝트의 유효성을 검사한다.
     * @return bool  모두 유효하면 true
     */
    private bool ValidateComponents()
    {
        bool isValid = true;

        if (_vsliderJoint == null)
        {
            Debug.LogError("[Slider2Lock] VsliderJoint 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_sliderFloorDetector2 == null)
        {
            Debug.LogError("[Slider2Lock] SliderFloorDetector2 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_shuttleController == null)
        {
            Debug.LogError("[Slider2Lock] ShuttleController 가 할당되지 않았습니다.");
            isValid = false;
        }

        if (!isValid) enabled = false;
        return isValid;
    }
}