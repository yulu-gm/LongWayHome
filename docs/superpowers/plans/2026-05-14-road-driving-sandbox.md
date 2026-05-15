# 公路驾驶沙盒 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 做出一个可运行、可驾驶、可调参的 Godot 4.6 C# 车辆驾驶沙盒，支持 2.5D 斜俯视、四种车辆 preset、手刹漂移、悬挂、引擎和自动变速箱。

**Architecture:** `DriveSandbox.tscn` 是第一阶段入口，`PrototypeCar.tscn` 使用 `RigidBody3D` 车身和四个自研 raycast 轮。车辆纯计算逻辑拆到 `scripts/vehicles/core/`，Godot 节点适配拆到 `scripts/vehicles/runtime/`，UI 和相机独立，保证单元测试能覆盖动力、差速、轮胎和 preset 行为。

**Tech Stack:** Godot 4.6 .NET, C#, .NET SDK 9, NUnit for pure C# tests, Godot `RigidBody3D`/`PhysicsRayQueryParameters3D` for runtime wheel physics.

---

## File Map

- Create: `Godot_V2.csproj`，Godot C# 项目文件。
- Create: `Godot_V2.sln`，方便 `dotnet test` 引用主项目。
- Create: `tests/Godot_V2.Tests/Godot_V2.Tests.csproj`，NUnit 测试项目。
- Create: `scripts/vehicles/core/VehicleTypes.cs`，驱动形式、输入快照、轮端数据、遥测基础类型。
- Create: `scripts/vehicles/core/VehiclePreset.cs`，车辆参数资源和内置 preset 工厂。
- Create: `scripts/vehicles/core/Gearbox.cs`，自动变速箱。
- Create: `scripts/vehicles/core/Differential.cs`，FWD/RWD/AWD 扭矩分配。
- Create: `scripts/vehicles/core/EngineModel.cs`，发动机扭矩曲线和 RPM 目标。
- Create: `scripts/vehicles/core/TireForceModel.cs`，纵向/横向轮胎力和手刹抓地衰减。
- Create: `scripts/vehicles/core/Powertrain.cs`，油门、刹车、倒车、引擎、变速箱、差速串联。
- Create: `scripts/vehicles/runtime/VehicleController.cs`，`RigidBody3D` 车辆控制器。
- Create: `scripts/vehicles/runtime/RaycastWheel.cs`，单轮 raycast 悬挂与轮胎力。
- Create: `scripts/vehicles/runtime/VehicleInputReader.cs`，Godot 输入采样。
- Create: `scripts/camera/IsoFollowCamera.cs`，2.5D 斜俯视跟随相机。
- Create: `scripts/ui/VehicleDebugPanel.cs`，HUD 和调参面板。
- Create: `scripts/sandbox/DriveSandbox.cs`，沙盒 preset 切换和场景协调。
- Create: `scenes/DriveSandbox.tscn`，主场景。
- Create: `scenes/vehicles/PrototypeCar.tscn`，原型车。
- Create: `scenes/ui/VehicleDebugPanel.tscn`，调试 UI。
- Modify: `project.godot`，加入主场景和输入映射。
- Modify: `.gitignore`，忽略 `.superpowers/`。

## Task 1: C# Project and Test Harness

**Files:**
- Create: `Godot_V2.csproj`
- Create: `tests/Godot_V2.Tests/Godot_V2.Tests.csproj`
- Create: `tests/Godot_V2.Tests/VehicleCoreTests.cs`

- [ ] **Step 1: Write failing smoke test**

Create `tests/Godot_V2.Tests/VehicleCoreTests.cs` with:

```csharp
using Godot_V2.Scripts.Vehicles.Core;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class VehicleCoreTests
{
    [Test]
    public void PresetFactoryCreatesFourDistinctDrivetrains()
    {
        var presets = VehiclePresetFactory.CreateDefaults();

        Assert.That(presets, Has.Count.EqualTo(4));
        Assert.That(presets.Select(p => p.Drivetrain).Distinct(), Is.SupersetOf(new[]
        {
            DrivetrainType.Fwd,
            DrivetrainType.Rwd,
            DrivetrainType.Awd
        }));
    }
}
```

- [ ] **Step 2: Run test and verify RED**

Run:

```powershell
dotnet test tests/Godot_V2.Tests/Godot_V2.Tests.csproj
```

Expected: fail because the test project or `VehiclePresetFactory` does not exist.

- [ ] **Step 3: Add project files**

Create `Godot_V2.csproj` with `Godot.NET.Sdk`, and `tests/Godot_V2.Tests/Godot_V2.Tests.csproj` with NUnit references and a project reference to the main project.

- [ ] **Step 4: Add minimal preset types**

