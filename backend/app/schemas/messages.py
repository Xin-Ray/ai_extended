"""与 shared/schemas 对齐的 Pydantic 模型。

协议契约见 ../../../shared/schemas/ 与 docs/protocol.md。
camera_* 字段是 Quest 3 预留的可选扩展，V0 不使用。
"""
from __future__ import annotations

from typing import Any, Literal, Optional

from pydantic import BaseModel, Field


class PointingEvent(BaseModel):
    type: Literal["pointing_event"] = "pointing_event"
    session_id: str
    timestamp_ms: int
    target_id: str
    target_label: Optional[str] = None
    ray_origin: Optional[list[float]] = None
    ray_direction: Optional[list[float]] = None

    # —— Quest 3 预留（可选扩展，V0 不用）——
    camera_frame_id: Optional[str] = None
    camera_intrinsics: Optional[dict[str, Any]] = None
    camera_pose: Optional[dict[str, Any]] = None
    image_width: Optional[int] = None
    image_height: Optional[int] = None


class AssistantResponse(BaseModel):
    type: Literal["assistant_response"] = "assistant_response"
    target_id: str
    speech: str
    display_title: Optional[str] = None
    display_subtitle: Optional[str] = None
    avatar_intent: Literal["none", "look_and_point", "look"] = "look_and_point"
    tts_mode: Literal["local", "server"] = "local"


class ErrorResponse(BaseModel):
    type: Literal["error"] = "error"
    message: str
    target_id: Optional[str] = None
