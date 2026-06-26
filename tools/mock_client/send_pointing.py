"""Mock Quest 客户端：无需 Quest / Unity 验证后端闭环。

连 ws://localhost:8000/ws，发若干 pointing_event，打印后端返回的 speech。
用法：
    python tools/mock_client/send_pointing.py                 # 发默认 cup_01
    python tools/mock_client/send_pointing.py laptop_01 plant_01   # 发指定 target_id
    python tools/mock_client/send_pointing.py --url ws://192.168.1.20:8000/ws cup_01
"""
from __future__ import annotations

import argparse
import asyncio
import json
import time

import websockets

DEFAULT_URL = "ws://localhost:8000/ws"


def make_event(target_id: str) -> dict:
    return {
        "type": "pointing_event",
        "session_id": "demo_001",
        "timestamp_ms": int(time.time() * 1000),
        "target_id": target_id,
        "target_label": target_id.split("_")[0],
        "ray_origin": [0.12, 1.35, -0.22],
        "ray_direction": [0.15, -0.08, 0.98],
    }


async def run(url: str, target_ids: list[str]) -> None:
    async with websockets.connect(url) as ws:
        print(f"[connected] {url}")
        for tid in target_ids:
            await ws.send(json.dumps(make_event(tid)))
            raw = await ws.recv()
            msg = json.loads(raw)
            if msg.get("type") == "assistant_response":
                print(f"\n  Pointing at: {tid}")
                print(f"  Title:       {msg.get('display_title')}")
                print(f"  Assistant:   {msg.get('speech')}")
            else:
                print(f"\n  [{msg.get('type')}] {msg}")
        print("\n[done]")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", default=DEFAULT_URL, help="WebSocket 地址")
    parser.add_argument("target_ids", nargs="*", default=None, help="要指向的 target_id 列表")
    args = parser.parse_args()
    targets = args.target_ids or ["cup_01"]
    asyncio.run(run(args.url, targets))


if __name__ == "__main__":
    main()
