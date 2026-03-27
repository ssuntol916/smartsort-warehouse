// ============================================================
// 파일명  : main.cpp
// 역할    : 4-way 셔틀 ESP32 MQTT 제어 펌웨어
// 작성자  : 송준호
// 작성일  : 2026-03-27
// 수정이력 : 
// ============================================================

#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <ESP32Servo.h>
#include "secrets.h"

// ============================================================
// MQTT 설정
// ============================================================
const int MQTT_PORT = 1883;
const char* MQTT_CLIENT_ID = "smartsort-shuttle-esp32";

// 구독 토픽
const char* TOPIC_CMD_SHUTTLE = "warehouse/cmd/shuttle";

// 발행 토픽
const char* TOPIC_STATUS_SHUTTLE = "warehouse/status/shuttle";
const char* TOPIC_ALERT = "warehouse/alert";

// ============================================================
// 그리드 설정 (3×3×3)
// ============================================================
const int GRID_MAX_X = 3;
const int GRID_MAX_Y = 3;
const int GRID_MAX_Z = 3;

// ============================================================
// 서보모터 핀 매핑
// ============================================================
// 방향 전환: 래크 앤 피니언 (MG996R × 2) — Y축 바퀴 양측 동시 승강
const int PIN_DIR_LEFT     = 13;
const int PIN_DIR_RIGHT    = 15;

// X축 주행: MG90S × 2 (바퀴 직결)
const int PIN_X_LEFT       = 14;
const int PIN_X_RIGHT      = 27;

// Y축 주행: MG90S × 2 (바퀴 직결)
const int PIN_Y_LEFT       = 26;
const int PIN_Y_RIGHT      = 25;

// Z축 리프트: MG996R × 2 (스풀/벨트 양측 동시 리프팅)
const int PIN_Z_LEFT       = 32;
const int PIN_Z_RIGHT      = 33;

// ============================================================
// 엔드스톱 스위치 핀 — 홈 포지션 (0, 0, 0) 캘리브레이션
// ============================================================
const int PIN_ENDSTOP_X    = 34;  // X축 홈 (INPUT_ONLY)
const int PIN_ENDSTOP_Y    = 35;  // Y축 홈 (INPUT_ONLY)
const int PIN_ENDSTOP_Z    = 36;  // Z축 홈 (INPUT_ONLY)

// ============================================================
// 서보 각도 설정
// ============================================================
// 방향 전환 (래크 앤 피니언)
const int DIR_ANGLE_X_MODE = 0;    // Y축 바퀴 상승 → X축 접지
const int DIR_ANGLE_Y_MODE = 180;  // Y축 바퀴 하강 → Y축 접지

// MG90S 연속 회전 서보 — 정지/정회전/역회전 각도
const int MG90S_STOP       = 90;
const int MG90S_CW         = 0;    // 정방향
const int MG90S_CCW        = 180;  // 역방향

// MG996R Z축 — 스풀/벨트 감아올리기
const int Z_LIFT_STOP      = 90;
const int Z_LIFT_UP        = 0;    // 감아올리기 (상승)
const int Z_LIFT_DOWN      = 180;  // 풀어내리기 (하강)

// 셀 간 이동 시간 (ms) — 실측 후 조정 필요
const unsigned long MOVE_TIME_PER_CELL_XY = 1000;
const unsigned long MOVE_TIME_PER_CELL_Z  = 1500;
const unsigned long DIR_SWITCH_DELAY      = 500;   // 방향 전환 안정화 대기

// ============================================================
// 셔틀 상태 열거형
// ============================================================
enum ShuttleState {
    STATE_IDLE,
    STATE_HOMING,
    STATE_DIR_SWITCH,
    STATE_MOVING_X,
    STATE_MOVING_Y,
    STATE_LIFTING_Z,
    STATE_ERROR
};

const char* stateToString(ShuttleState state) {
    switch (state) {
        case STATE_IDLE:       return "idle";
        case STATE_HOMING:     return "homing";
        case STATE_DIR_SWITCH: return "dir_switch";
        case STATE_MOVING_X:   return "moving_x";
        case STATE_MOVING_Y:   return "moving_y";
        case STATE_LIFTING_Z:  return "lifting_z";
        case STATE_ERROR:      return "error";
        default:               return "unknown";
    }
}

