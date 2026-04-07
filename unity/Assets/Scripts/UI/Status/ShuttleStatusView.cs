using UnityEngine;
using UnityEngine.UI;

public class ShuttleStatusView : MonoBehaviour
{
    [SerializeField] private RectTransform _shuttle;
    [SerializeField] private int _posX;
    [SerializeField] private int _posY;
    [SerializeField] private float _cellSizeX = 87f;
    [SerializeField] private float _cellSizeY = 87f;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _offsetX = 6f;
    [SerializeField] private float _offsetY = 0f;

    [SerializeField] private ShuttleState _state = ShuttleState.Idle;

    // UI 변수
    [SerializeField] private Image _statusBG;
    //[SerializeField] private TextMeshProUGUI _statusText;

    public enum ShuttleState
    {
        Idle,     // 대기중
        Moving,   // 이동중
        Working   // 작업중
    }

    void Update()
    {
        // 목표 위치 계산
        float targetX = _posX * _cellSizeX + _offsetX;
        float targetY = -_posY * _cellSizeY + _offsetY;

        // 현재 위치에서 목표 위치로 Lerp 이동
        Vector2 currentPos = _shuttle.anchoredPosition;
        float newX = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * _speed);
        float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * _speed);
        _shuttle.anchoredPosition = new Vector2(newX, newY);

        //UpdateStatusUI();
    }

//    private void UpdateStatusUI()
//    {
//        switch (_state)
//        {
//            case ShuttleState.Idle:
//                _statusBG.color = new Color(255 / 255f, 210 / 255f, 0 / 255f, 200 / 255f);  // 노란색
//                _statusText.color = Color.black;
//                _statusText.text = "IDLE";
//                break;

//            case ShuttleState.Moving:
//                _statusBG.color = new Color(50 / 255f, 180 / 255f, 80 / 255f, 200 / 255f);  // 초록색
//                _statusText.color = Color.black;
//                _statusText.text = "MOVING";
//                break;

//            case ShuttleState.Working:
//                _statusBG.color = new Color(255 / 255f, 140 / 255f, 0 / 255f, 200 / 255f);  // 주황색
//                _statusText.color = Color.black;
//                _statusText.text = "WORKING";
//                break;
//        }
//    }
}