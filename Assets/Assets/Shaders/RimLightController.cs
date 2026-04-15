using UnityEngine;

/// <summary>
/// 边缘光效果控制器
/// 用于实时调整Shader中的边缘光参数
/// </summary>
[ExecuteInEditMode]
public class RimLightController : MonoBehaviour
{
    [Header("材质引用")]
    [Tooltip("要控制的材质，如果为空则使用当前GameObject的材质")]
    public Material targetMaterial;
    
    [Header("边缘光基础设置")]
    [ColorUsage(true, true)]
    [Tooltip("边缘光颜色")]
    public Color rimColor = Color.white;
    
    [Range(0.5f, 8.0f)]
    [Tooltip("边缘光锐利度：值越大越集中在边缘")]
    public float rimPower = 3.0f;
    
    [Range(0f, 2f)]
    [Tooltip("边缘光强度倍乘")]
    public float rimIntensity = 1.0f;
    
    [Header("动态效果")]
    [Tooltip("启用根据视角动态调整边缘光")]
    public bool enableDynamicRim = true;
    
    [Range(0f, 1f)]
    [Tooltip("动态调整的最小强度")]
    public float minDynamicIntensity = 0.5f;
    
    [Range(1f, 2f)]
    [Tooltip("动态调整的最大强度")]
    public float maxDynamicIntensity = 1.5f;
    
    [Header("动画效果")]
    [Tooltip("启用边缘光脉动效果")]
    public bool enablePulse = false;
    
    [Range(0.1f, 5f)]
    [Tooltip("脉动速度")]
    public float pulseSpeed = 1.0f;
    
    [Range(0f, 1f)]
    [Tooltip("脉动强度变化范围")]
    public float pulseAmount = 0.2f;
    
    [Header("调试信息")]
    [ReadOnly]
    public float currentRimFactor = 0f;
    [ReadOnly]
    public float currentDynamicIntensity = 1f;
    
    // 私有变量
    private Material materialInstance;
    private Renderer targetRenderer;
    private float pulseTime = 0f;
    
    void Start()
    {
        InitializeMaterial();
    }
    
    void Update()
    {
        UpdateMaterialParameters();
        
        if (enableDynamicRim)
        {
            UpdateDynamicRimLight();
        }
        
        if (enablePulse)
        {
            UpdatePulseEffect();
        }
    }
    
    void OnValidate()
    {
        // 当Inspector值改变时立即更新
        UpdateMaterialParameters();
    }
    
    void OnDestroy()
    {
        // 清理实例化的材质
        if (materialInstance != null && Application.isPlaying)
        {
            Destroy(materialInstance);
        }
    }
    
