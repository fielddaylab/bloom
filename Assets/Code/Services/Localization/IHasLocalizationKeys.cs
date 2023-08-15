using BeauUtil;
using System.Collections.Generic;

namespace Zavala
{
    public interface IHasLocalizationKeys {
        #if UNITY_EDITOR
        IEnumerable<KeyValuePair<StringHash32, string>> GetStrings();
        #endif // UNITY_EDITOR
    }
}