// ============================================================
// 전역 객체
// ============================================================
WiFiClient _wifiClient;
PubSubClient _mqttClient(_wifiClient);

// 서보 객체 — 총 8개
Servo _servoDirL;        // 방향 전환 좌측 (래크 앤 피니언)
Servo _servoDirR;        // 방향 전환 우측 (래크 앤 피니언)
Servo _servoXL;          // X축 좌측
Servo _servoXR;          // X축 우측
Servo _servoYL;          // Y축 좌측
Servo _servoYR;          // Y축 우측
Servo _servoZL;          // Z축 좌측 (스풀/벨트)
Servo _servoZR;          // Z축 우측 (스풀/벨트)

// 현재 셔틀 상태
int _currentX = 0;
int _currentY = 0;
int _currentZ = 0;
String _currentBinId = "";
ShuttleState _currentState = STATE_IDLE;
bool _isHomed = false;

// MQTT 재연결 간격
unsigned long _lastReconnectAttempt = 0;
const unsigned long RECONNECT_INTERVAL = 5000;

// ============================================================
// 함수 선언
// ============================================================
void setupWiFi();
void setupServos();
void setupEndstops();
bool reconnectMQTT();
void onMqttMessage(char* topic, byte* payload, unsigned int length);
void handleMoveCommand(JsonDocument& doc);
void handleHomeCommand();
void executeTransferSequence(int targetX, int targetY, int targetZ, const char* binId);
void switchToXMode();
void switchToYMode();
void moveX(int cells);
void moveY(int cells);
void moveZ(int cells);
void stopAllDrive();
bool homeAxis(int endstopPin, Servo& servoL, Servo& servoR, int driveAngle, const char* axisName);
void publishStatus();
void publishAlert(const char* level, const char* msg);
int clampGrid(int value, int maxVal);

// ============================================================
// setup / loop
// ============================================================

void setup() {
    Serial.begin(115200);
    Serial.println("\n[셔틀 ESP32] 시작");

    setupEndstops();
    setupWiFi();
    setupServos();

    _mqttClient.setServer(MQTT_BROKER, MQTT_PORT);
    _mqttClient.setCallback(onMqttMessage);

    // JSON 페이로드 최대 크기 설정 (기본 256바이트 → 512바이트)
    _mqttClient.setBufferSize(512);
}

void loop() {
    // Wi-Fi 연결 유지
    if (WiFi.status() != WL_CONNECTED) {
        setupWiFi();
    }

    // MQTT 연결 유지 (논블로킹 재연결)
    if (!_mqttClient.connected()) {
        unsigned long now = millis();
        if (now - _lastReconnectAttempt > RECONNECT_INTERVAL) {
            _lastReconnectAttempt = now;
            if (reconnectMQTT()) {
                _lastReconnectAttempt = 0;
            }
        }
    } else {
        _mqttClient.loop();
    }
}

// ============================================================
// Wi-Fi 연결
// ============================================================

/**
 * @brief Wi-Fi 네트워크에 연결한다.
 */
void setupWiFi() {
    Serial.printf("[WiFi] %s 에 연결 중...\n", WIFI_SSID);
    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

    int retryCount = 0;
    while (WiFi.status() != WL_CONNECTED && retryCount < 20) {
        delay(500);
        Serial.print(".");
        retryCount++;
    }

    if (WiFi.status() == WL_CONNECTED) {
        Serial.printf("\n[WiFi] 연결 완료 — IP: %s\n", WiFi.localIP().toString().c_str());
    } else {
        Serial.println("\n[WiFi] 연결 실패 — 재시도 예정");
    }
}

// ============================================================
// 엔드스톱 스위치 초기화
// ============================================================

/**
 * @brief 엔드스톱 스위치 핀을 입력 풀업으로 초기화한다.
 */
void setupEndstops() {
    pinMode(PIN_ENDSTOP_X, INPUT_PULLUP);
    pinMode(PIN_ENDSTOP_Y, INPUT_PULLUP);
    pinMode(PIN_ENDSTOP_Z, INPUT_PULLUP);
    Serial.println("[엔드스톱] 초기화 완료");
}

// ============================================================
// 서보모터 초기화
// ============================================================

/**
 * @brief 서보모터 8개를 초기화하고 핀에 연결한다.
 *        MG90S (주행): 500~2400μs, MG996R (방향전환/리프트): 500~2400μs
 */
