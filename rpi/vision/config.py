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

# ── 근접 촬영 파라미터 ───────────────────────────────────────────────────────
# AfMode: 0=수동, 1=연속AF, 2=단일AF
AF_MODE      = 0               # Pi Camera v2는 고정 초점 렌즈 → 수동(0)
# LensPosition: 0.0(무한원) ~ 10.0(최근접 ~10cm)
# 소형 부품 근접 촬영 권장 거리: 약 20~30cm → LensPosition ≈ 3.0~5.0
# 실측 후 아래 값 조정
LENS_POSITION = 4.0

# ── 노출 / 화이트밸런스 ──────────────────────────────────────────────────────
# 실내 LED 조명 기준. 변경 필요 시 환경에 맞게 조정
AWB_MODE     = "indoor"        # auto / indoor / daylight / tungsten / fluorescent
EXPOSURE_TIME = 20000          # 단위: microseconds (20ms → 밝은 실내 기준)
ANALOGUE_GAIN = 1.5            # ISO 유사 값 (1.0~8.0). 높을수록 밝지만 노이즈↑

# ── 프리뷰 ───────────────────────────────────────────────────────────────────
PREVIEW_ENABLED = False        # 헤드리스 RPi에서는 False 유지
