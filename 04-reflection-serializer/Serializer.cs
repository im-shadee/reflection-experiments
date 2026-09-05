using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.Serializer;

public static class Serializer
{
    private static readonly BindingFlags s_displayFlags = BindingFlags.NonPublic
                                                          | BindingFlags.Public
                                                          | BindingFlags.Static
                                                          | BindingFlags.Instance;

    private static readonly JsonWriterOptions s_options = new() { Indented = true };
    
    /// <summary>
    /// Shade: Maps a display name to a field info.
    /// </summary>
    private struct FieldInfoEntry
    {
        private string m_serializedName;
        private FieldInfo m_fieldInfo;
        
        // Public accessors
        public readonly string SerializedName => m_serializedName;
        public readonly FieldInfo FieldInfo => m_fieldInfo;
        
        public FieldInfoEntry(string name, FieldInfo fieldInfo)
        {
            m_serializedName = name;
            m_fieldInfo = fieldInfo;
        }
    }
    
    // Shade: Cached dictionary (thread safe) to cache serialized fields per type.
    // This avoids re-running heavy reflection steps on recurrent types
    private static readonly ConcurrentDictionary<Type, FieldInfoEntry[]> s_fieldCache = new();
    
    public static void SerializeToJson(object target, Stream stream, [CallerArgumentExpression(nameof(target))] string? rootName = null)
    {
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, s_options);
        
        if (!string.IsNullOrEmpty(rootName))
        {
            writer.WriteStartObject();
            WriteObject(rootName, target, writer);
            writer.WriteEndObject();
        }
        else
        {
            WriteObjectValue(target, writer);
        }
        
