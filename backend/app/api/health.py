"""健康检查端点。返回状态、版本与时间（清单 Gate 3.1 要求）。"""
import time

from fastapi import APIRouter

router = APIRouter()

VERSION = "0.1.0"


@router.get("/health")
async def health() -> dict:
    return {
        "status": "ok",
        "version": VERSION,
        "timestamp_ms": int(time.time() * 1000),
    }