void setupServos() {
    ESP32PWM::allocateTimer(0);
    ESP32PWM::allocateTimer(1);
    ESP32PWM::allocateTimer(2);
    ESP32PWM::allocateTimer(3);

    // 방향 전환 (MG996R × 2) — 래크 앤 피니언 Y축 바퀴 양측 동시 승강
    _servoDirL.setPeriodHertz(50);
    _servoDirR.setPeriodHertz(50);
    _servoDirL.attach(PIN_DIR_LEFT, 500, 2400);
    _servoDirR.attach(PIN_DIR_RIGHT, 500, 2400);
    _servoDirL.write(DIR_ANGLE_X_MODE);  // 초기: X축 접지
    _servoDirR.write(DIR_ANGLE_X_MODE);

    // X축 주행 (MG90S × 2)
    _servoXL.setPeriodHertz(50);
    _servoXR.setPeriodHertz(50);
    _servoXL.attach(PIN_X_LEFT, 500, 2400);
    _servoXR.attach(PIN_X_RIGHT, 500, 2400);
    _servoXL.write(MG90S_STOP);
    _servoXR.write(MG90S_STOP);

    // Y축 주행 (MG90S × 2)
    _servoYL.setPeriodHertz(50);
    _servoYR.setPeriodHertz(50);
    _servoYL.attach(PIN_Y_LEFT, 500, 2400);
    _servoYR.attach(PIN_Y_RIGHT, 500, 2400);
    _servoYL.write(MG90S_STOP);
    _servoYR.write(MG90S_STOP);

    // Z축 리프트 (MG996R × 2) — 스풀/벨트 양측 동시
    _servoZL.setPeriodHertz(50);
    _servoZR.setPeriodHertz(50);
    _servoZL.attach(PIN_Z_LEFT, 500, 2400);
    _servoZR.attach(PIN_Z_RIGHT, 500, 2400);
    _servoZL.write(Z_LIFT_STOP);
    _servoZR.write(Z_LIFT_STOP);

    Serial.println("[서보] 8개 초기화 완료 (방향전환 ×2, X ×2, Y ×2, Z ×2)");
}

// ============================================================
// MQTT 연결 및 콜백
// ============================================================

/**
 * @brief MQTT 브로커에 연결하고 토픽을 구독한다.
 * @return 연결 성공 여부
 */
bool reconnectMQTT() {
    Serial.println("[MQTT] 브로커 연결 시도...");

    if (_mqttClient.connect(MQTT_CLIENT_ID)) {
        Serial.println("[MQTT] 연결 성공");

        // warehouse/cmd/shuttle 토픽 구독
        _mqttClient.subscribe(TOPIC_CMD_SHUTTLE);
        Serial.printf("[MQTT] 구독: %s\n", TOPIC_CMD_SHUTTLE);

        // 연결 완료 상태 발행
        publishStatus();
        return true;
    }

    Serial.printf("[MQTT] 연결 실패 — rc=%d\n", _mqttClient.state());
    return false;
}

/**
 * @brief MQTT 메시지 수신 콜백. 페이로드를 파싱하여 명령을 분기한다.
 *        설계 문서 페이로드: {"target_x":2,"target_y":1,"target_z":3,"bin_id":"B-021"}
 * @param topic 수신된 토픽
 * @param payload 메시지 페이로드 (바이트 배열)
 * @param length 페이로드 길이
 */
void onMqttMessage(char* topic, byte* payload, unsigned int length) {
    Serial.printf("[MQTT] 수신 — 토픽: %s\n", topic);

    // JSON 파싱
    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, payload, length);

    if (error) {
        Serial.printf("[MQTT] JSON 파싱 실패: %s\n", error.c_str());
        publishAlert("error", "JSON 파싱 실패");
        return;
    }

    // home 명령 처리
    if (doc.containsKey("action")) {
        const char* action = doc["action"];
        if (strcmp(action, "home") == 0) {
            handleHomeCommand();
            return;
        }
    }

    // 이동 명령 처리
    handleMoveCommand(doc);
}

// ============================================================
// 명령 처리
// ============================================================

/**
 * @brief 셔틀 그리드 이동 명령을 처리한다.
 *        설계 문서 페이로드: {"target_x":2,"target_y":1,"target_z":3,"bin_id":"B-021"}
 * @param doc JSON 페이로드
 */
