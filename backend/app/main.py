"""FastAPI 应用入口。

启动：
    cd backend
    uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

--host 0.0.0.0 让同内网的 Quest 头显能连上开发机。
"""
import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api import health, ws

logging.basicConfig(level=logging.INFO)

app = FastAPI(title="Quest Spatial Assistant Backend", version="0.1.0")

# 开发期放开 CORS，方便本地工具/浏览器联调
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(health.router)
app.include_router(ws.router)


@app.get("/")
async def root() -> dict[str, str]:
    return {"service": "quest-spatial-assistant", "version": "0.1.0"}
