"""
// ============================================================
// 파일명  : capture.py
// 역할    : Pi Camera v2 설정 모듈
// 작성자  : 송준호
// 작성일  : 2026-04-03
// ============================================================
"""

# ── 촬영 해상도 ──────────────────────────────────────────────────────────────
# Pi Camera v2 최대 해상도: 3280 x 2464
# 소형 부품 근접 촬영 시 과도한 해상도는 처리 지연을 유발하므로
# Gemini Vision API 입력에 최적화된 1920x1080 사용
CAPTURE_WIDTH  = 1920
CAPTURE_HEIGHT = 1080

# ── 정지 이미지 저장 경로 ────────────────────────────────────────────────────
import os
BASE_DIR    = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMAGE_DIR   = os.path.join(BASE_DIR, "images")
IMAGE_FORMAT = "jpeg"          # jpeg / png
JPEG_QUALITY = 92              # 0~100 (92 = 고품질 + 적절한 용량)

# ── 노출 / 화이트밸런스 ──────────────────────────────────────────────────────
# 실내 LED 조명 기준. 변경 필요 시 환경에 맞게 조정
AWB_MODE     = "indoor"        # auto / indoor / daylight / tungsten / fluorescent
EXPOSURE_TIME = 20000          # 단위: microseconds (20ms → 밝은 실내 기준)
ANALOGUE_GAIN = 1.5            # ISO 유사 값 (1.0~8.0). 높을수록 밝