Add `VehicleTypes.cs` and `VehiclePreset.cs` with `DrivetrainType`, `VehiclePreset`, and `VehiclePresetFactory.CreateDefaults()`.

- [ ] **Step 5: Run test and verify GREEN**

Run:

```powershell
dotnet test tests/Godot_V2.Tests/Godot_V2.Tests.csproj
```

Expected: one passing test.

## Task 2: Powertrain and Drivetrain Core

**Files:**
- Modify: `tests/Godot_V2.Tests/VehicleCoreTests.cs`
- Create: `scripts/vehicles/core/Gearbox.cs`
- Create: `scripts/vehicles/core/Differential.cs`
- Create: `scripts/vehicles/core/EngineModel.cs`
- Create: `scripts/vehicles/core/Powertrain.cs`

- [ ] **Step 1: Write failing tests**

Add tests that assert:

```csharp
[Test]
public void RwdDifferentialSendsTorqueOnlyToRearWheels()
{
    var torque = Differential.SplitTorque(DrivetrainType.Rwd, 400f, 0.5f);
    Assert.That(torque.FrontLeft, Is.EqualTo(0f));
    Assert.That(torque.FrontRight, Is.EqualTo(0f));
    Assert.That(torque.RearLeft, Is.EqualTo(200f).Within(0.001f));
    Assert.That(torque.RearRight, Is.EqualTo(200f).Within(0.001f));
}

[Test]
public void AutomaticGearboxUpshiftsNearRedline()
{
    var gearbox = new Gearbox(new[] { 3.2f, 2.1f, 1.4f }, 3.42f, 0.8f, 0.35f);
    gearbox.Update(0.1f, 0.9f, 6100f, 6500f);
    Assert.That(gearbox.CurrentGear, Is.EqualTo(2));
}

[Test]
public void PowertrainProducesForwardTorqueUnderThrottle()
{
    var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");
    var powertrain = new Powertrain(preset);
    var output = powertrain.Update(0.016f, throttle: 1f, brake: 0f, wheelSpeedKph: 20f);
    Assert.That(output.EngineRpm, Is.GreaterThan(preset.IdleRpm));
    Assert.That(output.WheelTorque.Total, Is.GreaterThan(0f));
}
```

- [ ] **Step 2: Run and verify RED**

Run:

```powershell
dotnet test tests/Godot_V2.Tests/Godot_V2.Tests.csproj --filter VehicleCoreTests
```

Expected: fail because the drivetrain classes do not exist.

- [ ] **Step 3: Implement minimal drivetrain core**

Implement:

- `Gearbox.Update(delta, throttle, rpm, redlineRpm)` with shift cooldown and up/down thresholds.
- `Differential.SplitTorque(type, totalTorque, awdFrontBias)`.
- `EngineModel.EvaluateTorque(rpm, throttle, preset)`.
- `Powertrain.Update(delta, throttle, brake, wheelSpeedKph)`.

- [ ] **Step 4: Run and verify GREEN**

Run the same filtered test command. Expected: all drivetrain tests pass.

## Task 3: Tire Force and Handbrake Core

**Files:**
- Modify: `tests/Godot_V2.Tests/VehicleCoreTests.cs`
- Create: `scripts/vehicles/core/TireForceModel.cs`

- [ ] **Step 1: Write failing tire tests**

Add tests that assert:

```csharp
[Test]
public void TireLateralForceFallsAfterPeakSlip()
{
    var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");
    var lowSlip = TireForceModel.CalculateLateralForce(preset, normalLoad: 3500f, lateralSlip: 0.15f, handbrake: false, isRearWheel: true);
    var highSlip = TireForceModel.CalculateLateralForce(preset, normalLoad: 3500f, lateralSlip: 1.2f, handbrake: false, isRearWheel: true);
    Assert.That(MathF.Abs(highSlip), Is.LessThan(MathF.Abs(lowSlip)));
}

[Test]
public void HandbrakeReducesRearLateralGrip()
{
    var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");
    var normal = TireForceModel.CalculateLateralForce(preset, 3500f, 0.25f, handbrake: false, isRearWheel: true);
    var handbrake = TireForceModel.CalculateLateralForce(preset, 3500f, 0.25f, handbrake: true, isRearWheel: true);
    Assert.That(MathF.Abs(handbrake), Is.LessThan(MathF.Abs(normal) * 0.7f));
}
```

- [ ] **Step 2: Run and verify RED**

Run:

```powershell
dotnet test tests/Godot_V2.Tests/Godot_V2.Tests.csproj --filter Tire
```

Expected: fail because `TireForceModel` does not exist.

- [ ] **Step 3: Implement tire model**

Implement a tunable approximation:

