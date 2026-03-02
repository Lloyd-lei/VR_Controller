# OpenArm Teleop - Meta Quest 3 VR Teleoperation

基于 Meta Quest 3 的双手遥操作系统，通过 WebSocket 向 ROS2 发送手部位姿数据，用于控制机械臂。

## 技术栈

- **Unity 6 (URP)** + Meta XR SDK v60+
- **WebSocket** -> rosbridge_server
- **ROS2** geometry_msgs / std_msgs

---

## ROS Topics

| Topic | 类型 | 说明 |
|-------|------|------|
| `/quest3/right_hand_pose` | `geometry_msgs/PoseStamped` | 右手手腕位姿（相对 clutch anchor） |
| `/quest3/left_hand_pose` | `geometry_msgs/PoseStamped` | 左手手腕位姿 |
| `/quest3/right_gripper` | `std_msgs/Float32` | 右手夹爪值 0~1（食指捏合） |
| `/quest3/left_gripper` | `std_msgs/Float32` | 左手夹爪值 |
| `/quest3/right_index_relative` | `geometry_msgs/Vector3Stamped` | 右手食指相对手腕（世界坐标差值） |
| `/quest3/left_index_relative` | `geometry_msgs/Vector3Stamped` | 左手食指相对手腕 |
| `/quest3/right_index_relative_pose` | `geometry_msgs/PoseStamped` | 右手食指相对手腕（局部坐标+旋转） |
| `/quest3/left_index_relative_pose` | `geometry_msgs/PoseStamped` | 左手食指相对手腕 |
| `/quest3/clutch` | `std_msgs/Bool` | Clutch 状态（true=发送数据） |

---

## 坐标系

**发送坐标系**: ROS2 REP 103（右手系）

| 轴 | 方向 |
|----|------|
| X | 前（Forward） |
| Y | 左（Left） |
| Z | 上（Up） |

**转换公式** (Unity -> ROS2):

```
x_ros = z_unity
y_ros = -x_unity
z_ros = y_unity
```

---

## 消息格式

### 1. 手腕位姿 (PoseStamped)

```json
{
  "op": "publish",
  "topic": "/quest3/right_hand_pose",
  "msg": {
    "header": {
      "stamp": { "sec": 1739959860, "nanosec": 123000000 },
      "frame_id": "quest3_right_hand"
    },
    "pose": {
      "position": { "x": 0.123456, "y": -0.045678, "z": 0.234567 },
      "orientation": { "x": 0.001234, "y": -0.002345, "z": 0.003456, "w": 0.999990 }
    }
  }
}
```

### 2. 夹爪值 (Float32)

```json
{
  "op": "publish",
  "topic": "/quest3/right_gripper",
  "msg": { "data": 0.8500 }
}
```

### 3. Clutch 状态 (Bool)

```json
{
  "op": "publish",
  "topic": "/quest3/clutch",
  "msg": { "data": true }
}
```

### 4. 食指相对手腕 - 世界坐标差值 (Vector3Stamped)

```json
{
  "op": "publish",
  "topic": "/quest3/right_index_relative",
  "msg": {
    "header": {
      "stamp": { "sec": 1739959860, "nanosec": 123000000 },
      "frame_id": "quest3_right_wrist"
    },
    "vector": { "x": 0.030000, "y": -0.005000, "z": 0.010000 }
  }
}
```

### 5. 食指相对手腕 - 局部坐标+旋转 (PoseStamped)

```json
{
  "op": "publish",
  "topic": "/quest3/right_index_relative_pose",
  "msg": {
    "header": {
      "stamp": { "sec": 1739959860, "nanosec": 123000000 },
      "frame_id": "quest3_right_wrist"
    },
    "pose": {
      "position": { "x": 0.028000, "y": -0.004000, "z": 0.095000 },
      "orientation": { "x": 0.05, "y": -0.02, "z": 0.01, "w": 0.998 }
    }
  }
}
```

**语义说明**:
- `position` = 食指尖在手腕局部坐标系下的位置: `inv(R_wrist) * (p_index - p_wrist)`
- `orientation` = 食指尖相对手腕的旋转: `inv(q_wrist) * q_index`

---

## 控制流程

```
1. 左手小指 Pinch -> Clutch ON/OFF 切换
2. Clutch ON -> ReAnchorBothHands() 记录当前位置为零点
3. 发送循环 90Hz -> 发送相对 anchor 的位移 + 旋转
4. 安全限制 -> 工作空间裁剪 + 速度限制
5. WebSocket 发送 -> rosbridge_server
```

---

## 位姿语义

发送的手腕 pose 是**控制空间下的绝对位姿**（原点在 Clutch ON 时的 anchor 位置），不是逐帧 delta。

- Clutch ON 时，当前手位置被记录为 anchor（零点）
- 后续发送 `pose = current - anchor`
- ROS 端可直接作为 TCP 目标位姿使用

---

## 核心脚本

| 文件 | 功能 |
|------|------|
| `HandTrackingController.cs` | 手势/手柄双模式输入，提取手腕+食指骨骼 |
| `TeleopManager.cs` | Clutch 控制、校准、安全限制、发送调度 |
| `ROSBridgeConnection.cs` | WebSocket 连接、心跳、消息队列 |
| `ROSCoordinateConverter.cs` | Unity - ROS2 坐标系转换 |
| `IronManHUD.cs` | 沉浸式 HUD 界面 |
| `SettingsPanel.cs` | 设置面板 UI |

---

## 使用方法

### 1. ROS 端启动 rosbridge

```bash
ros2 launch rosbridge_server rosbridge_websocket_launch.xml
```

### 2. Unity Inspector 配置

在 `ROSBridgeConnection` 组件设置:
- `Rosbridge Url`: `ws://<ROS_IP>:9090`
- `Use ROS2 Coordinates`: 勾选（启用坐标转换）

### 3. Quest 操作

1. 戴上头显，确保双手可见
2. 左手小指+拇指捏合 -> Clutch ON（开始发送）
3. 移动右手控制机械臂
4. 食指捏合控制夹爪开合
5. 再次小指捏合 -> Clutch OFF（停止发送）

---

## ROS 端验证

```bash
# 查看所有 Quest topics
ros2 topic list | grep quest3

# 监听手腕位姿
ros2 topic echo /quest3/right_hand_pose

# 监听夹爪
ros2 topic echo /quest3/right_gripper

# 监听食指局部位姿
ros2 topic echo /quest3/right_index_relative_pose

# 监听 Clutch 状态
ros2 topic echo /quest3/clutch
```

---

## License

MIT
