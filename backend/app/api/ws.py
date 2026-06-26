"""WebSocket 端点：收 pointing_event，回 assistant_response。

V0 链路：
  pointing_event -> resolver.resolve(target_id) -> explain() -> assistant_response
未知 target_id 回 error 消息但不断开连接。
"""
from __future__ import annotations

import logging

from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from pydantic import ValidationError

from app.schemas.messages import AssistantResponse, ErrorResponse, PointingEvent
from app.services.explain import explain
from app.services.resolver import resolve

logger = logging.getLogger("ws")

router = APIRouter()


async def handle_pointing(event: PointingEvent) -> AssistantResponse | ErrorResponse:
    obj = resolve(event.target_id)
    if obj is None:
        return ErrorResponse(
            message=f"unknown target_id: {event.target_id}",
            target_id=event.target_id,
        )
    speech = await explain(obj)
    return AssistantResponse(
        target_id=obj.target_id,
        speech=speech,
        display_title=obj.display_title,
        display_subtitle=obj.display_subtitle,
        avatar_intent="look_and_point",
        tts_mode="local",
    )


@router.websocket("/ws")
async def ws_endpoint(websocket: WebSocket) -> None:
    await websocket.accept()
    logger.info("client connected")
    try:
        while True:
            raw = await websocket.receive_text()
            try:
                event = PointingEvent.model_validate_json(raw)
            except ValidationError as e:
                await websocket.send_text(
                    ErrorResponse(message=f"invalid pointing_event: {e}").model_dump_json()
                )
                continue

            reply = await handle_pointing(event)
            await websocket.send_text(reply.model_dump_json())
            logger.info("handled target_id=%s -> %s", event.target_id, reply.type)
    except WebSocketDisconnect:
        logger.info("client disconnected")
