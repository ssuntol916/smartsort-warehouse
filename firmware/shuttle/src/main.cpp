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
// 서보모터 핀 매핑
// ============================================================
const int SERVO_PIN_X = 18;  // X축 서보
const int SERVO_PIN_Y = 19;  // Y축 서보
const int SERVO_PIN_Z = 21;  // Z축 서보

// 서보 각도 범위
const int SERVO_MIN_ANGLE = 0;
const int SERVO_MAX_ANGLE = 180;

// ============================================================
// 전역 객체
// ============================================================
WiFiClient _wifiClient;
PubSubClient _mqttClient(_wifiClient);

Servo _servoX;
Servo _servoY;
Servo _servoZ;

// 현재 셔틀 상태
int _currentX = 0;
int _currentY = 0;
int _currentZ = 0;
String _currentState = "idle";

// MQTT 재연결 간격
unsigned long _lastReconnectAttempt = 0;
const unsigned long RECONNECT_INTERVAL = 5000;

// ============================================================
// 함수 선언
// ============================================================
void setupWiFi();
void setupServos();
bool reconnectMQTT();
void onMqttMessage(char* topic, byte* payload, unsigned int length);
void handleServoCommand(JsonDocument& doc);
void handleMoveCommand(JsonDocument& doc);
void publishStatus();
void publishAlert(const char* level, const char* msg);
Servo* getServoByPin(int pin);
int clampAngle(int angle);

// ============================================================
// setup / loop
// ============================================================

void setup() {
    Serial.begin(115200);
    Serial.println("\n[셔틀 ESP32] 시작");

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
// 서보모터 초기화
// ============================================================

/**
 * @brief 서보모터를 초기화하고 핀에 연결한다.
 */
void setupServos() {
    ESP32PWM::allocateTimer(0);
    ESP32PWM::allocateTimer(1);
    ESP32PWM::allocateTimer(2);

    _servoX.setPeriodHertz(50);
    _servoY.setPeriodHertz(50);
    _servoZ.setPeriodHertz(50);

    _servoX.attach(SERVO_PIN_X, 500, 2400);
    _servoY.attach(SERVO_PIN_Y, 500, 2400);
    _servoZ.attach(SERVO_PIN_Z, 500, 2400);

    // 초기 위치 (0도)
    _servoX.write(0);
    _servoY.write(0);
    _servoZ.write(0);

    Serial.println("[서보] 초기화 완료");
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

    // type 필드로 명령 분기
    const char* type = doc["type"] | "move";

    if (strcmp(type, "servo") == 0) {
        handleServoCommand(doc);
    } else if (strcmp(type, "move") == 0) {
        handleMoveCommand(doc);
    } else {
        Serial.printf("[MQTT] 알 수 없는 명령 타입: %s\n", type);
        publishAlert("warn", "알 수 없는 명령 타입");
    }
}

// ============================================================
// 명령 처리
// ============================================================

/**
 * @brief 서보모터 개별 제어 명령을 처리한다.
 * @param doc JSON 페이로드 {"type":"servo","pin":18,"angle":1,"direction":"cw"}
 *            또는 {"type":"servo","pin":18,"angle":90,"mode":"absolute"}
 */
void handleServoCommand(JsonDocument& doc) {
    int pin = doc["pin"] | -1;
    int angle = doc["angle"] | 0;
    const char* direction = doc["direction"] | "cw";
    const char* mode = doc["mode"] | "relative";

    Servo* targetServo = getServoByPin(pin);
    if (targetServo == nullptr) {
        Serial.printf("[서보] 잘못된 핀 번호: %d\n", pin);
        publishAlert("error", "잘못된 서보 핀 번호");
        return;
    }

    int currentAngle = targetServo->read();
    int newAngle;

    if (strcmp(mode, "absolute") == 0) {
        // 절대 위치 이동
        newAngle = clampAngle(angle);
    } else {
        // 상대 이동 (cw: +, ccw: -)
        int delta = (strcmp(direction, "ccw") == 0) ? -angle : angle;
        newAngle = clampAngle(currentAngle + delta);
    }

    targetServo->write(newAngle);
    Serial.printf("[서보] 핀 %d → %d도\n", pin, newAngle);

    publishStatus();
}

/**
 * @brief 셔틀 그리드 이동 명령을 처리한다.
 * @param doc JSON 페이로드 {"type":"move","x":1,"y":2,"z":0,"action":"store"}
 */
void handleMoveCommand(JsonDocument& doc) {
    int targetX = doc["x"] | 0;
    int targetY = doc["y"] | 0;
    int targetZ = doc["z"] | 0;
    const char* action = doc["action"] | "move";

    Serial.printf("[셔틀] 이동 명령 — 목표: (%d,%d,%d) 동작: %s\n",
                  targetX, targetY, targetZ, action);

    _currentState = "moving";
    publishStatus();

    // TODO: 실제 모터 제어 로직 구현
    // 각 축 서보를 목표 위치에 맞는 각도로 이동
    _currentX = targetX;
    _currentY = targetY;
    _currentZ = targetZ;

    if (strcmp(action, "home") == 0) {
        _servoX.write(0);
        _servoY.write(0);
        _servoZ.write(0);
        _currentX = 0;
        _currentY = 0;
        _currentZ = 0;
    }

    _currentState = "idle";
    Serial.printf("[셔틀] 이동 완료 — 위치: (%d,%d,%d)\n",
                  _currentX, _currentY, _currentZ);

    publishStatus();
}

// ============================================================
// 상태 발행
// ============================================================

/**
 * @brief 현재 셔틀 상태를 MQTT로 발행한다.
 */
void publishStatus() {
    JsonDocument doc;
    doc["x"] = _currentX;
    doc["y"] = _currentY;
    doc["z"] = _currentZ;
    doc["state"] = _currentState;
    doc["servo_x"] = _servoX.read();
    doc["servo_y"] = _servoY.read();
    doc["servo_z"] = _servoZ.read();

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
    doc["level"] = level;
    doc["source"] = "shuttle";
    doc["msg"] = msg;

    char buffer[256];
    serializeJson(doc, buffer);

    _mqttClient.publish(TOPIC_ALERT, buffer);
    Serial.printf("[MQTT] 알림 발행: %s\n", buffer);
}

// ============================================================
// 유틸리티
// ============================================================

/**
 * @brief 핀 번호로 해당 서보 객체를 반환한다.
 * @param pin GPIO 핀 번호
 * @return 서보 포인터. 매칭 실패 시 nullptr
 */
Servo* getServoByPin(int pin) {
    if (pin == SERVO_PIN_X) return &_servoX;
    if (pin == SERVO_PIN_Y) return &_servoY;
    if (pin == SERVO_PIN_Z) return &_servoZ;
    return nullptr;
}

/**
 * @brief 서보 각도를 유효 범위(0~180)로 제한한다.
 * @param angle 입력 각도
 * @return 제한된 각도
 */
int clampAngle(int angle) {
    if (angle < SERVO_MIN_ANGLE) return SERVO_MIN_ANGLE;
    if (angle > SERVO_MAX_ANGLE) return SERVO_MAX_ANGLE;
    return angle;
}
