// ============================================================
// 파일명  : main.cpp
// 역할    : 4-way 셔틀 ESP32 MQTT 제어 펌웨어
// 작성자  : 송준호
// 작성일  : 2026-03-27
// 수정이력 : 2026-04-01 — 서보모터 점진적 이동(smooth move) 적용
//           2026-04-10 — X, Y축 µs 캘리브레이션 보정 적용
//           2026-04-15 — mDNS + RemoteSerial(Telnet) 원격 로깅 적용
// ============================================================

#include <Arduino.h>
#include <WiFi.h>
#include <ESPmDNS.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <ESP32Servo.h>
#include "secrets.h"
#include "RemoteSerial.h"

// MG90S 개체별 정지점 (µs) — 캘리브레이션 후 측정값으로 교체
const int STOP_US_X_LEFT  = 1500;
const int STOP_US_X_RIGHT = 1500;
const int STOP_US_Y_LEFT  = 1500;
const int STOP_US_Y_RIGHT = 1500;

// X축 캘리브레이션 펄스 (µs) — 실측값
const int X_FWD_US_L = 1800;   // 전진 좌측
const int X_FWD_US_R = 1200;   // 전진 우측
const int X_REV_US_L = 1200;   // 후진 좌측
const int X_REV_US_R = 1800;   // 후진 우측

// Y축 캘리브레이션 펄스 (µs) — 실측값
const int Y_FWD_US_L = 1800;   // 전진 좌측
const int Y_FWD_US_R = 1200;   // 전진 우측
const int Y_REV_US_L = 1200;   // 후진 좌측
const int Y_REV_US_R = 1800;   // 후진 우측

// µs 기반 램프 설정
const int RAMP_US_STEP     = 5;   // 1스텝당 µs 변화량
const int RAMP_US_DELAY_MS = 10;   // 스텝 간 대기시간

// ============================================================
// MQTT 설정
// ============================================================
const int MQTT_PORT = 1883;
const char* MQTT_CLIENT_ID = "smartsort-shuttle-esp32";
const char* MDNS_HOSTNAME  = "smartsort-shuttle";

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

// ============================================================
// 점진적 이동(Smooth Move) 설정
// ============================================================
// 위치 서보 (방향 전환 MG996R): 각도 스텝 기반
const int   SMOOTH_STEP_DEG       = 2;     // 1스텝당 이동 각도 (°)
const int   SMOOTH_STEP_DELAY_MS  = 15;    // 스텝 간 대기시간 (ms)

// 연속회전 서보 (MG90S / MG996R): 속도 램프 기반
const int   RAMP_STEP_DEG        = 5;      // 1스텝당 속도 변화 (°, 90 기준 오프셋)
const int   RAMP_STEP_DELAY_MS   = 20;     // 램프 스텝 간 대기시간 (ms)

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
RemoteSerial LOG;

// 서보 객체 — 총 8개
Servo _servoDirL;        // 방향 전환 좌측 (래크 앤 피니언)
Servo _servoDirR;        // 방향 전환 우측 (래크 앤 피니언)
Servo _servoXL;          // X축 좌측
Servo _servoXR;          // X축 우측
Servo _servoYL;          // Y축 좌측
Servo _servoYR;          // Y축 우측
Servo _servoZL;          // Z축 좌측 (스풀/벨트)
Servo _servoZR;          // Z축 우측 (스풀/벨트)

// 현재 서보 각도 추적 — 점진적 이동에 필요
int _angleDirL = DIR_ANGLE_X_MODE;
int _angleDirR = DIR_ANGLE_X_MODE;

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
void setupMDNS();
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

// 점진적 이동 함수
void smoothServoMove(Servo& servo, int& currentAngle, int targetAngle);
void smoothServoPairMove(Servo& servoL, Servo& servoR,
                         int& currentAngleL, int& currentAngleR,
                         int targetAngleL, int targetAngleR);
void rampDrivePair(Servo& servoL, Servo& servoR, int targetAngle);
void rampStopPair(Servo& servoL, Servo& servoR, int currentDriveAngle);
void rampDrivePairUs(Servo& servoL, Servo& servoR,
                     int stopL, int stopR, int targetL, int targetR);
void rampStopPairUs(Servo& servoL, Servo& servoR,
                    int currentL, int currentR, int stopL, int stopR);

