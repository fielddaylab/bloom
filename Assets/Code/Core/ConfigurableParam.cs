using System;
using System.Collections.Generic;
using System.Reflection;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay;
using UnityEngine.Scripting;
using static UnityEngine.Rendering.DebugUI;

namespace Zavala {
    /// <summary>
    /// Marks a field as a Configurable parameter.
    /// </summary>
    public abstract class ConfigVarAttribute : PreserveAttribute {
        public readonly string DisplayName;

        private string m_DataName;
        private string m_Category;
        private StringHash32 m_DataNameHash;
        private FieldInfo m_Field;
        private Variant m_DefaultValue;

        public string DataName {
            get { return m_DataName; }
        }

        public string Category {
            get { return m_Category; }
        }

        public StringHash32 DataNameHash {
            get { return m_DataNameHash; }
        }

        public Variant DefaultValue {
            get { return m_DefaultValue; }
        }

        public ConfigVarAttribute(string displayName) {
            DisplayName = displayName;
        }

        public virtual void Bind(FieldInfo field) {
            m_Field = field;
            m_Category = field.DeclaringType.Name;
            m_DataName = string.Concat(m_Category, "::", field.Name);
            m_DataNameHash = new StringHash32(m_DataName);
            Variant.TryConvertFrom(field.GetValue(null), out m_DefaultValue);
        }

        public void WriteValue(Variant value) {
            Variant.TryConvertTo(value, m_Field.FieldType, out NonBoxedValue converted);
            m_Field.SetValue(null, converted.AsObject());
        }
    }

    /// <summary>
    /// Marks a static field as a configurable float parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigFloatVar : ConfigVarAttribute {
        public readonly float MinValue;
        public readonly float MaxValue;
        public readonly float Increment;

        public ConfigFloatVar(string name, float minValue = 0, float maxValue = 1, float increment = 0) : base(name) {
            MinValue = minValue;
            MaxValue = maxValue;
            Increment = increment;
        }

        public override void Bind(FieldInfo field) {
            if (field.FieldType != typeof(float)) {
                throw new ArgumentException(string.Format("Field '{0}' is not a float", field.Name));
            }

            base.Bind(field);
        }

        public void WriteValue(float value) {
            base.WriteValue(value);
        }
    }

    /// <summary>
    /// Marks a static field as a configurable integer parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigIntVar : ConfigVarAttribute {
        public readonly int MinValue;
        public readonly int MaxValue;
        public readonly int Increment;

        public ConfigIntVar(string name, int minValue = 0, int maxValue = 100, int increment = 1) : base(name) {
            MinValue = minValue;
            MaxValue = maxValue;
            Increment = increment;
        }

        public override void Bind(FieldInfo field) {
            if (field.FieldType != typeof(int)) {
                throw new ArgumentException(string.Format("Field '{0}' is not an int", field.Name));
            }

            base.Bind(field);
        }

        public void WriteValue(int value) {
            base.WriteValue(value);
        }
    }

    /// <summary>
    /// Set of configurable variables.
    /// </summary>
    public class ConfigVarSet {
        private readonly Dictionary<StringHash32, ConfigVarAttribute> m_VarDefMap = new Dictionary<StringHash32, ConfigVarAttribute>();
        private readonly Dictionary<StringHash32, Variant> m_VarValueMap = new Dictionary<StringHash32, Variant>();

        public void LoadDefinitions() {
            m_VarDefMap.Clear();
            m_VarValueMap.Clear();

            foreach(var pair in Reflect.FindFields<ConfigVarAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                pair.Attribute.Bind(pair.Info);

                m_VarDefMap.Add(pair.Attribute.DataNameHash, pair.Attribute);
                m_VarValueMap.Add(pair.Attribute.DataNameHash, pair.Attribute.DefaultValue);
            }
        }
    }
}