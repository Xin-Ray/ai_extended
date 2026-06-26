# 架构

## 总览

```
                         ┌──────────────────────────────┐
                         │        Quest Client          │
                         │      Unity + Meta XR          │
                         │                              │
                         │  Hand Tracking               │
                         │  Index Finger Ray            │
                         │  Virtual Object Selection    │
                         │  UI / TTS  (Avatar later)    │
                         └──────────────┬───────────────┘
                                        │  WebSocket JSON
                                        ▼
┌────────────────────────────────────────────────────────────────┐
│                         AI Backend (FastAPI)                     │
│  ┌────────────────┐  ┌──────────────────┐  ┌────────────────┐  │
│  │ Target Resolver│  │ Explanation      │  │ (TTS 提示)     │  │
│  │ target_id→obj  │  │ 模板 / Ollama    │  │ tts_mode=local │  │
│  └────────────────┘  └──────────────────┘  └────────────────┘  │
│            Later: YOLO / VLM / GS / Spatial Memory              │
└────────────────────────────────────────────────────────────────┘
```

## V0 数据流

1. Unity 手部追踪得到右手食指指尖 Transform。
2. `IndexFingerRaycaster` 沿食指 forward 做 `Physics.Raycast`，命中带 `ObjectMetadata` 的 Collider。
3. 连续停留 ≥1s 去抖后，`BackendClient` 经 WebSocket 发 `pointing_event{target_id}`。
4. 后端 `resolver.resolve(target_id)` 查注册表 → `explain()` 生成一句话（V0 模板，可切 Ollama）→ 回 `assistant_response`。
5. `ResponseDisplay` 更新 UI 文本并调 `TtsPlayer.Speak()` 播报。

## 模块边界（关键：换输入不动后端）

| 层 | V0 | V1（Quest 3） |
|---|---|---|
| 输入 | 食指射线命中虚拟 Collider | 食指射线 + RGB 帧 → YOLO → box/mask 匹配 |
| 协议 | `pointing_event{target_id}` | 同左 + 可选 `camera_*` 字段 |
| 后端 | resolver（内置注册表）+ explain | resolver 改为读真实检测结果，explain 不变 |
| 客户端回放 | UI + TTS | 不变 |

后端协议、explain、TTS、UI 在各版本间保持复用；每版只替换"输入如何变成 target"。

## 后端代码地图

```
backend/app/
├── main.py            # FastAPI 装配，挂 health + ws 路由，开 CORS
├── api/health.py      # GET /health
├── api/ws.py          # WS /ws：收 pointing_event → handle_pointing → assistant_response
├── schemas/messages.py# Pydantic：PointingEvent / AssistantResponse / ErrorResponse
└── services/
    ├── resolver.py    # target_id → ResolvedObject（V0 内置 5 物体注册表）
    └── explain.py     # ResolvedObject → 一句解释（模板 / Ollama，USE_OLLAMA 开关）
```
