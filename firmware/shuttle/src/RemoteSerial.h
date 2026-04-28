// ============================================================
// 파일명  : RemoteSerial.h
// 역할    : USB Serial + Telnet 양방향 미러링 로거
// 작성자  : 송준호
// 작성일  : 2026-04-15
// ============================================================
#pragma once

#include <Arduino.h>
#include <WiFi.h>

class RemoteSerial : public Print {
public:
    /**
     * @brief Telnet 서버를 시작한다. WiFi 연결 후 호출할 것.
     * @param port Telnet 포트 (기본 23)
     */
    void begin(uint16_t port = 23) {
        if (_server) return;
        _server = new WiFiServer(port);
        _server->begin();
        _server->setNoDelay(true);
        Serial.printf("[RemoteSerial] Telnet 서버 시작 — %s:%u\n",
                      WiFi.localIP().toString().c_str(), port);
    }

    /**
     * @brief loop()에서 주기적으로 호출. 신규 클라이언트 수락 및 입력 비움.
     */
    void handle() {
        if (!_server) return;

        // 신규 접속 처리 (1개 클라이언트만 허용)
        if (_server->hasClient()) {
            if (_client && _client.connected()) {
                _client.println("[RemoteSerial] 다른 클라이언트가 접속해 연결을 종료합니다.");
                _client.stop();
            }
            _client = _server->available();
            _client.setNoDelay(true);
            _client.printf("[RemoteSerial] 연결됨 — %s\n",
                           WiFi.localIP().toString().c_str());
        }

        // 끊어진 클라이언트 정리
        if (_client && !_client.connected()) {
            _client.stop();
        }

        // 클라이언트 입력 비우기 (지금은 단방향 로깅용)
        if (_client && _client.connected()) {
            while (_client.available()) {
                _client.read();
            }
        }
    }

    // ---- Print 인터페이스 구현 ----
    size_t write(uint8_t c) override {
        Serial.write(c);
        if (_client && _client.connected()) {
            _client.write(c);
        }
        return 1;
    }

    size_t write(const uint8_t* buffer, size_t size) override {
        Serial.write(buffer, size);
        if (_client && _client.connected()) {
            _client.write(buffer, size);
        }
        return size;
    }

private:
    WiFiServer* _server = nullptr;
    WiFiClient  _client;
};

// 전역 인스턴스 — main.cpp에서 정의
extern RemoteSerial LOG;