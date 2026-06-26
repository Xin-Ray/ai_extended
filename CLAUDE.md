# CLAUDE.md — Quest Spatial Assistant

给在本仓库工作的 AI 助手的项目指引。

## 项目是什么

XR 空间助手：戴 Quest 头显，用食指指向物体，AI 解释它是什么并语音播报。分阶段构建，**当前在 V0**。

核心原则：**先把系统链路跑通，再逐层替换输入**。不要为了追求"真实感"提前引入 Quest 3 相机 / YOLO / Gaussian Splatting，那些是后续版本。每一版只替换前半段输入，后端协议 / LLM / TTS / 对话状态保持可复用。

## 架构（两半）

```
[Quest 客户端 / Unity + Meta XR]  --WebSocket JSON-->  [后端 / FastAPI + Python]
 手部追踪 → 食指射线 → 命中虚拟物体                      Target Resolver → LLM/模板 → TTS 提示
 显示文字 + Android TTS 播放        <--assistant_response--
```

- 前半段（Unity/设备）：在 `quest-client-unity/`，需在 Unity 编辑器和真机上操作。
- 后半段（后端）：在 `backend/`，纯 Python，可无设备本地跑通验证。

## 关键约定

- **协议是契约**：`shared/schemas/` 里的 `pointing_event` / `assistant_response` 是前后端唯一接口。改字段要同时改两端，并在 `docs/protocol.md` 记版本。Quest 3 的相机字段是 **可选扩展**，不要改动 V0 已有字段。
- **后端 LLM 开关**：`backend/app/services/explain.py` 用环境变量 `USE_OLLAMA`（默认 `0`）切换"固定模板"与"本地 Ollama"。V0 默认模板，排错优先。Ollama 地址 `http://localhost:11434`。
- **物体注册表**：V0 的虚拟物体在 `backend/app/services/resolver.py` 内置（cup_01 等），与 Unity 里挂的 `ObjectMetadata.targetId` 必须一一对应。
- **本地模型主机**：开发/推理主机是 RTX 4090 24GB，本地模型可上 qwen3:14b/32b、qwen2.5vl:7b，不受 8GB 限制。

## 验证方式

后端半条链路无需任何设备即可验证：
```bash
cd backend && uvicorn app.main:app --reload
curl http://localhost:8000/health                  # {"status":"ok"}
python ../tools/mock_client/send_pointing.py        # Assistant: This is a blue ceramic mug. ...
```
设备端验收指标见 `docs/mvp_definition.md`。

## 不要做（V0 范围外）

Quest 3 RGB 相机 / 真实 YOLO / Gaussian Splatting / 房间扫描 / 真实空间锚定 / 复杂 Avatar IK / 连续语音对话。需要时按 `docs/mvp_definition.md` 的版本路线推进。

## 环境备注

- 平台 Windows，shell 优先 PowerShell。
- Python 走 anaconda（base 为 3.10）。后端依赖见 `backend/requirements.txt`。
- Unity 目标版本 6.3 LTS + Android Build Support；客户端用 URP，不用 HDRP。
