#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Zavala.Debugging;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Services;
using BeauUtil.Tags;
using EasyAssetStreaming;
using ScriptableBake;
using UnityEngine;
using FieldDay.Scripting;

namespace Zavala
{
    public partial class LocService : ServiceBehaviour, ILoadable
    {
        static private readonly FourCC DefaultLanguage = FourCC.Parse("EN");

        #region Inspector

        [SerializeField, Required] private LocManifest m_EnglishManifest;
        [SerializeField] private bool DEBUG_ForceBinary;

        #endregion // Inspector

        [NonSerialized] private LocPackage m_LanguagePackage;

        private Routine m_LoadRoutine;
        private IPool<TagString> m_TagStringPool;

        [NonSerialized] private bool m_Loading;
        [NonSerialized] private FourCC m_CurrentLanguage;
        [NonSerialized] private List<LocText> m_ActiveTexts = new List<LocText>(64);

        public readonly CastableEvent<FourCC> OnLanguageUpdated = new CastableEvent<FourCC>(8);

        #if DEVELOPMENT

        [NonSerialized] private readonly HashSet<StringHash32> m_UsageAudit = Zavala.Collections.NewSet<StringHash32>(1024);

        #endif // DEVELOPMENT
        
        #region Loading

        private IEnumerator InitialLoad()
        {
            yield return LoadLanguage(m_EnglishManifest);
        }

        private IEnumerator LoadLanguage(LocManifest manifest)
        {
            m_Loading = true;
            
            if (m_LanguagePackage == null) {
                m_LanguagePackage = ScriptableObject.CreateInstance<LocPackage>();
                m_LanguagePackage.name = "LanguageStrings";
            }

            m_LanguagePackage.Clear();
            using(Profiling.Time("loading language")) {
                bool loadPackages;
                #if PREVIEW || PRODUCTION
                loadPackages = false;
                #else
                loadPackages = !DEBUG_ForceBinary && manifest.Packages.Length > 0;
                #endif // PREVIEW || PRODUCTION
                if (loadPackages) {
                    Log.Msg("[LocService] Loading '{0}' from {1} packages", manifest.name, manifest.Packages.Length);
                    foreach(var file in manifest.Packages) {
                        var parser = BlockParser.ParseAsync(ref m_LanguagePackage, file, Parsing.Block, LocPackage.Generator.Instance);
                        yield return Async.Schedule(parser, AsyncFlags.HighPriority); 
                    }
                } else {
                    Log.Msg("[LocService] Loading '{0}' from {1:0.00}kb binary", manifest.name, manifest.Binary.Length / 1024f);
                    yield return Async.Schedule(LocPackage.ReadFromBinary(m_LanguagePackage, manifest.Binary), AsyncFlags.HighPriority);
                }
            }

            #if DEVELOPMENT

            m_UsageAudit.Clear();
            foreach(var key in m_LanguagePackage.AllKeys) {
                m_UsageAudit.Add(key);
            }

#endif // DEVELOPMENT

            Log.Msg("[LocService] Loaded {0} keys ({1})", m_LanguagePackage.Count, manifest.LanguageId.ToString());

            m_CurrentLanguage = manifest.LanguageId;
            m_Loading = false;
            DispatchTextRefresh();
        }

        #endregion // Loading

        #region Localization

        public FourCC CurrentLanguageId {
            get { return m_CurrentLanguage; }
        }

        /// <summary>
        /// Localizes the given key.
        /// </summary>
        public string Localize(TextId inKey, bool inbIgnoreEvents = false)
        {
            return Localize(inKey, string.Empty, null, inbIgnoreEvents);
        }

        /// <summary>
        /// Localize the given key.
        /// </summary>
        public string Localize(TextId inKey, StringSlice inDefault, object inContext = null, bool inbIgnoreEvents = false)
        {
            if (m_Loading)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return inDefault.ToString();
            }

            if (inKey.IsEmpty)
                return inDefault.ToString();

            string content;
            bool hasEvents;
            if (!m_LanguagePackage.TryGetContent(inKey, out content))
            {
                if (inDefault.IsEmpty || m_CurrentLanguage != DefaultLanguage)
                {
                    Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}' ({1})", inKey.Source(), inKey.Hash().HashValue);
                }
                content = inDefault.ToString();
                hasEvents = content.IndexOf('{') >= 0;
            }
            else
            {
                #if DEVELOPMENT
                m_UsageAudit.Remove(inKey);
                #endif // DEVELOPMENT

                hasEvents = m_LanguagePackage.HasEvents(inKey);
            }
            
            return content;
        }

        #endregion // Localization

        #region Tagged

        public bool LocalizeTagged(ref TagString ioTagString, TextId inKey, object inContext = null)
        {
            if (ioTagString == null)
                ioTagString = new TagString();
            else
                ioTagString.Clear();

            if (m_Loading)
            {
                Debug.LogErrorFormat("[LocService] Localization is still loading");
                return false;
            }

            if (inKey.IsEmpty)
            {
                return true;
            }

            string content;
            if (!m_LanguagePackage.TryGetContent(inKey, out content))
            {
                Debug.LogErrorFormat("[LocService] Unable to locate entry for '{0}' ({1})", inKey.Source(), inKey.Hash().HashValue);
                return false;
            }

            #if DEVELOPMENT
            m_UsageAudit.Remove(inKey);
#endif // DEVELOPMENT

            ScriptUtility.ParseToTag(ref ioTagString, content, inContext);

            return true;
        }

        #endregion // Tagged

        #region Texts

        public void RegisterText(LocText inText)
        {
            m_ActiveTexts.Add(inText);
        }

        public void DeregisterText(LocText inText)
        {
            m_ActiveTexts.FastRemove(inText);
        }

        private void DispatchTextRefresh()
        {
            for(int i = 0, length = m_ActiveTexts.Count; i < length; i++)
                m_ActiveTexts[i].OnLocalizationRefresh();
            OnLanguageUpdated.Invoke(m_CurrentLanguage);
        }

        #endregion // Texts

        #region IService

        public bool IsLoading()
        {
            return m_LoadRoutine;
        }

        protected override void Initialize()
        {
            m_LoadRoutine.Replace(this, InitialLoad()).TryManuallyUpdate(0);

            m_TagStringPool = new DynamicPool<TagString>(4, Pool.DefaultConstructor<TagString>());
            m_TagStringPool.Prewarm();
        }

        protected override void Shutdown()
        {
            UnityHelper.SafeDestroy(ref m_LanguagePackage);

            base.Shutdown();
        }

        #endregion // IService

        #region IDebuggable

        #endregion // IDebuggable
    }
}