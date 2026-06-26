# MVP 定义与版本路线

## V0 成功标准（一句话）

> 戴 Quest 2，用食指指向 Unity 里的杯子，后端收到 target_id，Quest 显示并说出对这个杯子的解释。

## V0 包含

```
手指射线 + 虚拟物体 + WebSocket + LLM/模板 + TTS
```

## V0 不做

Quest 3 RGB 相机 / 真实 YOLO / Gaussian Splatting / 房间扫描 / 真实空间锚定 / 复杂 Avatar IK / 连续语音对话。
（协议为这些预留扩展位，但不实现。）

## V0 验收指标

| 指标 | 标准 | 由谁验 |
|---|---|---|
| 后端 health | `/health` 返回 `{"status":"ok"}` | ✅ 已验（本机） |
| 后端闭环 | mock client 发 target_id 得到正确解释，未知 id 不崩 | ✅ 已验（本机） |
| Quest 2 手部追踪 | 稳定识别右手食指 | 设备端 |
| 目标命中 | 连续指向杯子 ≥1s，正确选中率 ≥90% | 设备端 |
| 高亮反馈 | 命中后 300ms 内出现视觉反馈 | 设备端 |
| 后端通信 | WebSocket 持续连接、断开可重连 | 设备端 |
| 响应延迟 | 指向到文字显示 < 1 秒 | 设备端 |
| 语音播放 | 指向后完整播出一句话 | 设备端 |
| Demo 物体数量 | ≥ 5 个 | 设备端 |
| 完整闭环 | 连续演示 5 次不崩溃 | 设备端 |

## 版本路线

| 版本 | 输入/能力 | 关键新增 |
|---|---|---|
| **V0** | Quest 2 + 虚拟物体指向 | 食指射线 / WebSocket / 模板或LLM / TTS |
| **V1** | Quest 3 真实物体识别 | RGB 相机 + YOLO + 射线投影 + box/mask 匹配 |
| **V2** | 空间记忆 | object registry / room id / 锚点 / "刚才那个杯子在哪" |
| **V3** | Gaussian Splatting 房间模型 | 多视角采集 + camera pose + gsplat/Nerfstudio |
| **V4** | AI 人物 | Avatar gaze / look-at / point-at / lip sync |
| **V5** | 连续自然对话 | 语音输入 + 对话记忆 + 工具调用 + 物体/房间问答 |
