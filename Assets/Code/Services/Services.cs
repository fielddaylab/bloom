#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections.Generic;
using UnityEngine;
using BeauUtil.Services;
using UnityEngine.SceneManagement;

namespace Zavala
{
    public class Services
    {
        #region Cache

        static Services()
        {
            Application.quitting += () => { s_Quitting = true; };
        }
        
        static private readonly ServiceCache s_ServiceCache = new ServiceCache();
        static private bool s_Quitting;

        static public bool Valid { get { return !s_Quitting; } }

        #endregion // Cache

        #region Accessors

        [ServiceReference, UnityEngine.Scripting.Preserve] static public AssetsService Assets { get; private set; }
        [ServiceReference, UnityEngine.Scripting.Preserve] static public LocService Loc { get; private set; }
    
        #endregion // Accessors

        #region Setup

        static public void AutoSetup(GameObject inRoot)
        {
            s_ServiceCache.AddFromHierarchy(inRoot.transform);
            s_ServiceCache.Process();
        }

        static public void AutoSetup(Scene inScene)
        {
            s_ServiceCache.AddFromScene(inScene);
            s_ServiceCache.Process();
        }

        static public void Deregister(IService inService)
        {
            s_ServiceCache.Remove(inService);
            s_ServiceCache.Process();
        }

        static public void Deregister(Scene inScene)
        {
            s_ServiceCache.RemoveFromScene(inScene);
            s_ServiceCache.Process();
        }

        static public void Shutdown()
        {
            s_ServiceCache.ClearAll();
        }

        static public void Inject(object inObject)
        {
            s_ServiceCache.InjectReferences(inObject);
        }

        #endregion // Setup

        #region All

        static public IEnumerable<IService> All()
        {
            return s_ServiceCache.All<IService>();
        }

        static public IEnumerable<T> All<T>()
        {
            return s_ServiceCache.All<T>();
        }

        static public IEnumerable<ILoadable> AllLoadable()
        {
            return s_ServiceCache.All<ILoadable>();
        }

        #endregion // All
    }
}