// ============================================================
// 점진적 이동 구현 — 위치 서보용 (방향 전환 MG996R)
// ============================================================

/**
 * @brief 위치 서보를 현재 각도에서 목표 각도까지 점진적으로 이동한다.
 *        SMOOTH_STEP_DEG 단위로 SMOOTH_STEP_DELAY_MS 간격 이동.
 * @param servo        제어 대상 서보
 * @param currentAngle 현재 각도 (업데이트됨)
 * @param targetAngle  목표 각도
 */
void smoothServoMove(Servo& servo, int& currentAngle, int targetAngle) {
    targetAngle = constrain(targetAngle, 0, 180);

    if (currentAngle == targetAngle) return;

    int step = (targetAngle > currentAngle) ? SMOOTH_STEP_DEG : -SMOOTH_STEP_DEG;

    while (currentAngle != targetAngle) {
        currentAngle += step;

        // 오버슈트 방지
        if ((step > 0 && currentAngle > targetAngle) ||
            (step < 0 && currentAngle < targetAngle)) {
            currentAngle = targetAngle;
        }

        servo.write(currentAngle);
        delay(SMOOTH_STEP_DELAY_MS);
    }
}

/**
 * @brief 좌/우 위치 서보 쌍을 동시에 점진적으로 이동한다.
 *        두 서보가 같은 스텝으로 동기 이동하여 래크 앤 피니언 균형을 유지한다.
 * @param servoL        좌측 서보
 * @param servoR        우측 서보
 * @param currentAngleL 좌측 현재 각도 (업데이트됨)
 * @param currentAngleR 우측 현재 각도 (업데이트됨)
 * @param targetAngleL  좌측 목표 각도
 * @param targetAngleR  우측 목표 각도
 */
void smoothServoPairMove(Servo& servoL, Servo& servoR,
                         int& currentAngleL, int& currentAngleR,
                         int targetAngleL, int targetAngleR) {
    targetAngleL = constrain(targetAngleL, 0, 180);
    targetAngleR = constrain(targetAngleR, 0, 180);

    int stepL = (targetAngleL > currentAngleL) ? SMOOTH_STEP_DEG : -SMOOTH_STEP_DEG;
    int stepR = (targetAngleR > currentAngleR) ? SMOOTH_STEP_DEG : -SMOOTH_STEP_DEG;

    bool doneL = (currentAngleL == targetAngleL);
    bool doneR = (currentAngleR == targetAngleR);

    while (!doneL || !doneR) {
        if (!doneL) {
            currentAngleL += stepL;
            if ((stepL > 0 && currentAngleL >= targetAngleL) ||
                (stepL < 0 && currentAngleL <= targetAngleL)) {
                currentAngleL = targetAngleL;
                doneL = true;
            }
            servoL.write(currentAngleL);
        }

        if (!doneR) {
            currentAngleR += stepR;
            if ((stepR > 0 && currentAngleR >= targetAngleR) ||
                (stepR < 0 && currentAngleR <= targetAngleR)) {
                currentAngleR = targetAngleR;
                doneR = true;
            }
            servoR.write(currentAngleR);
        }

        delay(SMOOTH_STEP_DELAY_MS);
    }
}

// ============================================================
// 점진적 이동 구현 — 연속회전 서보용 (MG90S / MG996R 스풀)
// ============================================================

/**
 * @brief 연속회전 서보 쌍을 정지(90°)에서 목표 속도까지 점진적으로 가속한다.
 *        90° → targetAngle 방향으로 RAMP_STEP_DEG씩 램프업.
 * @param servoL      좌측 서보
 * @param servoR      우측 서보
 * @param targetAngle 목표 각도 (0=정방향 풀스피드, 180=역방향 풀스피드)
 */
void rampDrivePair(Servo& servoL, Servo& servoR, int targetAngle) {
    int current = MG90S_STOP;  // 시작: 정지 (90°)
    int step = (targetAngle > current) ? RAMP_STEP_DEG : -RAMP_STEP_DEG;

    while (current != targetAngle) {
        current += step;

        // 오버슈트 방지
        if ((step > 0 && current > targetAngle) ||
            (step < 0 && current < targetAngle)) {
            current = targetAngle;
        }

        servoL.write(current);
        servoR.write(current);
        delay(RAMP_STEP_DELAY_MS);
    }
}

