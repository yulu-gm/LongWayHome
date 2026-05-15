# DriveSandbox 基线验证

日期：2026-05-15

## 结论

当前 `DriveSandbox.tscn` 可以作为后续开发基线：

- C# 单元测试通过。
- C# 项目构建通过。
- Godot 场景可以加载。
- 原型车可以加速、转向、手刹。
- 调试面板可以通过 toggle 行为隐藏/显示。
- 可以生成当前沙盒截图。

但当前画面仍是开发验证场，距离 `design.png` 和 `road_trip_scene_visual_reference_cn.md` 的目标氛围差距很大。后续进入画面相关任务时，必须优先补道路环境、车辆旅行化轮廓、天气/灯光、地点 POI 和玩家 HUD。

## 验证命令

```powershell
dotnet test tests\Godot_V2.Tests\Godot_V2.Tests.csproj
```

结果：

```text
通过：10
失败：0
跳过：0
```

```powershell
dotnet build Godot_V2.csproj
```

结果：

```text
0 个警告
0 个错误
```

```powershell
& 'C:\Program Files\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\yulu\Documents\godot-v-2' --script 'res://tests/godot/vehicle_scene_smoke_test.gd'
```

结果：通过。脚本加载 `DriveSandbox.tscn`，找到 `PrototypeCar`，按下油门后速度超过阈值，并执行转向与手刹流程。

```powershell
& 'C:\Program Files\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\yulu\Documents\godot-v-2' --script 'res://tests/godot/vehicle_steering_direction_test.gd'
```

结果：通过。`D` 方向转向符合预期。

```powershell
& 'C:\Program Files\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\yulu\Documents\godot-v-2' --script 'res://tests/godot/vehicle_steering_direction_test.gd' -- --left
```

结果：通过。`A` 方向转向符合预期。

```powershell
& 'C:\Program Files\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --headless --path 'C:\Users\yulu\Documents\godot-v-2' --script 'res://tests/godot/debug_panel_toggle_test.gd'
```

结果：通过。`VehicleDebugPanel` 默认显示，触发 `toggle_debug_panel` 后隐藏，再次触发后显示。

```powershell
& 'C:\Program Files\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64_console.exe' --path 'C:\Users\yulu\Documents\godot-v-2' --script 'res://tests/godot/capture_drive_sandbox.gd'
```

结果：通过。截图保存到：

```text
C:\Users\yulu\Documents\godot-v-2\tmp\drive_sandbox_smoke.png
```

说明：同一截图脚本在 `--headless` 下会卡住，因此当前截图验证使用非 headless 渲染路径。

## 当前视觉基线

截图显示：

- 已有 2.5D 斜俯视相机。
- 已有简单道路、中心黄线、原型红色车辆、调试 HUD。
- 场景缺少道路边缘细节、自然环境、POI、天气、时间、车灯、交通和旅行化车辆资产。
- 当前 UI 是开发调试面板，不是最终驾驶 HUD。

视觉基线结论：

```text
可作为功能验证基线，但不能作为目标画面基线。
```