void handleMoveCommand(JsonDocument& doc) {
    int targetX = doc["target_x"] | -1;
    int targetY = doc["target_y"] | -1;
    int targetZ = doc["target_z"] | -1;
    const char* binId = doc["bin_id"] | "";

    // 필수 필드 검증
    if (targetX < 0 || targetY < 0 || targetZ < 0) {
        publishAlert("error", "필수 필드 누락 (target_x, target_y, target_z)");
        return;
    }

    // 그리드 범위 검증
    if (targetX >= GRID_MAX_X || targetY >= GRID_MAX_Y || targetZ >= GRID_MAX_Z) {
        Serial.printf("[셔틀] 그리드 범위 초과 — 목표: (%d,%d,%d), 최대: (%d,%d,%d)\n",
                      targetX, targetY, targetZ,
                      GRID_MAX_X - 1, GRID_MAX_Y - 1, GRID_MAX_Z - 1);
        publishAlert("error", "그리드 범위 초과");
        return;
    }

    // 홈 캘리브레이션 미완료 시 경고
    if (!_isHomed) {
        Serial.println("[셔틀] 경고: 홈 캘리브레이션 미완료 상태에서 이동 시도");
        publishAlert("warn", "홈 캘리브레이션 미완료");
    }

    _currentBinId = binId;

    Serial.printf("[셔틀] 이동 명령 — 목표: (%d,%d,%d) Bin: %s\n",
                  targetX, targetY, targetZ, binId);

    executeTransferSequence(targetX, targetY, targetZ, binId);
}

/**
 * @brief 엔드스톱 기반 홈 포지션 (0,0,0) 캘리브레이션을 수행한다.
 */
void handleHomeCommand() {
    Serial.println("[셔틀] 홈 캘리브레이션 시작");
    _currentState = STATE_HOMING;
    publishStatus();

    // Z축 먼저 홈 (안전을 위해 최하단으로)
    switchToXMode();  // 방향 전환 초기화
    delay(DIR_SWITCH_DELAY);

    bool zOk = homeAxis(PIN_ENDSTOP_Z, _servoZL, _servoZR, Z_LIFT_DOWN, "Z");
    bool xOk = homeAxis(PIN_ENDSTOP_X, _servoXL, _servoXR, MG90S_CCW, "X");

    switchToYMode();
    delay(DIR_SWITCH_DELAY);

    bool yOk = homeAxis(PIN_ENDSTOP_Y, _servoYL, _servoYR, MG90S_CCW, "Y");

    switchToXMode();  // 초기 상태 복원 (X축 접지)
    delay(DIR_SWITCH_DELAY);

    if (xOk && yOk && zOk) {
        _currentX = 0;
        _currentY = 0;
        _currentZ = 0;
        _isHomed = true;
        _currentState = STATE_IDLE;
        Serial.println("[셔틀] 홈 캘리브레이션 완료 — (0,0,0)");
    } else {
        _currentState = STATE_ERROR;
        _isHomed = false;
        Serial.println("[셔틀] 홈 캘리브레이션 실패");
        publishAlert("error", "홈 캘리브레이션 실패");
    }

    publishStatus();
}

// ============================================================
// 이송 시퀀스 — 설계 문서 정밀 정지 시퀀스 5~7단계
// ============================================================

/**
 * @brief 셔틀 이송 시퀀스를 실행한다.
 *        5단계: Y축 바퀴 상승(X축 접지) → X축 목적 열까지 정밀 이동
 *        6단계: Y축 바퀴 하강(Y축 접지) → Y축 목적 행까지 정밀 이동
 *        7단계: Z축 리프트 → 목적 층까지 수직 이동
 * @param targetX 목적 열 (0 ~ GRID_MAX_X-1)
 * @param targetY 목적 행 (0 ~ GRID_MAX_Y-1)
 * @param targetZ 목적 층 (0 ~ GRID_MAX_Z-1)
 * @param binId   이송 대상 Bin ID
 */