/**
 * @brief 연속회전 서보 쌍을 현재 속도에서 정지(90°)까지 점진적으로 감속한다.
 *        currentDriveAngle → 90° 방향으로 RAMP_STEP_DEG씩 램프다운.
 * @param servoL           좌측 서보
 * @param servoR           우측 서보
 * @param currentDriveAngle 현재 구동 각도
 */
void rampStopPair(Servo& servoL, Servo& servoR, int currentDriveAngle) {
    int current = currentDriveAngle;
    int target = MG90S_STOP;  // 목표: 정지 (90°)
    int step = (target > current) ? RAMP_STEP_DEG : -RAMP_STEP_DEG;

    while (current != target) {
        current += step;

        if ((step > 0 && current > target) ||
            (step < 0 && current < target)) {
            current = target;
        }

        servoL.write(current);
        servoR.write(current);
        delay(RAMP_STEP_DELAY_MS);
    }
}

// ============================================================
// µs 기반 점진적 가감속 — 좌/우 독립 오프셋 지원
// ============================================================

/**
 * @brief 정지 펄스에서 목표 펄스까지 좌/우 서보를 비례 동기 가속한다.
 *        오프셋이 큰 쪽 기준으로 스텝 수를 결정하고, 작은 쪽은 비례 보간.
 */
void rampDrivePairUs(Servo& servoL, Servo& servoR,
                     int stopL, int stopR,
                     int targetL, int targetR) {
    int maxOffset = max(abs(targetL - stopL), abs(targetR - stopR));
    int steps = maxOffset / RAMP_US_STEP;
    if (steps < 1) steps = 1;

    for (int i = 1; i <= steps; i++) {
        int pulseL = stopL + (long)(targetL - stopL) * i / steps;
        int pulseR = stopR + (long)(targetR - stopR) * i / steps;
        servoL.writeMicroseconds(pulseL);
        servoR.writeMicroseconds(pulseR);
        delay(RAMP_US_DELAY_MS);
    }
}

/**
 * @brief 현재 구동 펄스에서 정지 펄스까지 좌/우 서보를 비례 동기 감속한다.
 */
void rampStopPairUs(Servo& servoL, Servo& servoR,
                    int currentL, int currentR,
                    int stopL, int stopR) {
    int maxOffset = max(abs(stopL - currentL), abs(stopR - currentR));
    int steps = maxOffset / RAMP_US_STEP;
    if (steps < 1) steps = 1;

    for (int i = 1; i <= steps; i++) {
        int pulseL = currentL + (long)(stopL - currentL) * i / steps;
        int pulseR = currentR + (long)(stopR - currentR) * i / steps;
        servoL.writeMicroseconds(pulseL);
        servoR.writeMicroseconds(pulseR);
        delay(RAMP_US_DELAY_MS);
    }
}

// ============================================================
// setup / loop
// ============================================================

void setup() {
    Serial.begin(115200);
    LOG.println("\n[셔틀 ESP32] 시작");

    setupEndstops();
    setupWiFi();
    setupMDNS();
    LOG.begin(23);
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
        setupMDNS();
    }

    LOG.handle();

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
    LOG.printf("[WiFi] %s 에 연결 중...\n", WIFI_SSID);
    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

    int retryCount = 0;
    while (WiFi.status() != WL_CONNECTED && retryCount < 20) {
        delay(500);
        LOG.print(".");
        retryCount++;
    }

    if (WiFi.status() == WL_CONNECTED) {
        LOG.printf("\n[WiFi] 연결 완료 — IP: %s\n", WiFi.localIP().toString().c_str());
    } else {
        LOG.println("\n[WiFi] 연결 실패 — 재시도 예정");
    }
}

/**
 * @brief mDNS 응답자를 시작하고 Telnet/MQTT 서비스를 광고한다.
 *        WiFi 연결 완료 후 호출할 것.
 */
