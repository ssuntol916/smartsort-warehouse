// ============================================================
// 파일명  : ShuttleStatusView.cs
// 역할    : 셔틀 상태 표시 및 위치 이동 제어
//           매 프레임 Update()에서 셔틀이 목표 위치로 Lerp 이동하도록 처리한다.
//           _state 변경 시 배경색·텍스트를 업데이트한다.
// 작성자  : 이현화
// 작성일  : 2026-04-07
// 수정이력: 
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShuttleStatusView : MonoBehaviour
{
    [Header("셔틀 이동 설정")]
    [SerializeField] private RectTransform _shuttle;        // 셔틀 UI 오브젝트
    [SerializeField] private int _posX;                     // 목표 셀 X 좌표
    [SerializeField] private int _posY;                     // 목표 셀 Y 좌표
    [SerializeField] private float _cellSizeX = 85f;        // 셀 X 크기 (픽셀)
    [SerializeField] private float _cellSizeY = 85f;        // 셀 Y 크기 (픽셀)
    [SerializeField] private float _speed = 5f;             // Lerp 이동 속도

    [Header("상태 및 UI")]
    [SerializeField] private ShuttleState _state = ShuttleState.Idle;  // 현재 셔틀 상태
    [SerializeField] private Image _statusBG;               // 상태 배경 이미지
    [SerializeField] private TextMeshProUGUI _statusText;   // 상태 텍스트

    private ShuttleState _previousState;    // 이전 프레임 상태 (변경 감지용)
    private float _offsetX = 41f;            // X축 픽셀 오프셋
    private float _offsetY = 48f;            // Y축 픽셀 오프셋
    private float TargetX => _posX * _cellSizeX + _offsetX;    // 목표 X 위치 (셀 좌표 → 픽셀 위치 변환)
    private float TargetY => _posY * _cellSizeY + _offsetY;    // 목표 Y 위치 (셀 좌표 → 픽셀 위치 변환)

    public enum ShuttleState
    {
        Idle,     // 대기중
        Moving,   // 이동중
        Working   // 작업중
    }

    //TODO: MQTT warehouse/status/shuttle 토픽 구독 시 SetPosition(), SetState()로 교체
    /**
     * @brief  초기 위치 및 상태 UI를 설정한다.
     */
    private void Start()
    {
        _shuttle.anchoredPosition = new Vector2(TargetX, TargetY);
        UpdateStatusUI();
    }

    //TODO: MQTT warehouse/status/shuttle 토픽 수신 시 _posX, _posY, _state 값 업데이트 필요
    //      SetPosition(int x, int y), SetState(ShuttleState state) 메서드 추가 예정
    /**
     * @brief  매 프레임 셔틀을 목표 위치로 Lerp 이동한다.
     *         상태 변경 감지 시 UpdateStatusUI()를 호출한다.
     */
    void Update()
    {   
        // 현재 위치에서 목표 위치로 Lerp 이동
        Vector2 currentPos = _shuttle.anchoredPosition;
        float newX = Mathf.Lerp(currentPos.x, TargetX, Time.deltaTime * _speed);
        float newY = Mathf.Lerp(currentPos.y, TargetY, Time.deltaTime * _speed);
        _shuttle.anchoredPosition = new Vector2(newX, newY);

        // 상태가 변경된 경우에만 UI 업데이트 (매 프레임 호출 방지)
        if (_state != _previousState)
        {
            _previousState = _state;
            UpdateStatusUI();
        }
    }

    /**
     * @brief  셔틀 상태에 따라 UI 업데이트(배경색, 상태 텍스트)를 수행한다.
     */
    private void UpdateStatusUI()
    {
        switch (_state)
        {
            case ShuttleState.Idle:
                _statusBG.color = new Color(255 / 255f, 210 / 255f, 0 / 255f, 200 / 255f);  // 노란색
                _statusText.text = "IDLE";
                break;

            case ShuttleState.Moving:
                _statusBG.color = new Color(50 / 255f, 180 / 255f, 80 / 255f, 200 / 255f);  // 초록색
                _statusText.text = "MOVING";
                break;

            case ShuttleState.Working:
                _statusBG.color = new Color(255 / 255f, 140 / 255f, 0 / 255f, 200 / 255f);  // 주황색
                _statusText.text = "WORKING";
                break;
        }
    }
}