"""
// ============================================================
// 파일명  : capture.py
// 역할    : Pi Camera v2 캡처 모듈
// 작성자  : 송준호
// 작성일  : 2026-04-03
// ============================================================
"""

import os
import time
import logging
from datetime import datetime
from pathlib import Path

from picamera2 import Picamera2
from picamera2.encoders import JpegEncoder
from libcamera import controls

from . import config

logger = logging.getLogger(__name__)


def _ensure_image_dir() -> Path:
    """이미지 저장 디렉토리 생성 (없을 경우)"""
    image_dir = Path(config.IMAGE_DIR)
    image_dir.mkdir(parents=True, exist_ok=True)
    return image_dir


def _build_filename(prefix: str = "capture") -> str:
    """타임스탬프 기반 파일명 생성 예: capture_20260403_153012.jpg"""
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    return f"{prefix}_{ts}.{config.IMAGE_FORMAT}"


def capture_image(filename: str | None = None, prefix: str = "capture") -> str:
    """
    Pi Camera v2로 정지 이미지 1장을 촬영하고 저장한다.

    Args:
        filename: 저장 파일명. None이면 타임스탬프 자동 생성.
        prefix:   자동 파일명 접두사 (기본: 'capture')

    Returns:
        저장된 이미지의 절대 경로 문자열
    """
    image_dir  = _ensure_image_dir()
    filename   = filename or _build_filename(prefix)
    save_path  = str(image_dir / filename)

    logger.info("카메라 초기화 중...")
    cam = Picamera2()

    try:
        # 스틸 캡처 설정
        still_cfg = cam.create_still_configuration(
            main={"size": (config.CAPTURE_WIDTH, config.CAPTURE_HEIGHT),
                  "format": "RGB888"},
            buffer_count=1,
        )
        cam.configure(still_cfg)

        # 근접 촬영 파라미터 적용
        cam.set_controls({
            "AfMode":       config.AF_MODE,
            "LensPosition": config.LENS_POSITION,
            "AwbMode":      controls.AwbModeEnum.__members__.get(
                                config.AWB_MODE.capitalize(), controls.AwbModeEnum.Indoor
                            ),
            "ExposureTime": config.EXPOSURE_TIME,
            "AnalogueGain": config.ANALOGUE_GAIN,
        })

        cam.start()
        logger.info("카메라 시작, 안정화 대기 (2초)...")
        time.sleep(2)  # AEC/AWB 안정화

        cam.capture_file(save_path)
        logger.info("이미지 저장 완료: %s", save_path)

    finally:
        cam.stop()
        cam.close()

    return save_path