void setupMDNS() {
    if (!MDNS.begin(MDNS_HOSTNAME)) {
        LOG.println("[mDNS] 시작 실패");
        return;
    }

    // 서비스 광고 — 네트워크 스캐너(Bonjour Browser 등)에서 발견 가능
    MDNS.addService("telnet", "tcp", 23);
    MDNS.addService("smartsort", "tcp", 23);  // 커스텀 식별자

    LOG.printf("[mDNS] 시작 완료 — http://%s.local (Telnet 23)\n",
                  MDNS_HOSTNAME);
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
    LOG.println("[엔드스톱] 초기화 완료");
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
    _angleDirL = DIR_ANGLE_X_MODE;       // 각도 추적 동기화
    _angleDirR = DIR_ANGLE_X_MODE;

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

    LOG.println("[서보] 8개 초기화 완료 (방향전환 ×2, X ×2, Y ×2, Z ×2)");
}

// ============================================================
// MQTT 연결 및 콜백
// ============================================================

/**
 * @brief MQTT 브로커에 연결하고 토픽을 구독한다.
 * @return 연결 성공 여부
 */
bool reconnectMQTT() {
    LOG.println("[MQTT] 브로커 연결 시도...");

    if (_mqttClient.connect(MQTT_CLIENT_ID)) {
        LOG.println("[MQTT] 연결 성공");
        _mqttClient.subscribe(TOPIC_CMD_SHUTTLE);
        LOG.printf("[MQTT] 구독: %s\n", TOPIC_CMD_SHUTTLE);
        publishStatus();
        return true;
    }

    LOG.printf("[MQTT] 연결 실패 — rc=%d\n", _mqttClient.state());
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
    LOG.printf("[MQTT] 수신 — 토픽: %s\n", topic);

    JsonDocument doc;
    DeserializationError error = deserializeJson(doc, payload, length);

    if (error) {
        LOG.printf("[MQTT] JSON 파싱 실패: %s\n", error.c_str());
        publishAlert("error", "JSON 파싱 실패");
        return;
    }

    if (!doc["action"].isNull()) {
        const char* action = doc["action"];
        if (strcmp(action, "home") == 0) {
            handleHomeCommand();
            return;
        }

        if (strcmp(action, "servo_direct") == 0) {
            int angle = doc["angle"] | -1;
            if (angle < 0 || angle > 180) {
                publishAlert("error", "angle 범위 오류 (0~180)");
                return;
            }

            // pin이 배열인지 단일값인지 판별
            JsonVariant pinVar = doc["pin"];

            if (pinVar.is<JsonArray>()) {
                JsonArray pins = pinVar.as<JsonArray>();

                // 핀 2개 + 방향전환 쌍(13,15)인 경우 → 동기 점진 이동
                if (pins.size() == 2) {
                    int p1 = pins[0] | -1;
                    int p2 = pins[1] | -1;

                    bool isDirPair = (p1 == PIN_DIR_LEFT && p2 == PIN_DIR_RIGHT) ||
                                    (p1 == PIN_DIR_RIGHT && p2 == PIN_DIR_LEFT);

                    if (isDirPair) {
                        smoothServoPairMove(_servoDirL, _servoDirR,
                                            _angleDirL, _angleDirR,
                                            angle, angle);
                        LOG.printf("[서보직접] 방향전환 쌍(13,15) → %d° (sync smooth)\n", angle);
                        publishStatus();
                        return;
                    }

                    publishAlert("error", "지원하지 않는 핀 쌍");
                    return;
                }

                publishAlert("error", "pin 배열 크기 오류 (2개만 지원)");
                return;
            }

            // 단일 핀 (기존 동작 유지)
            int pin = pinVar | -1;
            if (pin == PIN_DIR_LEFT) {
                smoothServoMove(_servoDirL, _angleDirL, angle);
                LOG.printf("[서보직접] 핀 %d → %d° (smooth)\n", pin, angle);
                publishStatus();
            } else if (pin == PIN_DIR_RIGHT) {
                smoothServoMove(_servoDirR, _angleDirR, angle);
                LOG.printf("[서보직접] 핀 %d → %d° (smooth)\n", pin, angle);
                publishStatus();
            } else {
                publishAlert("error", "지원하지 않는 핀 번호");
            }
            return;
        }

        if (strcmp(action, "drive_direct") == 0) {
            const char* axis = doc["axis"] | "";
            JsonVariant angleVar = doc["angle"];
            unsigned long durationMs = doc["duration_ms"] | 0UL;

            if (durationMs == 0) {
                publishAlert("error", "duration_ms 필수");
                return;
            }

            Servo* servoL = nullptr;
            Servo* servoR = nullptr;
            int stopAngle = MG90S_STOP;

            if (strcmp(axis, "x") == 0) {
                servoL = &_servoXL;
                servoR = &_servoXR;
            } else if (strcmp(axis, "y") == 0) {
                servoL = &_servoYL;
                servoR = &_servoYR;
            } else if (strcmp(axis, "z") == 0) {
                servoL = &_servoZL;
                servoR = &_servoZR;
                stopAngle = Z_LIFT_STOP;
            } else {
                publishAlert("error", "axis 오류 (x/y/z)");
                return;
            }

            int angleL, angleR;
            if (angleVar.is<JsonArray>()) {
                JsonArray angles = angleVar.as<JsonArray>();
                if (angles.size() != 2) {
                    publishAlert("error", "angle 배열 크기 오류 (2개)");
                    return;
                }
                angleL = angles[0] | -1;
                angleR = angles[1] | -1;
            } else {
                int a = angleVar | -1;
                angleL = a;
                angleR = a;
            }

            if (angleL < 0 || angleL > 180 || angleR < 0 || angleR > 180) {
                publishAlert("error", "angle 범위 오류 (0~180)");
                return;
            }

            LOG.printf("[주행직접] axis=%s L:%d° R:%d° %lums\n",
                        axis, angleL, angleR, durationMs);

            // X축: 캘리브레이션 µs 값 적용
            if (strcmp(axis, "x") == 0) {
                int usL, usR;
                if (angleL < MG90S_STOP) {        // 정방향 (0°)
                    usL = X_FWD_US_L;
                    usR = X_FWD_US_R;
                } else if (angleL > MG90S_STOP) { // 역방향 (180°)
                    usL = X_REV_US_L;
                    usR = X_REV_US_R;
                } else {                           // 정지 (90°)
                    usL = STOP_US_X_LEFT;
                    usR = STOP_US_X_RIGHT;
                }

                rampDrivePairUs(_servoXL, _servoXR,
                                STOP_US_X_LEFT, STOP_US_X_RIGHT, usL, usR);

                unsigned long startMs = millis();
                while (millis() - startMs < durationMs) {
                    _mqttClient.loop();
                    delay(10);
                }

                rampStopPairUs(_servoXL, _servoXR,
                               usL, usR, STOP_US_X_LEFT, STOP_US_X_RIGHT);
            } else if (strcmp(axis, "y") == 0) {
                int usL, usR;
                if (angleL < MG90S_STOP) {
                    usL = Y_FWD_US_L;
                    usR = Y_FWD_US_R;
                } else if (angleL > MG90S_STOP) {
                    usL = Y_REV_US_L;
                    usR = Y_REV_US_R;
                } else {
                    usL = STOP_US_Y_LEFT;
                    usR = STOP_US_Y_RIGHT;
                }

                rampDrivePairUs(_servoYL, _servoYR,
                                STOP_US_Y_LEFT, STOP_US_Y_RIGHT, usL, usR);

                unsigned long startMs = millis();
                while (millis() - startMs < durationMs) {
                    _mqttClient.loop();
                    delay(10);
                }

                rampStopPairUs(_servoYL, _servoYR,
                               usL, usR, STOP_US_Y_LEFT, STOP_US_Y_RIGHT);
            } else {
                // Z축: 기존 동작 유지
                servoL->write(angleL);
                servoR->write(angleR);

                unsigned long startMs = millis();
                while (millis() - startMs < durationMs) {
                    _mqttClient.loop();
                    delay(10);
                }

                servoL->write(stopAngle);
                servoR->write(stopAngle);
            }

            publishStatus();
            return;
        }

        if (strcmp(action, "drive_direct_us") == 0) {
            const char* axis = doc["axis"] | "";
            JsonVariant pulseVar = doc["pulse"];
            unsigned long durationMs = doc["duration_ms"] | 0UL;

            if (durationMs == 0) {
                publishAlert("error", "duration_ms 필수");
                return;
            }

            Servo* servoL = nullptr;
            Servo* servoR = nullptr;
            int stopPulseL = 1500;
            int stopPulseR = 1500;

            if (strcmp(axis, "x") == 0) {
                servoL = &_servoXL;
                servoR = &_servoXR;
                stopPulseL = STOP_US_X_LEFT;
                stopPulseR = STOP_US_X_RIGHT;
            } else if (strcmp(axis, "y") == 0) {
                servoL = &_servoYL;
                servoR = &_servoYR;
                stopPulseL = STOP_US_Y_LEFT;
                stopPulseR = STOP_US_Y_RIGHT;
            } else if (strcmp(axis, "z") == 0) {
                servoL = &_servoZL;
                servoR = &_servoZR;
            } else {
                publishAlert("error", "axis 오류 (x/y/z)");
                return;
            }

            int pulseL, pulseR;
            if (pulseVar.is<JsonArray>()) {
                JsonArray pulses = pulseVar.as<JsonArray>();
                if (pulses.size() != 2) {
                    publishAlert("error", "pulse 배열 크기 오류 (2개)");
                    return;
                }
                pulseL = pulses[0] | -1;
                pulseR = pulses[1] | -1;
            } else {
                int p = pulseVar | -1;
                pulseL = p;
                pulseR = p;
            }

            if (pulseL < 1000 || pulseL > 2000 || pulseR < 1000 || pulseR > 2000) {
                publishAlert("error", "pulse 범위 오류 (1000~2000us)");
                return;
            }

            LOG.printf("[주행직접us] axis=%s L:%dus R:%dus %lums\n",
                        axis, pulseL, pulseR, durationMs);

            servoL->writeMicroseconds(pulseL);
            servoR->writeMicroseconds(pulseR);

            unsigned long startMs = millis();
            while (millis() - startMs < durationMs) {
                _mqttClient.loop();
                delay(10);
            }

            servoL->writeMicroseconds(stopPulseL);
            servoR->writeMicroseconds(stopPulseR);

            publishStatus();
            return;
        }
    }

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

    if (targetX < 0 || targetY < 0 || targetZ < 0) {
        publishAlert("error", "필수 필드 누락 (target_x, target_y, target_z)");
        return;
    }

    if (targetX >= GRID_MAX_X || targetY >= GRID_MAX_Y || targetZ >= GRID_MAX_Z) {
        LOG.printf("[셔틀] 그리드 범위 초과 — 목표: (%d,%d,%d), 최대: (%d,%d,%d)\n",
                      targetX, targetY, targetZ,
                      GRID_MAX_X - 1, GRID_MAX_Y - 1, GRID_MAX_Z - 1);
        publishAlert("error", "그리드 범위 초과");
        return;
    }

    if (!_isHomed) {
        LOG.println("[셔틀] 경고: 홈 캘리브레이션 미완료 상태에서 이동 시도");
        publishAlert("warn", "홈 캘리브레이션 미완료");
    }

    _currentBinId = binId;

    LOG.printf("[셔틀] 이동 명령 — 목표: (%d,%d,%d) Bin: %s\n",
                  targetX, targetY, targetZ, binId);

    executeTransferSequence(targetX, targetY, targetZ, binId);
}

