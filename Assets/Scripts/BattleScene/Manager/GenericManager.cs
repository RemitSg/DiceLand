using UnityEngine;

/// <summary>
/// 泛型Manager基类，提供通用的Manager功能
/// </summary>
/// <typeparam name="T">Manager管理的对象类型</typeparam>
public class GenericManager<T> : MonoBehaviour where T : class
{
    #region Singleton Pattern
    private static GenericManager<T> _instance;
    
    /// <summary>
    /// 获取Manager的单例实例
    /// </summary>
    public static GenericManager<T> Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GenericManager<T>>();
                
                if (_instance == null)
                {
                    GameObject managerObject = new GameObject(typeof(GenericManager<T>).Name);
                    _instance = managerObject.AddComponent<GenericManager<T>>();
                    DontDestroyOnLoad(managerObject);
                }
            }
            
            return _instance;
        }
    }
    
    /// <summary>
    /// 检查单例是否已初始化
    /// </summary>
    public static bool IsSingletonInitialized => _instance != null;
    #endregion
    
    #region Initialization & Lifecycle
    /// <summary>
    /// Manager初始化状态
    /// </summary>
    public bool IsInitialized { get; private set; } = false;
    
    /// <summary>
    /// Manager初始化
    /// </summary>
    public virtual void Initialize()
    {
        if (IsInitialized)
        {
            Debug.LogWarning($"{GetType().Name} 已经初始化过了");
            return;
        }
        
        // 执行初始化逻辑
        OnInitialize();
        IsInitialized = true;
        
        Debug.Log($"{GetType().Name} 初始化完成");
    }
    
    /// <summary>
    /// 子类可以重写的初始化方法
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 子类可以重写此方法来实现具体的初始化逻辑
    }
    
    /// <summary>
    /// Manager销毁
    /// </summary>
    public virtual void Destroy()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"{GetType().Name} 尚未初始化");
            return;
        }
        
        // 执行销毁逻辑
        OnManagerDestroy();
        IsInitialized = false;
        
        Debug.Log($"{GetType().Name} 销毁完成");
    }
    
    /// <summary>
    /// 子类可以重写的销毁方法
    /// </summary>
    protected virtual void OnManagerDestroy()
    {
        // 子类可以重写此方法来实现具体的销毁逻辑
    }
    
    /// <summary>
    /// Unity生命周期：Awake方法
    /// </summary>
    protected virtual void Awake()
    {
        // 确保只有一个实例
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this as GenericManager<T>;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Unity生命周期：OnDestroy方法
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion
    
    #region Data Management
    private T _managedData;
    
    /// <summary>
    /// 管理的数据对象
    /// </summary>
    public T ManagedData
    {
        get => _managedData;
        set
        {
            if (_managedData != null && _managedData.Equals(value))
            {
                return;
            }
            
            T oldValue = _managedData;
            _managedData = value;
            OnDataChanged(oldValue, _managedData);
        }
    }
    
    /// <summary>
    /// 当数据发生变化时调用
    /// </summary>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    protected virtual void OnDataChanged(T oldValue, T newValue)
    {
        // 子类可以重写此方法来处理数据变化
    }
    
    /// <summary>
    /// 清空管理的数据
    /// </summary>
    public virtual void ClearData()
    {
        _managedData = null;
        OnDataCleared();
    }
    
    /// <summary>
    /// 当数据被清空时调用
    /// </summary>
    protected virtual void OnDataCleared()
    {
        // 子类可以重写此方法来处理数据清空
    }
    #endregion
    
    #region Event Management
    /// <summary>
    /// 事件委托类型
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="args">事件参数</param>
    public delegate void ManagerEventHandler(object sender, ManagerEventArgs args);
    
    /// <summary>
    /// Manager事件参数
    /// </summary>
    public class ManagerEventArgs : System.EventArgs
    {
        public string EventName { get; }
        public object Data { get; }
        
        public ManagerEventArgs(string eventName, object data = null)
        {
            EventName = eventName;
            Data = data;
        }
    }
    
    /// <summary>
    /// 事件字典
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, System.Action<object, ManagerEventArgs>> _eventHandlers = 
        new System.Collections.Generic.Dictionary<string, System.Action<object, ManagerEventArgs>>();
    
    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="handler">事件处理方法</param>
    public void AddEventListener(string eventName, System.Action<object, ManagerEventArgs> handler)
    {
        if (!_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers[eventName] = null;
        }
        
        _eventHandlers[eventName] += handler;
    }
    
    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="handler">事件处理方法</param>
    public void RemoveEventListener(string eventName, System.Action<object, ManagerEventArgs> handler)
    {
        if (_eventHandlers.ContainsKey(eventName))
        {
            _eventHandlers[eventName] -= handler;
        }
    }
    
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="data">事件数据</param>
    protected void TriggerEvent(string eventName, object data = null)
    {
        if (_eventHandlers.TryGetValue(eventName, out var handler) && handler != null)
        {
            handler(this, new ManagerEventArgs(eventName, data));
        }
    }
    #endregion
    
    #region Utility Methods
    /// <summary>
    /// 检查Manager是否已准备好
    /// </summary>
    /// <returns>如果Manager已初始化则返回true</returns>
    public bool IsReady()
    {
        return IsInitialized && _managedData != null;
    }
    
    /// <summary>
    /// 等待Manager准备好
    /// </summary>
    /// <param name="timeout">超时时间（秒）</param>
    /// <returns>如果Manager在超时前准备好则返回true</returns>
    public System.Collections.IEnumerator WaitForReady(float timeout = 5f)
    {
        float startTime = Time.time;
        
        while (!IsReady() && Time.time - startTime < timeout)
        {
            yield return null;
        }
        
        if (!IsReady())
        {
            Debug.LogWarning($"{GetType().Name} 在超时时间内未能准备好");
        }
    }
    #endregion
}