# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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

- 前半段（Unity/设备）：Unity 工程在 `quest-client-unity/ai_1/`，需在 Unity 编辑器和真机上操作。
- 后半段（后端）：在 `backend/`，纯 Python，可无设备本地跑通验证。

代码落点速查：
- `backend/app/main.py` — FastAPI 入口，挂 `health` 与 `ws` 两个 router；`/`、`/health` 为 HTTP，`/ws` 为 WebSocket。
- `backend/app/api/ws.py` — 唯一业务链路：收 `pointing_event` → `resolver.resolve` → `explain` → 回 `assistant_response`；未知 target_id 回 `error` 但不断连。
- `backend/app/services/resolver.py` — 从 `shared/objects.json` 加载物体注册表（**不是内置**），按 `target_id` 查 `ResolvedObject`。
- `backend/app/services/explain.py` — `USE_OLLAMA` 切模板/本地 Ollama；Ollama 失败一律回退模板，不阻断闭环。
- `backend/app/schemas/messages.py` — Pydantic 模型，必须与 `shared/schemas/` 和 `docs/protocol.md` 对齐。
- `tools/mock_client/send_pointing.py` — 无设备跑通后端的客户端。

## 关键约定

- **不要随意改 Unity 的 `.unity`（场景）/ `.prefab`（预制体）/ Animator(`.controller`/`.anim`) 文件**。这些是二进制/序列化资产，AI 直接编辑极易损坏且 diff 不可读。需要改场景/预制体时，给出在 Unity 编辑器里的操作步骤，由人来做。脚本（`.cs`）可以正常编辑。
- **协议是契约**：`shared/schemas/` 里的 `pointing_event` / `assistant_response` 是前后端唯一接口（回包的播报文本字段名是 `speech`）。物体目录的单一事实来源是 `shared/objects.json`（后端 `resolver.py` 读取它）。改字段要同时改三处：`shared/schemas/`、`backend/app/schemas/messages.py`、`docs/protocol.md` 记版本。Quest 3 的相机字段（`camera_*` / `image_*`）是 **可选扩展**，不要改动 V0 已有字段。
- **后端 LLM 开关**：`backend/app/services/explain.py` 用环境变量 `USE_OLLAMA`（默认 `0`）切换"固定模板"与"本地 Ollama"。V0 默认模板，排错优先。相关变量：`OLLAMA_URL`（默认 `http://localhost:11434`）、`OLLAMA_MODEL`（代码默认 `qwen3:4b`）、`OLLAMA_KEEP_ALIVE`、`OLLAMA_TIMEOUT`；qwen3 走 `think:false` 关思考保实时。配置样例见 `backend/.env.example`。
- **物体注册表**：V0 的虚拟物体定义在 `shared/objects.json`，由 `resolver.py` 加载（cup_01 / laptop_01 / bottle_01 / plant_01 / monitor_01）。每个 `target_id` 必须与 Unity 里挂的 `ObjectMetadata.targetId` 逐字一致。加物体只改这个 JSON，两端无需改代码。
- **本地模型主机**：开发/推理主机是 RTX 4090 24GB，本地模型可上 qwen3:14b/32b、qwen2.5vl:7b，不受 8GB 限制。

## 常用命令

后端（PowerShell，仓库根目录起）：
```powershell
cd backend
pip install -r requirements.txt
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000   # --host 0.0.0.0 让 Quest 同内网连得上
```
本地无设备验证后端闭环（另开一个终端）：
```powershell
curl http://localhost:8000/health                          # {"status":"ok","version":...}
python tools/mock_client/send_pointing.py                  # 默认发 cup_01
python tools/mock_client/send_pointing.py laptop_01 plant_01
python tools/mock_client/send_pointing.py --url ws://192.168.1.20:8000/ws cup_01   # 指向真机/远程后端
```
切到本地 Ollama 跑真实 LLM：先 `$env:USE_OLLAMA="1"` 再起 uvicorn（确保 `ollama serve` 已起且模型已 `ollama pull`）。

测试：当前仓库**没有自动化测试**，验证靠上面的 mock client + `/health`。设备端验收指标见 `docs/mvp_definition.md`。

## Unity 客户端现状（重要）

`quest-client-unity/ai_1/` 是 **Unity 6000.5.1f1 的全新 URP 工程**，目前还**不是**可用客户端：

- **Meta XR / Oculus SDK 尚未安装**（`Packages/manifest.json` 里只有基础 `com.unity.modules.xr`，没有 Meta XR All-in-One / OpenXR）。要做手部追踪指向，得先装 Meta XR SDK 并配 Android/XR 项目设置。
- **本项目的助手脚本尚未就位**：早先 `quest-client-unity/Assets/Scripts/` 下的 `BackendClient` / `IndexFingerRaycaster` / `ObjectMetadata` / `TtsPlayer` / `ResponseDisplay` 已在 git 中删除，新工程里还没重建。需要时按协议（`docs/protocol.md`）重新实现，挂到 `ai_1` 工程。
- 工程装了 **Unity MCP**（`com.coplaydev.unity-mcp`），可用 `mcp__UnityMCP__*` 工具直接读控制台 / 建脚本 / 查场景；改完脚本用 `read_console` 确认编译通过再继续。
- 默认 SampleScene 仍是 Unity 模板内容（`TutorialInfo` 等），可清理。

## 不要做（V0 范围外）

Quest 3 RGB 相机 / 真实 YOLO / Gaussian Splatting / 房间扫描 / 真实空间锚定 / 复杂 Avatar IK / 连续语音对话。需要时按 `docs/mvp_definition.md` 的版本路线推进。

## 环境备注

- 平台 Windows，shell 优先 PowerShell。
- Python 走 anaconda（base 为 3.10）。后端依赖见 `backend/requirements.txt`。
- Unity 目标版本 6.3 LTS + Android Build Support；客户端用 URP，不用 HDRP。