/**
 * @brief 엔드스톱 기반 홈 포지션 (0,0,0) 캘리브레이션을 수행한다.
 */
void handleHomeCommand() {
    LOG.println("[셔틀] 홈 캘리브레이션 시작");
    _currentState = STATE_HOMING;
    publishStatus();

    switchToXMode();
    delay(DIR_SWITCH_DELAY);

    bool zOk = homeAxis(PIN_ENDSTOP_Z, _servoZL, _servoZR, Z_LIFT_DOWN, "Z");
    bool xOk = homeAxis(PIN_ENDSTOP_X, _servoXL, _servoXR, MG90S_CCW, "X");

    switchToYMode();
    delay(DIR_SWITCH_DELAY);

    bool yOk = homeAxis(PIN_ENDSTOP_Y, _servoYL, _servoYR, MG90S_CCW, "Y");

    switchToXMode();
    delay(DIR_SWITCH_DELAY);

    if (xOk && yOk && zOk) {
        _currentX = 0;
        _currentY = 0;
        _currentZ = 0;
        _isHomed = true;
        _currentState = STATE_IDLE;
        LOG.println("[셔틀] 홈 캘리브레이션 완료 — (0,0,0)");
    } else {
        _currentState = STATE_ERROR;
        _isHomed = false;
        LOG.println("[셔틀] 홈 캘리브레이션 실패");
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
        LOG.printf("[셔틀] X축 이동: %d → %d (%+d칸)\n", _currentX, targetX, deltaX);
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
        LOG.printf("[셔틀] Y축 이동: %d → %d (%+d칸)\n", _currentY, targetY, deltaY);
        moveY(deltaY);
        _currentY = targetY;
    }

    // --- 7단계: Z축 리프트 ---
    if (deltaZ != 0) {
        _currentState = STATE_LIFTING_Z;
        publishStatus();
        LOG.printf("[셔틀] Z축 리프트: %d → %d (%+d층)\n", _currentZ, targetZ, deltaZ);
        moveZ(deltaZ);
        _currentZ = targetZ;
    }

    // 이송 완료 — X축 접지 상태로 복원
    switchToXMode();
    delay(DIR_SWITCH_DELAY);

    _currentState = STATE_IDLE;
    LOG.printf("[셔틀] 이송 완료 — 위치: (%d,%d,%d) Bin: %s\n",
                  _currentX, _currentY, _currentZ, binId);

    publishStatus();
}