- Slip below peak ramps linearly to peak grip.
- Slip above peak blends down to slide grip.
- Handbrake lowers rear lateral grip using `HandbrakeRearGripFactor`.
- Longitudinal force clamps by normal load and grip.

- [ ] **Step 4: Run and verify GREEN**

Run the same filtered test command. Expected: tire tests pass.

## Task 4: Godot Runtime Vehicle

**Files:**
- Create: `scripts/vehicles/runtime/VehicleInputReader.cs`
- Create: `scripts/vehicles/runtime/RaycastWheel.cs`
- Create: `scripts/vehicles/runtime/VehicleController.cs`
- Create: `scenes/vehicles/PrototypeCar.tscn`

- [ ] **Step 1: Create runtime scripts**

Implement `VehicleInputReader` from Godot input actions and `VehicleController` as a `RigidBody3D` with exported wheel node paths and preset selection.

- [ ] **Step 2: Implement raycast wheel**

`RaycastWheel` must:

- Query ground below each wheel using `PhysicsDirectSpaceState3D.IntersectRay`.
- Calculate spring force from suspension compression.
- Calculate damping force from compression velocity.
- Apply tire force to the parent rigid body at the wheel contact point.
- Store compression, load and slip telemetry.

- [ ] **Step 3: Create prototype car scene**

Scene must contain:

- `RigidBody3D` root with `VehicleController`.
- Body collision box and simple visible body mesh.
- Four wheel marker nodes.
- Four simple wheel meshes.

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build Godot_V2.csproj
```

Expected: build succeeds with zero C# compile errors.

## Task 5: Sandbox Scene, Camera and UI

**Files:**
- Create: `scripts/camera/IsoFollowCamera.cs`
- Create: `scripts/ui/VehicleDebugPanel.cs`
- Create: `scripts/sandbox/DriveSandbox.cs`
- Create: `scenes/ui/VehicleDebugPanel.tscn`
- Create: `scenes/DriveSandbox.tscn`
- Modify: `project.godot`

- [ ] **Step 1: Add input map**

Add actions:

- `drive_throttle`: W
- `drive_brake`: S
- `drive_steer_left`: A
- `drive_steer_right`: D
- `drive_handbrake`: Shift
- `toggle_debug_panel`: Tab

- [ ] **Step 2: Add camera**

`IsoFollowCamera` follows the target from a high diagonal offset and looks at the vehicle, using fixed 2.5D composition for Phase 1.

- [ ] **Step 3: Add UI**

`VehicleDebugPanel` displays speed, RPM, gear, drivetrain, handbrake, front/rear slip, four wheel compression/load, and buttons for four preset IDs.

- [ ] **Step 4: Add sandbox scene**

`DriveSandbox.tscn` contains ground, a broad road-like plane, lights, `PrototypeCar`, `IsoFollowCamera`, and `VehicleDebugPanel`.

- [ ] **Step 5: Set main scene**

Set `application/run/main_scene="res://scenes/DriveSandbox.tscn"` in `project.godot`.

## Task 6: Verification and Playability Pass

**Files:**
- Modify as needed: runtime scripts and scene files.

- [ ] **Step 1: Run automated tests**

Run:

```powershell
dotnet test tests/Godot_V2.Tests/Godot_V2.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 2: Run compile check**

Run:

```powershell
dotnet build Godot_V2.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Run Godot project**

Find the installed Godot executable if it is not in `PATH`, then run the project. The scene should open into `DriveSandbox`.

- [ ] **Step 4: Manual drive validation**

Validate:

- `W` accelerates forward.
- `S` brakes and can reverse at low speed.
- `A/D` turn the car.
- `Shift` induces rear slide.
- `Tab` toggles panel.
- All four preset buttons change behavior.
- HUD updates while driving.

- [ ] **Step 5: Tune until playable**

Adjust preset and tire values until the vehicle can be driven for at least one minute without physics explosions, and RWD standard / RWD sports can drift with `Shift`.

- [ ] **Step 6: Email notification**

After verification succeeds, send completion email using:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\yulu\.codex\skills\email-completion-notice\scripts\send-completion-email.ps1" -Subject "Codex 任务完成: 公路驾驶沙盒" -Body "<简短完成说明>"
```

## Self-Review

- Spec coverage: 计划覆盖 Phase 1 驾驶沙盒、2.5D 相机、C# 自研车辆物理、四种 preset、HUD/面板、测试和可玩验证。Phase 2 仅保留边界，不在本计划实现。
- Completion marker scan: 没有未完成标记。
- Type consistency: `VehiclePreset`、`DrivetrainType`、`Powertrain`、`Differential`、`TireForceModel`、`VehicleController` 等名称在任务之间保持一致。
- Git constraint: 当前目录不是 git 仓库，计划不包含 commit 步骤；执行完成后用文件和验证结果交付。
