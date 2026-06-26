"""V0 虚拟物体注册表：从 shared/objects.json 种子文件加载。

target_id 必须与 Unity 里挂的 ObjectMetadata.targetId 一一对应。
V1（Quest 3 + YOLO）会用真实检测结果替换这一步，但 resolver 的输出契约不变。
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Optional


@dataclass(frozen=True)
class ResolvedObject:
    target_id: str
    label: str
    display_title: str
    short_explanation: str
    tts_text: str
    display_subtitle: str = "Virtual object"
    scene_location: str = ""

    @property
    def description(self) -> str:
        """向后兼容旧字段名。"""
        return self.short_explanation


# 种子文件：仓库根的 shared/objects.json（相对本文件 ../../../../shared/objects.json）
_SEED_PATH = Path(__file__).resolve().parents[3] / "shared" / "objects.json"


def _load_registry() -> dict[str, ResolvedObject]:
    with _SEED_PATH.open("r", encoding="utf-8") as f:
        data = json.load(f)
    registry: dict[str, ResolvedObject] = {}
    for o in data.get("objects", []):
        obj = ResolvedObject(
            target_id=o["target_id"],
            label=o["label"],
            display_title=o["display_title"],
            short_explanation=o.get("short_explanation", ""),
            tts_text=o.get("tts_text", ""),
            display_subtitle=o.get("display_subtitle", "Virtual object"),
            scene_location=o.get("scene_location", ""),
        )
        registry[obj.target_id] = obj
    return registry


_REGISTRY: dict[str, ResolvedObject] = _load_registry()


def resolve(target_id: str) -> Optional[ResolvedObject]:
    """按 target_id 查物体；找不到返回 None。"""
    return _REGISTRY.get(target_id)


def all_target_ids() -> list[str]:
    return list(_REGISTRY.keys())