// ============================================================
// 방향 전환 — 래크 앤 피니언 슬라이더 (점진적 이동 적용)
// ============================================================

/**
 * @brief Y축 바퀴를 점진적으로 상승시켜 X축 바퀴만 접지한다. (X축 이동 모드)
 *        좌/우 MG996R 2개를 동시에 점진적으로 구동.
 */
void switchToXMode() {
    smoothServoPairMove(_servoDirL, _servoDirR,
                        _angleDirL, _angleDirR,
                        DIR_ANGLE_X_MODE, DIR_ANGLE_X_MODE);
    LOG.println("[방향전환] X축 모드 (Y축 바퀴 양측 점진적 상승)");
}

/**
 * @brief Y축 바퀴를 점진적으로 하강시켜 Y축 바퀴를 접지한다. (Y축 이동 모드)
 *        좌/우 MG996R 2개를 동시에 점진적으로 구동.
 */
void switchToYMode() {
    smoothServoPairMove(_servoDirL, _servoDirR,
                        _angleDirL, _angleDirR,
                        DIR_ANGLE_Y_MODE, DIR_ANGLE_Y_MODE);
    LOG.println("[방향전환] Y축 모드 (Y축 바퀴 양측 점진적 하강)");
}

// ============================================================
// 축별 이동 제어 (점진적 가감속 적용)
// ============================================================