    /// <summary>
    /// 初始化材质引用
    /// </summary>
    private void InitializeMaterial()
    {
        if (targetMaterial == null)
        {
            // 尝试从当前GameObject获取Renderer
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                // 创建材质实例以避免影响原始材质
                materialInstance = new Material(targetRenderer.sharedMaterial);
                targetRenderer.material = materialInstance;
                targetMaterial = materialInstance;
            }
        }
        else if (Application.isPlaying)
        {
            // 创建材质实例
            materialInstance = new Material(targetMaterial);
            targetMaterial = materialInstance;
            
            // 如果GameObject有Renderer，应用材质实例
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.material = materialInstance;
            }
        }
    }
    
    /// <summary>
    /// 更新材质参数
    /// </summary>
    private void UpdateMaterialParameters()
    {
        if (targetMaterial != null)
        {
            // 应用基础参数
            targetMaterial.SetColor("_RimColor", rimColor * rimIntensity);
            targetMaterial.SetFloat("_RimPower", rimPower);
        }
    }
    
    /// <summary>
    /// 更新动态边缘光效果
    /// </summary>
    private void UpdateDynamicRimLight()
    {
        if (targetMaterial == null || Camera.main == null)
            return;
        
        // 计算视角方向
        Vector3 viewDir = (Camera.main.transform.position - transform.position).normalized;
        
        // 获取法线方向（对于2D精灵，通常使用transform.up）
        Vector3 normal = transform.up;
        
        // 如果有Renderer，尝试获取真正的法线
        if (targetRenderer != null)
        {
            // 使用Renderer的bounds中心点的法线
            normal = (transform.position - targetRenderer.bounds.center).normalized;
        }
        
        // 计算边缘光因子
        currentRimFactor = 1.0f - Mathf.Clamp01(Vector3.Dot(viewDir, normal));
        currentRimFactor = Mathf.Pow(currentRimFactor, rimPower);
        
        // 根据视角动态调整强度
        currentDynamicIntensity = Mathf.Lerp(minDynamicIntensity, maxDynamicIntensity, currentRimFactor);
        
        // 应用动态强度
        targetMaterial.SetColor("_RimColor", rimColor * rimIntensity * currentDynamicIntensity);
    }
    
    /// <summary>
    /// 更新脉动效果
    /// </summary>
    private void UpdatePulseEffect()
    {
        if (targetMaterial == null)
            return;
        
        pulseTime += Time.deltaTime * pulseSpeed;
        
        // 使用正弦函数创建脉动效果
        float pulseValue = Mathf.Sin(pulseTime) * 0.5f + 0.5f; // 0到1之间
        float pulseIntensity = 1.0f + Mathf.Lerp(-pulseAmount, pulseAmount, pulseValue);
        
        // 应用脉动强度
        Color pulsedColor = rimColor * rimIntensity * pulseIntensity;
        targetMaterial.SetColor("_RimColor", pulsedColor);
    }
    
    /// <summary>
    /// 手动触发一次边缘光闪烁
    /// </summary>
    /// <param name="duration">闪烁持续时间</param>
    /// <param name="intensityMultiplier">强度倍乘</param>
    public void FlashRimLight(float duration = 0.5f, float intensityMultiplier = 2f)
    {
        StartCoroutine(FlashRimLightCoroutine(duration, intensityMultiplier));
    }
    
    private System.Collections.IEnumerator FlashRimLightCoroutine(float duration, float intensityMultiplier)
    {
        if (targetMaterial == null)
            yield break;
        
        Color originalColor = rimColor;
        float originalIntensity = rimIntensity;
        
        // 瞬间增强边缘光
        rimIntensity *= intensityMultiplier;
        UpdateMaterialParameters();
        
        // 等待
        yield return new WaitForSeconds(duration * 0.3f);
        
        // 渐变恢复
        float elapsedTime = 0f;
        while (elapsedTime < duration * 0.7f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (duration * 0.7f);
            rimIntensity = Mathf.Lerp(originalIntensity * intensityMultiplier, originalIntensity, t);
            UpdateMaterialParameters();
            yield return null;
        }
        
        // 确保恢复原状
        rimIntensity = originalIntensity;
        UpdateMaterialParameters();
    }
    
    /// <summary>
    /// 重置所有参数到默认值
    /// </summary>
    public void ResetToDefaults()
    {
        rimColor = Color.white;
        rimPower = 3.0f;
        rimIntensity = 1.0f;
        enableDynamicRim = true;
        minDynamicIntensity = 0.5f;
        maxDynamicIntensity = 1.5f;
        enablePulse = false;
        pulseSpeed = 1.0f;
        pulseAmount = 0.2f;
        
        UpdateMaterialParameters();
    }
    
    /// <summary>
    /// 保存当前设置为预设
    /// </summary>
    public void SaveAsPreset(string presetName)
    {
        RimLightPreset preset = ScriptableObject.CreateInstance<RimLightPreset>();
        preset.rimColor = rimColor;
        preset.rimPower = rimPower;
        preset.rimIntensity = rimIntensity;
        
        // 这里可以添加保存到文件的逻辑
        Debug.Log($"预设 '{presetName}' 已创建（需要实现保存逻辑）");
    }
    
    /// <summary>
    /// 从预设加载设置
    /// </summary>
    public void LoadPreset(RimLightPreset preset)
    {
        if (preset != null)
        {
            rimColor = preset.rimColor;
            rimPower = preset.rimPower;
            rimIntensity = preset.rimIntensity;
            
            UpdateMaterialParameters();
            Debug.Log("预设已加载");
        }
    }
}

/// <summary>
/// 边缘光预设数据类
/// </summary>
public class RimLightPreset : ScriptableObject
{
    public Color rimColor = Color.white;
    public float rimPower = 3.0f;
    public float rimIntensity = 1.0f;
}

/// <summary>
/// 只读属性特性（用于调试显示）
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif