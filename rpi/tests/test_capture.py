"""
// ============================================================
// 파일명  : test_capture.py
// 역할    : Pi Camera v2 촬영 테스트
// 작성자  : 송준호
// 작성일  : 2026-04-03
// ============================================================
"""

import sys
import logging
import os
from pathlib import Path

# 프로젝트 루트를 sys.path에 추가 (단독 실행 시)
PROJECT_ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(PROJECT_ROOT))

from rpi.vision import capture, config

# ── 로깅 설정 ──────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger("test_capture")


def test_single_capture():
    """정지 이미지 1장 촬영 후 파일 저장 확인"""
    logger.info("=" * 50)
    logger.info("테스트: 정지 이미지 촬영")
    logger.info("해상도: %dx%d", config.CAPTURE_WIDTH, config.CAPTURE_HEIGHT)
    logger.info("=" * 50)

    try:
        saved_path = capture.capture_image(prefix="test")
        file_size  = os.path.getsize(saved_path) / 1024  # KB

        logger.info("✅ 촬영 성공!")
        logger.info("   저장 경로 : %s", saved_path)
        logger.info("   파일 크기 : %.1f KB", file_size)
        return saved_path

    except Exception as e:
        logger.error("❌ 촬영 실패: %s", e)
        raise


def test_multi_capture(count: int = 3):
    """연속 촬영 테스트 (노출값 비교용)"""
    logger.info("=" * 50)
    logger.info("테스트: 연속 %d회 촬영 (노출값 비교)", count)
    logger.info("=" * 50)

    # IMX219는 고정 초점 → LensPosition 대신 ExposureTime으로 비교
    exposure_times = [10000, 20000, 40000]  # 10ms / 20ms / 40ms

    for i, et in enumerate(exposure_times[:count], 1):
        config.EXPOSURE_TIME = et
        logger.info("[%d/%d] ExposureTime=%dμs 촬영 중...", i, count, et)
        try:
            path = capture.capture_image(prefix=f"exp_{et}")
            size = os.path.getsize(path) / 1024
            logger.info("   → 저장: %s (%.1f KB)", Path(path).name, size)
        except Exception as e:
            logger.error("   → 실패: %s", e)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Pi Camera v2 촬영 테스트",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument(
        "--mode",
        choices=["single", "multi"],
        default="single",
        help="single: 단일 촬영 / multi: 노출값 비교 촬영",
    )
    parser.add_argument(
        "--width",
        type=int,
        default=config.CAPTURE_WIDTH,
        help="촬영 해상도 - 가로 (px)",
    )
    parser.add_argument(
        "--height",
        type=int,
        default=config.CAPTURE_HEIGHT,
        help="촬영 해상도 - 세로 (px)",
    )
    args = parser.parse_args()

    # 매개변수를 config에 반영
    config.CAPTURE_WIDTH  = args.width
    config.CAPTURE_HEIGHT = args.height

    if args.mode == "single":
        test_single_capture()
    else:
        test_multi_capture()