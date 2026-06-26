# 安装与上手（你这台机器的实际清单）

> 本文档基于对当前开发机的实测结果。✅ = 已就绪，❌ = 需要你装。

## 0. 当前机器状态（已检测）

| 组件 | 状态 | 备注 |
|---|---|---|
| git / Python 3.10(anaconda) / Node / dotnet / Java / VS Code | ✅ | 后端可直接用 |
| GPU | ✅ RTX 4090 24GB | 本地模型可上大的 |
| Unity Hub | ✅ | 已装 |
| Unity 编辑器 6.3 LTS | ❌ | 当前只有 2022.3.1f1，**需装 6.3 LTS** |
| Unity Android Build Support | ❌ | 装编辑器时一起勾 |
| Meta Quest Developer Hub (MQDH) | ❌ | 需装 |
| ollama | ❌ | V0 不必装，接真 LLM 时再装 |
| adb | ❌ | 随 Android 模块/平台工具安装 |

---

## 1. 后端（可立即跑通，无需设备）

```powershell
cd E:\AI_EXTENDED\quest-spatial-assistant\backend
python -m pip install -r requirements.txt
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000
```

`--host 0.0.0.0` 让同内网的 Quest 头显能连。验证：

```powershell
curl http://localhost:8000/health        # {"status":"ok"}
python ..\tools\mock_client\send_pointing.py cup_01 laptop_01 plant_01
```

> ✅ 已在本机验证通过：三个物体返回正确解释，未知 id 优雅报错不断连。

查开发机内网 IP（填到 Unity `BackendClient.serverUrl`）：
```powershell
ipconfig | findstr IPv4
```
得到形如 `192.168.x.x`，Unity 里填 `ws://192.168.x.x:8000/ws`。

---

## 2. 你要装的东西

### 2.1 Unity 6.3 LTS + Android 模块
1. 打开 **Unity Hub → Installs → Install Editor → 选 6.3 LTS**。
2. 勾选模块：**Android Build Support**（含 **Android SDK & NDK Tools** 和 **OpenJDK**）。
3. 装好后新建项目：模板 **Universal 3D (URP)**，平台切 **Android**，命名 `QuestSpatialAssistant`，路径指向本仓库的 `quest-client-unity/`（或新建后把 `Assets/Scripts` 拷进去）。**不要用 HDRP。**

### 2.2 Meta Quest Developer Hub (MQDH)
- 从 Meta 开发者站点下载安装，用于设备管理、ADB、日志、把 apk 推到 Quest 2。
- 在 Quest 2 上开启 **开发者模式**（需在 Meta 开发者后台建一个组织）。

### 2.3 Meta XR SDK（在 Unity 项目里）
- 通过 Package Manager 或 Asset Store 导入 **Meta XR Core SDK** 与 **Meta XR Interaction SDK**。
- 开启 **Hand Tracking**（OVRManager / Meta XR 设置里把 Hand Tracking Support 设为 "Hands Only" 或 "Controllers and Hands"）。

---

## 3. 在 Unity 里组装 V0（脚本已备好）

脚本位于 `quest-client-unity/Assets/Scripts/`，命名空间 `QuestSpatialAssistant.*`。组装步骤：

1. **场景里建一个空物体 `Backend`**，挂 `BackendClient`，把 `serverUrl` 填成 `ws://<开发机IP>:8000/ws`。
2. **建 UI**：一块世界空间 Canvas + 两个 TextMeshPro 文本（标题/正文），挂 `ResponseDisplay`，把 `titleText`/`bodyText` 拖进去，`backendClient` 指向上面的 Backend。
3. **建 `TtsPlayer`**（挂在任意常驻物体上），拖到 `ResponseDisplay.tts`。
4. **摆 5 个物体**（Cup / Laptop / Bottle / Plant / Monitor）：每个加 **Collider**，挂 `ObjectMetadata`，填：
   - `cup_01` / cup，`laptop_01` / laptop，`bottle_01` / bottle，`plant_01` / plant，`monitor_01` / monitor
   - **targetId 必须与后端 resolver 一致**，否则后端会回 error。
5. **手部指向**：在右手食指指尖（Meta XR 的 Hand_IndexTip 骨骼 Transform）上或一个空物体上，挂 `IndexFingerRaycaster`：
   - `fingerTip` = 食指指尖 Transform
   - `backendClient` = Backend
   - `display` = ResponseDisplay
6. **Editor 里先验证**：把 `fingerTip` 临时指向一个空物体，旋转它对准某个物体，停留 1 秒，Console 应打印 `pointing confirmed: cup_01`，UI 显示解释，`[TtsPlayer:editor] would speak ...`。
7. **真机**：File → Build Settings → Android → Build and Run，用 MQDH/adb 推到 Quest 2，戴上头显测试。

---

## 4.（后续）接本地 LLM
```powershell
# 装 ollama 后
ollama pull qwen3:4b          # 4090 也可 qwen3:14b / qwen3:32b
# 起后端时切到 Ollama：
$env:USE_OLLAMA=1; $env:OLLAMA_MODEL="qwen3:14b"
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000
```
协议与客户端无需改动。