void executeTransferSequence(int targetX, int targetY, int targetZ, const char* binId) {
    int deltaX = targetX - _currentX;
    int deltaY = targetY - _currentY;
    int deltaZ = targetZ - _currentZ;

    // --- 5단계: X축 이동 ---
    if (deltaX != 0) {
        _currentState = STATE_DIR_SWITCH;
        publishStatus();
        switchToXMode();
        delay(DIR_SWITCH_DELAY);

        _currentState = STATE_MOVING_X;
        publishStatus();
        Serial.printf("[셔틀] X축 이동: %d → %d (%+d칸)\n", _currentX, targetX, deltaX);
        moveX(deltaX);
        _currentX = targetX;
    }

    // --- 6단계: Y축 이동 ---
    if (deltaY != 0) {
        _currentState = STATE_DIR_SWITCH;
        publishStatus();
        switchToYMode();
        delay(DIR_SWITCH_DELAY);

        _currentState = STATE_MOVING_Y;
        publishStatus();
        Serial.printf("[셔틀] Y축 이동: %d → %d (%+d칸)\n", _currentY, targetY, deltaY);
        moveY(deltaY);
        _currentY = targetY;
    }

    // --- 7단계: Z축 리프트 ---
    if (deltaZ != 0) {
        _currentState = STATE_LIFTING_Z;
        publishStatus();
        Serial.printf("[셔틀] Z축 리프트: %d → %d (%+d층)\n", _currentZ, targetZ, deltaZ);
        moveZ(deltaZ);
        _currentZ = targetZ;
    }

    // 이송 완료 — X축 접지 상태로 복원
    switchToXMode();
    delay(DIR_SWITCH_DELAY);

    _currentState = STATE_IDLE;
    Serial.printf("[셔틀] 이송 완료 — 위치: (%d,%d,%d) Bin: %s\n",
                  _currentX, _currentY, _currentZ, binId);

    publishStatus();
}

// ============================================================
// 방향 전환 — 래크 앤 피니언 슬라이더
// ============================================================

/**
 * @brief Y축 바퀴를 상승시켜 X축 바퀴만 접지한다. (X축 이동 모드)
 *        좌/우 MG996R 2개를 동시에 구동하여 Y축 바퀴를 균형 있게 상승시킨다.
 */
void switchToXMode() {
    _servoDirL.write(DIR_ANGLE_X_MODE);
    _servoDirR.write(DIR_ANGLE_X_MODE);
    Serial.println("[방향전환] X축 모드 (Y축 바퀴 양측 상승)");
}

/**
 * @brief Y축 바퀴를 하강시켜 Y축 바퀴를 접지한다. (Y축 이동 모드)
 *        좌/우 MG996R 2개를 동시에 구동하여 Y축 바퀴를 균형 있게 하강시킨다.
 */
void switchToYMode() {
    _servoDirL.write(DIR_ANGLE_Y_MODE);
    _servoDirR.write(DIR_ANGLE_Y_MODE);
    Serial.println("[방향전환] Y축 모드 (Y축 바퀴 양측 하강)");
}

// ============================================================
// 축별 이동 제어
// ============================================================

/**
 * @brief X축으로 지정 셀 수만큼 이동한다.
 *        양수: 정방향(+X), 음수: 역방향(-X)
 * @param cells 이동할 셀 수 (부호 포함)
 */
void moveX(int cells) {
    int driveAngle = (cells > 0) ? MG90S_CW : MG90S_CCW;
    unsigned long duration = abs(cells) * MOVE_TIME_PER_CELL_XY;

    _servoXL.write(driveAngle);
    _servoXR.write(driveAngle);
    delay(duration);
    _servoXL.write(MG90S_STOP);
    _servoXR.write(MG90S_STOP);
}

/**
 * @brief Y축으로 지정 셀 수만큼 이동한다.
 *        양수: 정방향(+Y), 음수: 역방향(-Y)
 * @param cells 이동할 셀 수 (부호 포함)
 */
void moveY(int cells) {
    int driveAngle = (cells > 0) ? MG90S_CW : MG90S_CCW;
    unsigned long duration = abs(cells) * MOVE_TIME_PER_CELL_XY;

    _servoYL.write(driveAngle);
    _servoYR.write(driveAngle);
    delay(duration);
    _servoYL.write(MG90S_STOP);
    _servoYR.write(MG90S_STOP);
}

/**
 * @brief Z축으로 지정 층 수만큼 리프트한다. (스풀/벨트 양측 동시)
 *        양수: 상승(+Z), 음수: 하강(-Z)
 * @param cells 이동할 층 수 (부호 포함)
 */
