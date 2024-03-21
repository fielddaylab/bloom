using System;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Services;
using Leaf.Runtime;
using UnityEngine;

namespace Zavala
{
    static public class Loc
    {
        [ServiceReference, UnityEngine.Scripting.Preserve] static private LocService Service;

        static private void Process(LocService inLoc, ref object ioObject)
        {
            if (ioObject is TextId)
                ioObject = inLoc.Localize((TextId) ioObject, true);
            else if (ioObject is StringHash32)
                ioObject = inLoc.Localize((TextId) (StringHash32) ioObject, true);
        }

        [MethodImpl(256)]
        static public string Find(TextId inText)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && EditorLoc != null)
                return EditorLoc(inText);
            #endif // UNITY_EDITOR
            return Service.Localize(inText);
        }

        [MethodImpl(256)]
        static public string Find(TextId inText, object inContext)
        {
            return Service.Localize(inText, null, inContext);
        }

        static public string Format(TextId inText, object inArg0)
        {
            var locService = Service;
            string format = locService.Localize(inText);
            Process(locService, ref inArg0);
            return string.Format(format, inArg0);
        }

        static public string Format(TextId inText, object inArg0, object inArg1)
        {
            var locService = Service;
            string format = locService.Localize(inText);
            Process(locService, ref inArg0);
            Process(locService, ref inArg1);
            return string.Format(format, inArg0, inArg1);
        }

        static public string Format(TextId inText, object inArg0, object inArg1, object inArg2)
        {
            var locService = Service;
            string format = locService.Localize(inText);
            Process(locService, ref inArg0);
            Process(locService, ref inArg1);
            Process(locService, ref inArg2);
            return string.Format(format, inArg0, inArg1, inArg2);
        }

        static public string Format(TextId inText, params object[] inArgs)
        {
            var locService = Service;
            string format = locService.Localize(inText);
            if (inArgs != null)
            {
                for(int i = 0; i < inArgs.Length; i++)
                    Process(locService, ref inArgs[i]);
            }
            return string.Format(format, inArgs);
        }

        static public string FormatFromString(string inFormat, object inArg0)
        {
            var locService = Service;
            Process(locService, ref inArg0);
            return string.Format(inFormat, inArg0);
        }

        static public string FormatFromString(string inFormat, object inArg0, object inArg1)
        {
            var locService = Service;
            Process(locService, ref inArg0);
            Process(locService, ref inArg1);
            return string.Format(inFormat, inArg0, inArg1);
        }

        static public string FormatFromString(string inFormat, object inArg0, object inArg1, object inArg2)
        {
            var locService = Service;
            Process(locService, ref inArg0);
            Process(locService, ref inArg1);
            Process(locService, ref inArg2);
            return string.Format(inFormat, inArg0, inArg1, inArg2);
        }

        static public string FormatFromString(string inFormat, params object[] inArgs)
        {
            var locService = Service;
            if (inArgs != null)
            {
                for(int i = 0; i < inArgs.Length; i++)
                    Process(locService, ref inArgs[i]);
            }
            return string.Format(inFormat, inArgs);
        }

        #if UNITY_EDITOR

        public delegate string EditorLocCallback(TextId inTextId);
        static public EditorLocCallback EditorLoc;

        #endif // UNITY_EDITOR

        static public bool IsServiceLoading() {
            return !Service.IsLoaded();
        }
    }
}