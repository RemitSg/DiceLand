# Sprite阴影问题诊断清单

## 🔍 Shader代码层面检查

### ✅ 已完成的配置
1. **渲染队列**: `Queue = "AlphaTest"` ✅
2. **深度写入**: `ZWrite On` ✅
3. **ShadowCaster Pass**: 存在且正确 ✅
4. **DepthOnly Pass**: 存在且正确 ✅
5. **阴影关键字**: 
   - `_MAIN_LIGHT_SHADOWS` ✅
   - `_MAIN_LIGHT_SHADOWS_CASCADE` ✅
   - `_ADDITIONAL_LIGHT_SHADOWS` ✅
   - `_SHADOWS_SOFT` ✅
6. **阴影采样**: `mainLight.shadowAttenuation` 已应用 ✅

### ⚠️ 发现的问题

#### 问题1：预乘Alpha冲突
```hlsl
// 在SubShader级别
Blend SrcAlpha OneMinusSrcAlpha  // 标准Alpha混合

// 在fragment shader末尾
litColor *= baseColor.a;  // 预乘Alpha操作
```

这会导致颜色计算错误，但不是阴影不显示的原因。

#### 问题2：2D Sprite的根本限制（最可能的原因）

**关键发现**：Unity的2D Sprite系统有以下限制：

1. **SpriteRenderer的阴影投射设置**
   - SpriteRenderer默认**不投射阴影**
   - 需要在Inspector中启用"Cast Shadows"

2. **SpriteRenderer的阴影接收设置**
   - SpriteRenderer默认**不接收阴影**
   - 需要在Inspector中启用"Receive Shadows"

3. **2D Sprite的Mesh结构**
   - 2D Sprite默认是**四边形平面**
   - 没有厚度信息
   - ShadowCaster依赖法线方向
   - Sprite的法线是(0,0,1)或(0,0,-1)

## 🎯 需要检查的Unity设置

### 检查清单1：SpriteRenderer组件

对于每个要投射/接收阴影的Sprite：

1. **选中SpriteRenderer组件**
2. **检查"Cast Shadows"设置**：
   - 应该设置为"On"或"Two Sided"
   - 不能是"Off"或"Shadows Only"

3. **检查"Receive Shadows"设置**：
   - 应该勾选/启用
   - 如果不勾选，Sprite不会接收任何阴影

4. **检查"Sorting Layer"和"Order in Layer"**：
   - 影响渲染顺序
   - 可能遮挡阴影

### 检查清单2：光源设置

1. **选中主光源（Directional Light）**
2. **检查"Shadow Type"**：
   - 应该设置为"Soft Shadows"或"Hard Shadows"
   - 不能是"No Shadows"

3. **检查强度**：
   - 强度 > 0

### 检查清单3：URP Asset设置

1. **打开 Project Settings > Graphics**
2. **找到 Universal Render Pipeline Asset**
3. **点击"Open"进入Asset设置**
4. **检查以下设置**：
   - ✅ Main Light Shadows: Enabled
   - ✅ Additional Light Shadows: Enabled（如果使用点光源）
   - ✅ Shadow Distance: 合理的值（建议50-200）
   - ✅ Shadow Cascades: 2或4
   - ✅ Soft Shadows: Enabled（可选）

## 🧪 诊断步骤

### 步骤1：测试基本阴影功能

1. **创建一个测试场景**：
   ```
   - 添加一个3D Cube
   - 添加一个3D Plane（作为地面）
   - 添加一个Directional Light（启用阴影）
   ```

2. **验证3D物体的阴影**：
   - Cube应该在Plane上投射阴影
   - 如果不行，说明URP配置有问题
   - 如果可以，说明URP配置正常，问题在Sprite上

### 步骤2：测试Sprite阴影投射

1. **添加一个使用本Shader的Sprite**
2. **在SpriteRenderer中启用"Cast Shadows"**
3. **查看Sprite是否在Plane上投射阴影**
   - 如果可以，说明Sprite可以投射阴影
   - 如果不可以，说明问题更深层

### 步骤3：测试Sprite阴影接收

1. **添加两个Sprite**
2. **两个Sprite都启用"Cast Shadows"和"Receive Shadows"**
3. **设置Z位置使一个在另一个前面**
4. **查看前面的Sprite是否接收后面Sprite的阴影**

## 💡 可能的解决方案

### 方案1：强制启用SpriteRenderer阴影（推荐）

在场景中选中所有需要阴影的Sprite：

1. **打开SpriteRenderer组件**
2. **设置 Cast Shadows = "Two Sided"**
3. **启用 Receive Shadows（勾选）**

### 方案2：使用3D Mesh代替Sprite

如果SpriteRenderer的阴影功能不可靠：

1. **将Sprite转换为3D Quad Mesh**
2. **使用标准3D MeshRenderer**
3. **应用本Shader材质**
4. **MeshRenderer的阴影功能更可靠**

### 方案3：修改Shader强制投射阴影

在Shader中添加Fallback或强制启用阴影：

```hlsl
// 在Properties中添加
[Toggle(_CASTING_SHADOWS)] _CastingShadows ("Cast Shadows", Float) = 1

// 在SubShader中确保所有Pass都正确处理
```

## 🔧 紧急修复尝试

尝试以下修改，看是否有效：

### 修改1：关闭预乘Alpha

```hlsl
// 在fragment shader中，注释掉这行：
// litColor *= baseColor.a;

// 改为直接返回
return float4(litColor, baseColor.a);
```

### 修改2：启用两面法线

```hlsl
// 在ShadowCaster Pass的vertex shader中
// 确保法线方向正确
float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

// 尝试强制法线方向
normalWS = normalize(float3(0, 0, -1));
```

### 修改3：添加诊断输出

```hlsl
// 在fragment shader中添加（仅用于调试）
// #define DEBUG_SHADOWS 1

#ifdef DEBUG_SHADOWS
    return float4(mainLight.shadowAttenuation.xxx, 1.0);
#endif
```

## 📋 完整检查清单

在回复时，请确认以下信息：

- [ ] 3D Cube能在Plane上投射阴影吗？
- [ ] SpriteRenderer的Cast Shadows设置是什么？
- [ ] SpriteRenderer的Receive Shadows是否勾选？
- [ ] Directional Light的Shadow Type是什么？
- [ ] URP Asset中的Main Light Shadows是否启用？
- [ ] Sprite的Z位置是否正确（有前后关系）？
- [ ] 使用的是Unity的SpriteRenderer还是自定义Mesh？

## 🎯 最可能的原因

根据经验，**最可能的原因是**：

1. **SpriteRenderer的Cast Shadows = Off**（最常见）
2. **SpriteRenderer的Receive Shadows未勾选**
3. **光源的Shadow Type = No Shadows**

这三个设置在Unity默认情况下都是关闭的，必须手动启用！

## 下一步

请按照上述清单检查你的设置，然后告诉我：
1. 哪些设置你已经确认过？
2. 哪些设置你不确定？
3. 你看到了什么具体的现象？

我会根据你的反馈提供更精确的解决方案。