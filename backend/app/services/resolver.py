"""V0 虚拟物体注册表：target_id -> 标签 / 描述 / 显示文案。

这些 id 必须与 Unity 里挂的 ObjectMetadata.targetId 一一对应。
V1（Quest 3 + YOLO）会用真实检测结果替换这一步，但 resolver 的输出契约不变。
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Optional


@dataclass(frozen=True)
class ResolvedObject:
    target_id: str
    label: str
    description: str
    display_title: str
    display_subtitle: str = "Virtual object"


# V0 内置的 5 个演示物体
_REGISTRY: dict[str, ResolvedObject] = {
    "cup_01": ResolvedObject(
        target_id="cup_01",
        label="cup",
        description="It is usually used for hot drinks.",
        display_title="Blue ceramic mug",
        display_subtitle="Virtual desk object",
    ),
    "laptop_01": ResolvedObject(
        target_id="laptop_01",
        label="laptop",
        description="It is used for work, coding, and browsing the web.",
        display_title="Laptop computer",
        display_subtitle="Virtual desk object",
    ),
    "bottle_01": ResolvedObject(
        target_id="bottle_01",
        label="bottle",
        description="It holds water to keep you hydrated.",
        display_title="Water bottle",
        display_subtitle="Virtual desk object",
    ),
    "plant_01": ResolvedObject(
        target_id="plant_01",
        label="plant",
        description="It is a decorative plant that brightens the room.",
        display_title="Potted plant",
        display_subtitle="Virtual floor object",
    ),
    "monitor_01": ResolvedObject(
        target_id="monitor_01",
        label="monitor",
        description="It displays the image from your computer.",
        display_title="Computer monitor",
        display_subtitle="Virtual desk object",
    ),
}


def resolve(target_id: str) -> Optional[ResolvedObject]:
    """按 target_id 查物体；找不到返回 None。"""
    return _REGISTRY.get(target_id)


def all_target_ids() -> list[str]:
    return list(_REGISTRY.keys())