void moveZ(int cells) {
    int driveAngle = (cells > 0) ? Z_LIFT_UP : Z_LIFT_DOWN;
    unsigned long duration = abs(cells) * MOVE_TIME_PER_CELL_Z;

    _servoZL.write(driveAngle);
    _servoZR.write(driveAngle);
    delay(duration);
    _servoZL.write(Z_LIFT_STOP);
    _servoZR.write(Z_LIFT_STOP);
}

/**
 * @brief 모든 주행 서보를 정지한다. (비상 정지용)
 */
void stopAllDrive() {
    _servoXL.write(MG90S_STOP);
    _servoXR.write(MG90S_STOP);
    _servoYL.write(MG90S_STOP);
    _servoYR.write(MG90S_STOP);
    _servoZL.write(Z_LIFT_STOP);
    _servoZR.write(Z_LIFT_STOP);
}

// ============================================================
// 엔드스톱 기반 홈 복귀
// ============================================================

/**
 * @brief 엔드스톱 스위치가 눌릴 때까지 지정 축을 역방향으로 구동하여 홈 위치로 복귀한다.
 * @param endstopPin 엔드스톱 GPIO 핀
 * @param servoL 좌측 서보
 * @param servoR 우측 서보
 * @param driveAngle 역방향 구동 각도
 * @param axisName 축 이름 (로그용)
 * @return 홈 도달 성공 여부
 */
bool homeAxis(int endstopPin, Servo& servoL, Servo& servoR, int driveAngle, const char* axisName) {
    Serial.printf("[홈] %s축 홈 복귀 시작\n", axisName);

    unsigned long timeout = 15000;  // 최대 15초
    unsigned long startTime = millis();

    servoL.write(driveAngle);
    servoR.write(driveAngle);

    while (digitalRead(endstopPin) == HIGH) {
        if (millis() - startTime > timeout) {
            servoL.write(MG90S_STOP);
            servoR.write(MG90S_STOP);
            Serial.printf("[홈] %s축 타임아웃 — 엔드스톱 미감지\n", axisName);
            return false;
        }
        delay(10);
    }

    servoL.write(MG90S_STOP);
    servoR.write(MG90S_STOP);
    Serial.printf("[홈] %s축 홈 도달\n", axisName);
    return true;
}

// ============================================================
// 상태 발행 — 설계 문서 warehouse/status/shuttle 페이로드
// ============================================================

/**
 * @brief 현재 셔틀 상태를 MQTT로 발행한다.
 *        설계 문서: {"x":2,"y":1,"z":3,"bin_id":"B-021","state":"idle"}
 */
void publishStatus() {
    JsonDocument doc;
    doc["x"] = _currentX;
    doc["y"] = _currentY;
    doc["z"] = _currentZ;
    doc["bin_id"] = _currentBinId;
    doc["state"] = stateToString(_currentState);
    doc["homed"] = _isHomed;

    char buffer[256];
    serializeJson(doc, buffer);

    _mqttClient.publish(TOPIC_STATUS_SHUTTLE, buffer);
    Serial.printf("[MQTT] 상태 발행: %s\n", buffer);
}

/**
 * @brief 알림 메시지를 MQTT로 발행한다.
 * @param level 알림 레벨 ("error", "warn", "info")
 * @param msg 알림 메시지
 */
void publishAlert(const char* level, const char* msg) {
    JsonDocument doc;
    doc["type"] = "shuttle_alert";
    doc["level"] = level;
    doc["source"] = "shuttle";
    doc["msg"] = msg;
    doc["x"] = _currentX;
    doc["y"] = _currentY;
    doc["z"] = _currentZ;

    char buffer[256];
    serializeJson(doc, buffer);

    _mqttClient.publish(TOPIC_ALERT, buffer);
    Serial.printf("[MQTT] 알림 발행: %s\n", buffer);
}

// ============================================================
// 유틸리티
// ============================================================

/**
 * @brief 그리드 좌표를 유효 범위로 제한한다.
 * @param value 입력 좌표
 * @param maxVal 최대값 (exclusive)
 * @return 제한된 좌표 (0 ~ maxVal-1)
 */
int clampGrid(int value, int maxVal) {
    if (value < 0) return 0;
    if (value >= maxVal) return maxVal - 1;
    return value;
}