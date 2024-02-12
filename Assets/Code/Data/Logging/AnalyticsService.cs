#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauUtil;
using BeauUtil.Services;
using FieldDay;
using UnityEngine;
using System;
using BeauUtil.Debugger;

namespace Zavala.Data {
    public class AnalyticsService : ServiceBehaviour, IService {

        #region Inspector

        [SerializeField, Required] private string m_AppId = "ZAVALA";
        [SerializeField, Required] private string m_AppVersion = "1.0";
        // FirebaseConsts is present in other AnalyticsServices but isn't in FieldDay here?
        [SerializeField] private FirebaseConsts m_Firebase = default;

        #endregion // Inspector

        #region Logging Variables

        private OGDLog m_Log;
        [NonSerialized] private bool m_Debug;

        [NonSerialized] private string m_CurrentRegionId;
        [NonSerialized] private int m_CurrentBudget;
        // current policies
        // dialogue character opened (or none)
        // dialogue phrase displaying (or none)


        #endregion // Logging Variables

        #region IService

        protected override void Initialize() {


            // TODO: register the events for data logging


            #if DEVELOPMENT
                m_Debug = true;
            #endif // DEVELOPMENT

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                // TODO: ClientLogVersion = ?
            });
            m_Log.UseFirebase(m_Firebase);
            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode) {
            Log.Msg("[Analytics] Setting user code: " + userCode);
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                AppBranch = BuildInfo.Branch()
                // TODO: ClientLogVersion = ?
            });
        }

        protected override void Shutdown() {
            Game.Events?.DeregisterAllForContext(this);
            m_Log.Dispose();
        }
        #endregion // IService
    }

}