/**
 * @brief X축으로 지정 셀 수만큼 이동한다. (µs 기반 캘리브레이션 적용)
 *        좌/우 독립 오프셋으로 직진성 보정.
 * @param cells 이동할 셀 수 (양수=전진, 음수=후진)
 */
void moveX(int cells) {
    int targetL, targetR;

    if (cells > 0) {
        targetL = X_FWD_US_L;
        targetR = X_FWD_US_R;
    } else {
        targetL = X_REV_US_L;
        targetR = X_REV_US_R;
    }

    int stopL = STOP_US_X_LEFT;
    int stopR = STOP_US_X_RIGHT;

    // 램프 시간 계산
    int maxOffset = max(abs(targetL - stopL), abs(targetR - stopR));
    int rampSteps = maxOffset / RAMP_US_STEP;
    unsigned long rampTime = (unsigned long)rampSteps * RAMP_US_DELAY_MS;
    unsigned long totalDuration = abs(cells) * MOVE_TIME_PER_CELL_XY;

    unsigned long cruiseTime = 0;
    if (totalDuration > rampTime * 2) {
        cruiseTime = totalDuration - rampTime * 2;
    }

    // 램프업 → 정속 → 램프다운
    rampDrivePairUs(_servoXL, _servoXR, stopL, stopR, targetL, targetR);

    if (cruiseTime > 0) {
        unsigned long startMs = millis();
        while (millis() - startMs < cruiseTime) {
            _mqttClient.loop();
            delay(10);
        }
    }

    rampStopPairUs(_servoXL, _servoXR, targetL, targetR, stopL, stopR);
}

/**
 * @brief Y축으로 지정 셀 수만큼 이동한다. (µs 기반 캘리브레이션 적용)
 *        좌/우 독립 오프셋으로 직진성 보정.
 * @param cells 이동할 셀 수 (양수=전진, 음수=후진)
 */
