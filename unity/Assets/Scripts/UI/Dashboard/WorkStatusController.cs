// ============================================================
// 파일명  : WorkStatusController.cs
// 역할    : 입고/출고 시스템 작업 현황 UI 컨트롤러
//           입고/출고 사이클 단계 표시 및 활성/비활성 상태 관리
// 작성자  : 이현화
// 작성일  : 2026-04-14
// 수정이력: 
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkStatusController : MonoBehaviour
{
    // ───────────────────────────── 색상 상수 ─────────────────────────────
    private static readonly Color32 ColorActive = new Color32(135, 217, 117, 255);   // 활성 (#87D975)
    private static readonly Color32 ColorCurrent = new Color32(230, 126, 34, 255);   // 현재 단계 (주황)
    private static readonly Color32 ColorDone = new Color32(135, 217, 117, 255);     // 완료 단계 (#87D975)
    private static readonly Color32 ColorInactive = new Color32(100, 100, 100, 255); // 비활성 (회색)
    private static readonly Color32 ColorWaiting = new Color32(44, 62, 80, 255);     // 대기 단계 (어두운)
    private static readonly Color32 ColorTextOn = new Color32(255, 255, 255, 255);   // 텍스트 활성
    private static readonly Color32 ColorTextOff = new Color32(150, 150, 150, 255);  // 텍스트 비활성

    // ───────────────────────────── SerializeField ─────────────────────────────

    [SerializeField] private TextMeshProUGUI _inboundTitleText;   // 입고 시스템 활성 표시

    [SerializeField] private TextMeshProUGUI _step_입고접수;       // 입고 단계 1
    [SerializeField] private TextMeshProUGUI _step_입고제품확인;   // 입고 단계 2
    [SerializeField] private TextMeshProUGUI _step_QR등록;         // 입고 단계 3
    [SerializeField] private TextMeshProUGUI _step_컨베이어투입;   // 입고 단계 4
    [SerializeField] private TextMeshProUGUI _step_제품확보;       // 입고 단계 5
    [SerializeField] private TextMeshProUGUI _step_적재완료;       // 입고 단계 6

    [SerializeField] private TextMeshProUGUI _outboundTitleText;  // 출고 시스템 활성 표시

    [SerializeField] private TextMeshProUGUI _step_출고접수;       // 출고 단계 1
    [SerializeField] private TextMeshProUGUI _step_Bin확보;        // 출고 단계 2
    [SerializeField] private TextMeshProUGUI _step_셔틀이동;       // 출고 단계 3
    [SerializeField] private TextMeshProUGUI _step_컨베이어배출;   // 출고 단계 4
    [SerializeField] private TextMeshProUGUI _step_출고제품확인;   // 출고 단계 5
    [SerializeField] private TextMeshProUGUI _step_출고완료;       // 출고 단계 6

    [SerializeField] private Image _box_입고접수;                  // 입고 단계 1 박스
    [SerializeField] private Image _box_입고제품확인;              // 입고 단계 2 박스
    [SerializeField] private Image _box_QR등록;                    // 입고 단계 3 박스
    [SerializeField] private Image _box_컨베이어투입;              // 입고 단계 4 박스
    [SerializeField] private Image _box_제품확보;                  // 입고 단계 5 박스
    [SerializeField] private Image _box_적재완료;                  // 입고 단계 6 박스

    [SerializeField] private Image _box_출고접수;                  // 출고 단계 1 박스
    [SerializeField] private Image _box_Bin확보;                   // 출고 단계 2 박스
    [SerializeField] private Image _box_셔틀이동;                  // 출고 단계 3 박스
    [SerializeField] private Image _box_컨베이어배출;              // 출고 단계 4 박스
    [SerializeField] private Image _box_출고제품확인;              // 출고 단계 5 박스
    [SerializeField] private Image _box_출고완료;                  // 출고 단계 6 박스

    // ───────────────────────────── Unity 생명주기 ─────────────────────────────

    /**
     * @brief 초기화 - 모든 단계 비활성 상태로 시작
     */
    void Awake()
    {
        // TODO: MQTT / Supabase Realtime 연동으로 상태 수신 후 호출
        SetInboundActive(false);
        SetInboundStep(0);
        SetOutboundActive(false);
        SetOutboundStep(0);
    }

    // ───────────────────────────── 공개 메서드 ─────────────────────────────

    /**
     * @brief 입고 시스템 활성/비활성 설정
     * @param active true=활성(초록), false=비활성(회색)
     */
    public void SetInboundActive(bool active)
    {
        _inboundTitleText.color = active ? ColorActive : ColorInactive;
    }

    /**
     * @brief 출고 시스템 활성/비활성 설정
     * @param active true=활성(초록), false=비활성(회색)
     */
    public void SetOutboundActive(bool active)
    {
        _outboundTitleText.color = active ? ColorActive : ColorInactive;
    }

    /**
     * @brief 입고 현재 단계 설정
     * @param step 0=입고접수, 1=제품확인, 2=QR등록, 3=컨베이어투입, 4=제품확보, 5=적재완료
     */
    public void SetInboundStep(int step)
    {
        TextMeshProUGUI[] steps = {
            _step_입고접수, _step_입고제품확인, _step_QR등록,
            _step_컨베이어투입, _step_제품확보, _step_적재완료
        };
        Image[] boxes = {
            _box_입고접수, _box_입고제품확인, _box_QR등록,
            _box_컨베이어투입, _box_제품확보, _box_적재완료
        };
        UpdateStepColors(steps, boxes, step);
    }

    /**
     * @brief 출고 현재 단계 설정
     * @param step 0=출고접수, 1=Bin확보, 2=셔틀이동, 3=컨베이어배출, 4=제품확인, 5=출고완료
     */
    public void SetOutboundStep(int step)
    {
        TextMeshProUGUI[] steps = {
            _step_출고접수, _step_Bin확보, _step_셔틀이동,
            _step_컨베이어배출, _step_출고제품확인, _step_출고완료
        };
        Image[] boxes = {
            _box_출고접수, _box_Bin확보, _box_셔틀이동,
            _box_컨베이어배출, _box_출고제품확인, _box_출고완료
        };
        UpdateStepColors(steps, boxes, step);
    }

    // ───────────────────────────── 내부 메서드 ─────────────────────────────

    /**
     * @brief 단계 색상 업데이트 공용 메서드
     * @param steps       단계 텍스트 배열
     * @param boxes       단계 박스 Image 배열
     * @param currentStep 현재 진행 단계 인덱스
     */
    private void UpdateStepColors(TextMeshProUGUI[] steps, Image[] boxes, int currentStep)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (i < currentStep)
            {
                steps[i].color = ColorTextOn;
                boxes[i].color = ColorDone;
            }
            else if (i == currentStep)
            {
                steps[i].color = ColorTextOn;
                boxes[i].color = ColorCurrent;
            }
            else
            {
                steps[i].color = ColorTextOff;
                boxes[i].color = ColorWaiting;
            }
        }
    }
}