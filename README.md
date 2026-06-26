# Quest Spatial Assistant

戴 Quest 头显，用食指指向物体，AI 解释它是什么并语音播报。

这是一个分阶段构建的 XR 空间助手。当前目标是 **V0 最小闭环**：在 Unity 虚拟房间里用 Quest 2 手部追踪指向虚拟物体，后端生成一句解释，头显显示并 TTS 播放。

## 当前版本：V0（Quest 2 + 虚拟物体）

```
Quest 2 手部追踪
→ 食指射线命中 Unity 虚拟物体
→ target_id 经 WebSocket 发后端
→ 后端生成解释（先模板，后接本地 LLM）
→ Unity 显示文字 + Android TTS 播放
```

V0 **不做**：Quest 3 真实相机、YOLO、Gaussian Splatting、房间扫描、空间锚定、复杂 Avatar、连续语音对话。协议为这些预留了扩展位，但不实现。

## 仓库结构

```
quest-spatial-assistant/
├── quest-client-unity/   # Unity 客户端（你在 Unity 里搭，脚本已备好）
├── backend/              # FastAPI 后端（health + WebSocket + 解释服务）
├── shared/               # 协议 schema 与 prompt 模板
├── tools/mock_client/    # 无设备验证后端闭环的 Python 客户端
└── docs/                 # 架构 / 协议 / MVP定义 / 安装指引
```

## 快速验证后端（无需 Quest / Unity）

```bash
cd backend
pip install -r requirements.txt
uvicorn app.main:app --reload

# 另开一个终端：
curl http://localhost:8000/health          # → {"status":"ok"}
python ../tools/mock_client/send_pointing.py  # → Assistant: This is a blue ceramic mug. ...
```

详细安装与设备端步骤见 [`docs/setup.md`](docs/setup.md)。版本路线见 [`docs/mvp_definition.md`](docs/mvp_definition.md)。