        writer.Flush(); // Shade: Ensures all buffered bytes hit the underlying stream
    }

    private static FieldInfoEntry[] BuildValidFieldsArray(Type type)
    {
        List<FieldInfoEntry> validFields = new List<FieldInfoEntry>();

        foreach (FieldInfo field in type.GetFields(s_displayFlags))
        {
            if (!IsFieldValid(field, out string serializedName)) continue;
            
            // Shade: Create and add the entry to the list if valid
            validFields.Add(new FieldInfoEntry(serializedName, field));
        }

        return validFields.ToArray();
    }

    private static bool IsFieldValid(FieldInfo field, out string serializedName)
    {
        // Shade: Defaults to field name. Set custom name through SerializeAsAttribute if the field possesses one
        serializedName = field.Name;
        
        // Shade: Keeps track of whether a field has a [SerializeField] attribute, allowing to serialize
        // public/internal/protected fields
        bool hasSerializeField = false;
        
        foreach (Attribute attribute in field.GetCustomAttributes())
        {
            switch (attribute)
            {
                // Shade: If the field has an ignore attribute, do not serialize it and escape immediately
                case IgnoreAttribute: return false;
                    
                case SerializeFieldAttribute:
                    hasSerializeField = true;
                    break;
                
                // Shade: Get the custom name to serialize this field to if it has one
                case SerializeAsAttribute serializeAsAttribute:
                    serializedName = serializeAsAttribute.SerializedName;
                    break;
            }
        }
        
        // Shade: If the field is private/protected/internal and serialized or public, serialize it, else leave it unchanged
        // Checking either Public or hasSerializeField allows to cover combined access modifiers
        return field.IsPublic || hasSerializeField;
    }

    private static void WriteSerializedValue(string? propertyName, object? value, Utf8JsonWriter writer)
    {
        switch (value)
        {
            case null:
                if (propertyName != null) writer.WriteNull(propertyName);
                else writer.WriteNullValue();
                break;

            case bool boolValue:
                if (propertyName != null) writer.WriteBoolean(propertyName, boolValue);
                else writer.WriteBooleanValue(boolValue);
                break;

            case int intValue:
                if (propertyName != null) writer.WriteNumber(propertyName, intValue);
                else writer.WriteNumberValue(intValue);
                break;
            
            case long longValue:
                if (propertyName != null) writer.WriteNumber(propertyName, longValue);
                else writer.WriteNumberValue(longValue);
                break;

            case float floatValue:
                if (propertyName != null) writer.WriteNumber(propertyName, floatValue);
                else writer.WriteNumberValue(floatValue);
                break;

            case double doubleValue:
                if (propertyName != null) writer.WriteNumber(propertyName, doubleValue);
                else writer.WriteNumberValue(doubleValue);
                break;

            case decimal decimalValue:
                if (propertyName != null) writer.WriteNumber(propertyName, decimalValue);
                else writer.WriteNumberValue(decimalValue);
                break;

            case string stringValue:
                if (propertyName != null) writer.WriteString(propertyName, stringValue);
                else writer.WriteStringValue(stringValue);
                break;
            
            case char charValue:
                if (propertyName != null) writer.WriteString(propertyName, charValue.ToString());
                else writer.WriteStringValue(charValue.ToString());
                break;
            
            case DateTime dateTimeValue:
                if (propertyName != null) writer.WriteString(propertyName, dateTimeValue);
                else writer.WriteStringValue(dateTimeValue);
                break;
            
            case Enum enumValue:
                WriteEnum(propertyName, enumValue, writer);
                break;
            
            case System.Collections.IDictionary dict:
                WriteDict(propertyName, dict, writer);
                break;

            // Shade: Ensure IEnumerable is always ran AFTER string, because string is an IEnumerable<char>
            case System.Collections.IEnumerable enumerable:
                WriteArray(propertyName, enumerable, writer);
                break;
            
            // Shade: Determine what to write using reflection
            default:
                if (propertyName != null) WriteObject(propertyName, value, writer);
                else WriteObjectValue(value, writer);
                break;
        }
    }

    private static void WriteEnum(string? propertyName, Enum value, Utf8JsonWriter writer)
    {
        Type underlying = Enum.GetUnderlyingType(value.GetType());

        // Shade: Handles the ulong case to avoid potential overflows from converting to long only
        if (underlying == typeof(ulong))
        {
            ulong ulongVal = ((IConvertible)value).ToUInt64(null);
            if (propertyName != null) writer.WriteNumber(propertyName, ulongVal);
            else writer.WriteNumberValue(ulongVal);
        }
        else
        {
            long longVal = ((IConvertible)value).ToInt64(null);
            if (propertyName != null) writer.WriteNumber(propertyName, longVal);
            else writer.WriteNumberValue(longVal);
        }
    }

    private static void WriteArray(string? propertyName, System.Collections.IEnumerable enumerable, Utf8JsonWriter writer)
    {
        if (propertyName != null) writer.WriteStartArray(propertyName);
        else writer.WriteStartArray();
        
        foreach (object? item in enumerable)
        {
            // Shade: Inside arrays, elements are written without a property name
            WriteSerializedValue(null, item, writer);
        }
        
        writer.WriteEndArray();
    }

    private static void WriteDict(string? propertyName, System.Collections.IDictionary dict, Utf8JsonWriter writer)
    {
        if (propertyName != null) writer.WriteStartObject(propertyName);
        else writer.WriteStartObject();

        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            string keyStr = entry.Key?.ToString() ?? "null";
            WriteSerializedValue(keyStr, entry.Value, writer);
        }
        
        writer.WriteEndObject();
    }

    private static void WriteTypeFields(object target, Utf8JsonWriter writer)
    {
        Type targetType = target.GetType();

        // Shade: Gets cached fields or builds them atomically if missing to avoid race condition bugs and double lookups
        FieldInfoEntry[] fields = s_fieldCache.GetOrAdd(targetType, BuildValidFieldsArray);

        foreach (FieldInfoEntry field in fields)
        {
            object? fieldValue = field.FieldInfo.GetValue(target);
            
            // Shade: Now, write each value and its name to JSON (recursively)
            WriteSerializedValue(field.SerializedName, fieldValue, writer);
        }
    }

    private static void WriteObject(string name, object target, Utf8JsonWriter writer)
    {
        writer.WriteStartObject(name);
        WriteTypeFields(target, writer);
        writer.WriteEndObject();
    }

    private static void WriteObjectValue(object target, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        WriteTypeFields(target, writer);
        writer.WriteEndObject();
    }
}
