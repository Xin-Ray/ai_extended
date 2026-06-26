"""把已解析的物体变成一句解释。

V0 默认走固定模板（USE_OLLAMA=0），排错优先、零依赖。
设 USE_OLLAMA=1 时改走本地 Ollama（http://localhost:11434），后端协议不变。
"""
from __future__ import annotations

import os

import httpx

from app.services.resolver import ResolvedObject

USE_OLLAMA = os.getenv("USE_OLLAMA", "0") == "1"
OLLAMA_URL = os.getenv("OLLAMA_URL", "http://localhost:11434")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "qwen3:4b")
OLLAMA_TIMEOUT = float(os.getenv("OLLAMA_TIMEOUT", "20"))


def _template_explain(obj: ResolvedObject) -> str:
    """固定模板：This is a {title}. {description}"""
    return f"This is a {obj.display_title.lower()}. {obj.description}"


async def _ollama_explain(obj: ResolvedObject) -> str:
    """调用本地 Ollama 生成一句解释；失败时回退到模板。"""
    prompt = (
        f"You are a spatial assistant in AR. The user is pointing at a {obj.label} "
        f"(known as '{obj.display_title}'). In ONE short, friendly English sentence, "
        f"tell them what it is and what it's used for. Do not add quotes or extra lines."
    )
    try:
        async with httpx.AsyncClient(timeout=OLLAMA_TIMEOUT) as client:
            resp = await client.post(
                f"{OLLAMA_URL}/api/generate",
                json={"model": OLLAMA_MODEL, "prompt": prompt, "stream": False},
            )
            resp.raise_for_status()
            text = (resp.json().get("response") or "").strip()
            return text or _template_explain(obj)
    except Exception:
        # Ollama 没起 / 模型没拉 / 超时 —— V0 不让它阻断闭环
        return _template_explain(obj)


async def explain(obj: ResolvedObject) -> str:
    if USE_OLLAMA:
        return await _ollama_explain(obj)
    return _template_explain(obj)