void moveY(int cells) {
    int targetL, targetR;

    if (cells > 0) {
        targetL = Y_FWD_US_L;
        targetR = Y_FWD_US_R;
    } else {
        targetL = Y_REV_US_L;
        targetR = Y_REV_US_R;
    }

    int stopL = STOP_US_Y_LEFT;
    int stopR = STOP_US_Y_RIGHT;

    int maxOffset = max(abs(targetL - stopL), abs(targetR - stopR));
    int rampSteps = maxOffset / RAMP_US_STEP;
    unsigned long rampTime = (unsigned long)rampSteps * RAMP_US_DELAY_MS;
    unsigned long totalDuration = abs(cells) * MOVE_TIME_PER_CELL_XY;

    unsigned long cruiseTime = 0;
    if (totalDuration > rampTime * 2) {
        cruiseTime = totalDuration - rampTime * 2;
    }

    rampDrivePairUs(_servoYL, _servoYR, stopL, stopR, targetL, targetR);

    if (cruiseTime > 0) {
        unsigned long startMs = millis();
        while (millis() - startMs < cruiseTime) {
            _mqttClient.loop();
            delay(10);
        }
    }

    rampStopPairUs(_servoYL, _servoYR, targetL, targetR, stopL, stopR);
}

/**
 * @brief Z축으로 지정 층 수만큼 리프트한다. (램프업 → 정속 → 램프다운)
 * @param cells 이동할 층 수 (부호 포함)
 */
void moveZ(int cells) {
    int driveAngle = (cells > 0) ? Z_LIFT_UP : Z_LIFT_DOWN;

    int rampSteps = abs(driveAngle - Z_LIFT_STOP) / RAMP_STEP_DEG;
    unsigned long rampTime = (unsigned long)rampSteps * RAMP_STEP_DELAY_MS;
    unsigned long totalDuration = abs(cells) * MOVE_TIME_PER_CELL_Z;

    unsigned long cruiseTime = 0;
    if (totalDuration > rampTime * 2) {
        cruiseTime = totalDuration - rampTime * 2;
    }

    rampDrivePair(_servoZL, _servoZR, driveAngle);

    if (cruiseTime > 0) {
        delay(cruiseTime);
    }

    rampStopPair(_servoZL, _servoZR, driveAngle);
}

/**
 * @brief 모든 주행 서보를 점진적으로 정지한다. (비상 정지용)
 *        비상 시에는 즉시 정지 — 안전 우선
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
// 엔드스톱 기반 홈 복귀 (점진적 가속 후 저속 접근)
// ============================================================

/**
 * @brief 엔드스톱 스위치가 눌릴 때까지 지정 축을 역방향으로 구동하여 홈 위치로 복귀한다.
 *        점진적 가속 후 정속 → 엔드스톱 감지 시 점진적 감속 정지.
 * @param endstopPin 엔드스톱 GPIO 핀
 * @param servoL 좌측 서보
 * @param servoR 우측 서보
 * @param driveAngle 역방향 구동 각도
 * @param axisName 축 이름 (로그용)
 * @return 홈 도달 성공 여부
 */
bool homeAxis(int endstopPin, Servo& servoL, Servo& servoR, int driveAngle, const char* axisName) {
    LOG.printf("[홈] %s축 홈 복귀 시작 (점진적)\n", axisName);

    unsigned long timeout = 15000;
    unsigned long startTime = millis();

    // 점진적 가속으로 구동 시작
    rampDrivePair(servoL, servoR, driveAngle);

    while (digitalRead(endstopPin) == HIGH) {
        if (millis() - startTime > timeout) {
            // 타임아웃 시 점진적 감속 정지
            rampStopPair(servoL, servoR, driveAngle);
            LOG.printf("[홈] %s축 타임아웃 — 엔드스톱 미감지\n", axisName);
            return false;
        }
        delay(10);
    }

    // 엔드스톱 감지 → 점진적 감속 정지
    rampStopPair(servoL, servoR, driveAngle);
    LOG.printf("[홈] %s축 홈 도달\n", axisName);
    return true;
}

// ============================================================
// 상태 발행
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
    LOG.printf("[MQTT] 상태 발행: %s\n", buffer);
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
    LOG.printf("[MQTT] 알림 발행: %s\n", buffer);
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
}   