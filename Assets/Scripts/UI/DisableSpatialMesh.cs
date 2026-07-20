using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;

namespace MRReP.UI
{
    /// <summary>
    /// 禁用MRTK空间感知网格显示。
    /// 挂到MixedRealityToolkit或任意激活物体上即可。
    /// </summary>
    public class DisableSpatialMesh : MonoBehaviour
    {
        private void Start()
        {
            var saSystem = CoreServices.SpatialAwarenessSystem;
            if (saSystem == null)
            {
                Debug.LogWarning("[DisableSpatialMesh] 未找到空间感知系统");
                return;
            }

            var dataProviderAccess = saSystem as IMixedRealityDataProviderAccess;
            if (dataProviderAccess == null)
            {
                Debug.LogWarning("[DisableSpatialMesh] 无法获取DataProviderAccess");
                return;
            }

            // 获取所有网格观察者并禁用显示
            var observers = dataProviderAccess.GetDataProviders<IMixedRealitySpatialAwarenessMeshObserver>();
            foreach (var observer in observers)
            {
                observer.DisplayOption = SpatialAwarenessMeshDisplayOptions.None;
                Debug.Log("[DisableSpatialMesh] 已禁用: " + observer.Name);
            }

            Debug.Log("[DisableSpatialMesh] 共禁用 " + observers.Count + " 个网格观察者");
        }
    }
}
