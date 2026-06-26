# 通信协议（V0）

前后端唯一接口。改字段必须同时改两端，并在此处记版本。JSON Schema 见 [`../shared/schemas/`](../shared/schemas/)。

- **传输**：WebSocket，`ws://<后端IP>:8000/ws`
- **编码**：UTF-8 JSON，一条消息一个对象
- **版本**：v0.1
- **方向**：客户端发 `pointing_event`，后端回 `assistant_response`

---

## 客户端 → 后端：`pointing_event`

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | `"pointing_event"` | ✅ | 消息类型常量 |
| `session_id` | string | ✅ | 会话标识，如 `demo_001` |
| `timestamp_ms` | int | ✅ | 客户端 Unix 毫秒时间戳 |
| `target_id` | string | ✅ | 命中物体 id，对应后端注册表 / Unity `ObjectMetadata.targetId`，如 `cup_01` |
| `target_label` | string | — | 客户端已知标签（后端以注册表为准） |
| `ray_origin` | `[x,y,z]` | — | 食指射线起点（世界坐标，米） |
| `ray_direction` | `[x,y,z]` | — | 食指射线方向（单位向量） |

示例：
```json
{
  "type": "pointing_event",
  "session_id": "demo_001",
  "timestamp_ms": 1710000000000,
  "target_id": "cup_01",
  "target_label": "cup",
  "ray_origin": [0.12, 1.35, -0.22],
  "ray_direction": [0.15, -0.08, 0.98]
}
```

---

## 后端 → 客户端：`assistant_response`

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | `"assistant_response"` | ✅ | 消息类型常量 |
| `target_id` | string | ✅ | 回显请求的 target_id |
| `speech` | string | ✅ | TTS 播报的完整句子 |
| `display_title` | string | — | UI 主标题 |
| `display_subtitle` | string | — | UI 副标题 |
| `avatar_intent` | `none`/`look_and_point`/`look` | — | Avatar 动作意图（V0 忽略） |
| `tts_mode` | `local`/`server` | — | TTS 播放位置，V0 用 `local` |

示例：
```json
{
  "type": "assistant_response",
  "target_id": "cup_01",
  "speech": "This is a blue ceramic mug. It is used for hot drinks.",
  "display_title": "Blue ceramic mug",
  "display_subtitle": "Virtual desk object",
  "avatar_intent": "look_and_point",
  "tts_mode": "local"
}
```

---

## 未来扩展（不推翻 V0）

Quest 3 接真实相机时，`pointing_event` **新增可选字段**，不改已有字段：

```json
{
  "camera_frame_id": "frame_003021",
  "camera_intrinsics": {},
  "camera_pose": {},
  "image_width": 1280,
  "image_height": 720
}
```

后端据此判断走"虚拟物体直选"（V0）还是"图像 + YOLO + 射线投影匹配"（V1）。协议版本随字段变更递增。
