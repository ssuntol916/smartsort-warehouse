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
    logger.info("LensPosition: %.1f", config.LENS_POSITION)
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
    """연속 촬영 테스트 (파라미터 비교용)"""
    logger.info("=" * 50)
    logger.info("테스트: 연속 %d회 촬영", count)
    logger.info("=" * 50)

    lens_positions = [2.0, 4.0, 6.0]  # 근접 거리 비교: 멀리 / 중간 / 가깝게

    for i, lp in enumerate(lens_positions[:count], 1):
        config.LENS_POSITION = lp
        logger.info("[%d/%d] LensPosition=%.1f 촬영 중...", i, count, lp)
        try:
            path = capture.capture_image(prefix=f"lens_{lp:.1f}")
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
        help="single: 단일 촬영 / multi: LensPosition 비교 촬영",
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
    parser.add_argument(
        "--lens",
        type=float,
        default=config.LENS_POSITION,
        help="LensPosition (0.0=무한원 ~ 10.0=최근접)",
    )
    args = parser.parse_args()

    # 매개변수를 config에 반영
    config.CAPTURE_WIDTH  = args.width
    config.CAPTURE_HEIGHT = args.height
    config.LENS_POSITION  = args.lens

    if args.mode == "single":
        test_single_capture()
    else:
        test_multi_capture()
