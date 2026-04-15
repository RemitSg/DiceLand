# 歧路旅人风格2D精灵3D光照Shader

这个Shader实现了类似《歧路旅人》（Octopath Traveler）的2D精灵适配3D光照效果，让2D像素艺术角色在3D环境中接收动态光照。

## 主要特性

### 1. 3D光照支持
- **法线贴图**：使用法线贴图让2D精灵有3D光照反应
- **动态光照**：支持URP的主光源和附加光源
- **阴影**：支持接收阴影（需要ShadowCaster Pass）

### 2. 光照效果
- **漫反射**：基础光照计算
- **高光反射**：Blinn-Phong高光模型，可调节光泽度
- **边缘光**：Rim Lighting效果，增强轮廓
- **环境光**：可调节的环境光强度

### 3. 2D精灵特性保留
- **Sprite支持**：保持2D精灵的所有特性
- **Alpha裁剪**：透明度阈值裁剪
- **Flip支持**：水平/垂直翻转
- **外部Alpha通道**：ETC1压缩纹理支持
- **预乘Alpha混合**：正确的透明混合

## 使用方法

### 1. 创建材质
1. 在Unity中创建新材质
2. 将Shader设置为"Assets/Shaders/Lit"
3. 分配Sprite纹理到_MainTex

### 2. 法线贴图创建
对于2D精灵，你需要创建法线贴图。有几种方法：

#### 方法A：使用Unity Sprite Editor（推荐）
1. 导入Sprite纹理
2. 在Inspector中设置Texture Type为"Sprite (2D and UI)"
3. 点击"Advanced" → 勾选"Generate Normal Map"
4. 调整法线贴图强度

#### 方法B：使用图像编辑软件
1. 在Photoshop/GIMP中使用法线贴图滤镜
2. 或者使用在线工具：https://cpetry.github.io/NormalMap-Online/

#### 方法C：使用Unity Asset Store工具
- Sprite Illuminator
- 2D Sprite Shader Kit

### 3. 光照设置
1. 确保场景中有URP的Directional Light
2. 调整光源位置和强度
3. 启用阴影（如果需要）

### 4. 材质参数调节

#### 基础参数：
- **_MainTex**：Sprite纹理
- **_NormalMap**：法线贴图（关键！）
- **_Color**：颜色着色
- **_Cutoff**：透明度裁剪阈值

#### 光照参数：
- **_Specular**：高光颜色
- **_Glossiness**：光泽度（0-1）
- **_RimColor**：边缘光颜色
- **_RimPower**：边缘光强度（0.5-8.0）
- **_AmbientIntensity**：环境光强度（0-1）

## 示例设置

### 角色精灵：
- Specular: (0.3, 0.3, 0.3, 1)
- Glossiness: 0.2
- RimColor: (0.8, 0.8, 1.0, 1)
- RimPower: 2.5
- AmbientIntensity: 0.2

### 环境物体：
- Specular: (0.1, 0.1, 0.1, 1)
- Glossiness: 0.1
- RimColor: (0.5, 0.5, 0.5, 1)
- RimPower: 1.5
- AmbientIntensity: 0.3

## 性能优化

1. **LOD系统**：为不同距离的精灵使用不同复杂度的Shader变体
2. **批处理**：确保使用相同材质的精灵可以合批
3. **光照剔除**：合理设置光照范围
4. **阴影优化**：根据需要启用/禁用阴影

## 故障排除

### 问题1：没有光照效果
- 检查场景中是否有激活的光源
- 确认法线贴图已正确分配
- 检查Shader编译错误

### 问题2：法线贴图看起来不对
- 确保法线贴图是切线空间法线贴图
- 检查法线贴图导入设置
- 尝试反转法线贴图的绿色通道

### 问题3：透明排序问题
- 确保"Queue"设置为"Transparent"
- 检查ZWrite设置为Off
- 调整渲染顺序

## 进阶用法

### 1. 自定义光照
修改`CalculateLighting`函数来实现不同的光照模型：
- Cel Shading（卡通着色）
- PBR（基于物理的渲染）
- 自定义光照函数

### 2. 特效集成
可以添加：
- 自发光
- 反射
- 折射
- 雾效

### 3. 动画支持
通过脚本控制材质参数来实现：
- 动态边缘光
- 闪烁效果
- 颜色渐变

## 兼容性
- Unity 2021.3+
- URP 12.0+
- 支持PC、移动平台

## 参考
- 《歧路旅人》（Octopath Traveler）美术风格
- URP官方文档
- Unity Shader编程指南