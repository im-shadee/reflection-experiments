using System.Collections;
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

    private static readonly JsonWriterOptions s_writerOptions = new() { Indented = true };
    
    private static readonly JsonReaderOptions s_readerOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
    
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
    
    #region Public API
    public static void SerializeToJson(object target, Stream stream, 
        [CallerArgumentExpression(nameof(target))] string? rootName = null)
    {
        if (string.IsNullOrEmpty(rootName))
        {
            throw new Exception(
                $"Serializer@SerializeToJson: Variable name from object '{target}' could not be retrieved.");
        }
        
        // Shade: Reject root objects that aren't decorated with [Serialize]
        if (!IsTypeSerializable(target.GetType())) return;
        
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, s_writerOptions);
        
        writer.WriteStartObject();
        WriteObject(rootName, target, writer);
        writer.WriteEndObject();
        
        writer.Flush(); // Shade: Ensures all buffered bytes hit the underlying stream
    }

    public static T? DeserializeFromJson<T>(string json, string rootName)
    {
        if (string.IsNullOrEmpty(rootName))
        {
            throw new Exception(
                $"Serializer@SerializeToJson: root name is empty or null. Make sure to pass a valid string, like 'nameof(myVar)'.");
        }
        
        // Shade: Reject root objects that aren't decorated with [Serialize]
        if (!IsTypeSerializable(typeof(T))) return default;
        
        // Shade: Convert JSON string to UTF-8 encoded bytes
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
        Utf8JsonReader reader = new Utf8JsonReader(jsonBytes, s_readerOptions);
        
        // Shade: Advance to the first token in the document
        if (!reader.Read())
        {
            throw new JsonException("Serializer@SerializeToJson: Failed to read JSON stream: payload is empty.");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == rootName)
                {
                    reader.Read(); // Shade: Advance past property name to StartObject '{'
                    break;
                }
                
                reader.Skip();
            }
        }
        
        object? instance = Activator.CreateInstance(typeof(T), nonPublic: true);
        if (instance == null) return (T?)instance;
        
        // Handle root wrapper object if present
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            PopulateObject(ref reader, ref instance);
        }

        return (T?)instance;
    }
    #endregion

    #region Serialization Helpers
    private static bool IsTypeSerializable(Type type)
    {
        // Shade: Interfaces and abstract classes are allowed; concrete implementations will be checked at runtime
        if (type.IsInterface || type.IsAbstract)
        {
            return true;
        }
        
        // Shade: Built-in types, primitives, and strings are naturally serializable
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || 
            type == typeof(decimal) || type == typeof(DateTime)) 
        {
            return true;
        }

        // Shade: BCL collections/arrays are naturally serializable
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return true;
        }

        // Shade: For custom classes/structs, enforce the [Serialize] attribute
        return type.IsDefined(typeof(SerializeAttribute), inherit: false);
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
            
            case IDictionary dict:
                WriteDict(propertyName, dict, writer);
                break;

            // Shade: Ensure IEnumerable is always ran AFTER string, because string is an IEnumerable<char>
            case IEnumerable enumerable:
                WriteArray(propertyName, enumerable, writer);
                break;
            
            // Shade: Determine what to write using reflection
            default:
                // Shade: Skip writing entirely if the custom class/struct lacks the [Serialize] attribute
                if (!IsTypeSerializable(value.GetType())) break;
                
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

    private static void WriteArray(string? propertyName, IEnumerable enumerable, Utf8JsonWriter writer)
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

    private static void WriteDict(string? propertyName, IDictionary dict, Utf8JsonWriter writer)
    {
        if (propertyName != null) writer.WriteStartObject(propertyName);
        else writer.WriteStartObject();

        foreach (DictionaryEntry entry in dict)
        {
            string keyStr = entry.Key?.ToString() ?? "null";
            WriteSerializedValue(keyStr, entry.Value, writer);
        }
        
        writer.WriteEndObject();
    }

    private static void WriteTypeFields(object target, Utf8JsonWriter writer)
    {
        Type targetType = target.GetType();
        
        // Shade: Embed type metadata so polymorphic/interface fields can be re-instantiated
        writer.WriteString("$type", targetType.AssemblyQualifiedName);

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
    #endregion
    
    #region Deserialization Helpers
    private static void PopulateObject(ref Utf8JsonReader reader, ref object target)
    {
        Type targetType = target.GetType();
        FieldInfoEntry[] fields = s_fieldCache.GetOrAdd(targetType, BuildValidFieldsArray);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            
            string? propertyName = reader.GetString();
            if (propertyName == null) continue;
            
            // Shade: Move to the property value
            reader.Read(); 
            
            // Shade: Find matching cached field entry
            FieldInfoEntry? entry = Array.Find(fields, f => f.SerializedName == propertyName);
            if (entry.HasValue)
            {
                object? value = ReadValue(ref reader, entry.Value.FieldInfo?.FieldType);
                
                // Shade: Set the deserialized value on the instance
                entry.Value.FieldInfo?.SetValue(target, value);
            }
            else
            {
                // Shade: Skip unmapped properties or unknown objects/arrays
                reader.Skip();
            }
        }
    }
    
    private static object? ReadValue(ref Utf8JsonReader reader, Type? targetType)
    {
        if (targetType == null) return null;
        
        // Shade: Handle JSON null tokens first
        if (reader.TokenType == JsonTokenType.Null) return null;
        
        // Shade: If targetType is a Nullable type, unwrap it (e.g., Nullable<int> => int)
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Shade: Handle enums explicitly
        if (underlyingType.IsEnum)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                long longVal = reader.GetInt64();
                return Enum.ToObject(underlyingType, longVal);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? strVal = reader.GetString();
                return strVal == null ? null : Enum.Parse(underlyingType, strVal, true);
            }
        }
        
        return reader.TokenType switch
        {
            // Shade: String-formatted types
            JsonTokenType.String when underlyingType == typeof(char) => reader.GetString()?[0],
            JsonTokenType.String when underlyingType == typeof(DateTime) => reader.GetDateTime(),
            JsonTokenType.String => reader.GetString(),
            
            // Shade: Number types
            JsonTokenType.Number when underlyingType == typeof(int) => reader.GetInt32(),
            JsonTokenType.Number when underlyingType == typeof(float) => reader.GetSingle(),
            JsonTokenType.Number when underlyingType == typeof(double) => reader.GetDouble(),
            JsonTokenType.Number when underlyingType == typeof(decimal) => reader.GetDecimal(),
            JsonTokenType.Number when underlyingType == typeof(long) => reader.GetInt64(),
            
            // Shade: Booleans
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            
            // Shade: Complex types
            JsonTokenType.StartObject when typeof(IDictionary).IsAssignableFrom(underlyingType) => ReadDictionary(ref reader, underlyingType),
            JsonTokenType.StartObject => ReadNestedObject(ref reader, underlyingType),
            JsonTokenType.StartArray => ReadArray(ref reader, underlyingType),
            
            _ => null
        };
    }

    private static object? ReadArray(ref Utf8JsonReader reader, Type targetType)
    {
        // Shade: Determine element type (e.g., List<int> => int)
        Type? elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments()[0];
        if (elementType == null) return null;

        Type listType = typeof(List<>).MakeGenericType(elementType);

        IList? list = (IList?)Activator.CreateInstance(listType, nonPublic: true);
        if (list == null) return null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            object? value = ReadValue(ref reader, elementType);
            list.Add(value);
        }

        if (targetType.IsArray)
        {
            Array array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        
        return list;
    }

    private static object? ReadDictionary(ref Utf8JsonReader reader, Type targetType)
    {
        Type[] genericArgs = targetType.GetGenericArguments();
        
        // Shade: Ensure type has exactly 2 generic args (key/value)
        if (genericArgs.Length != 2) return null;
        
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];
        
        IDictionary? dictionary = (IDictionary?)Activator.CreateInstance(targetType, nonPublic: true);
        if (dictionary == null) return null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string? keyStr = reader.GetString();
            if (keyStr == null) continue;
            
            // Shade: Move to the property value
            reader.Read();
            
            object? key = ReflectionTools.ConvertTo(keyType, keyStr);
            object? value = ReadValue(ref reader, valueType);
            
            if (key != null)
            {
                dictionary.Add(key, value);
            }
        }
        
        return dictionary;
    }

    private static object? ReadNestedObject(ref Utf8JsonReader reader, Type targetType)
    {
        // Shade: Clone reader to peek inside for a "$type" property without advancing the outer reader position
        Utf8JsonReader typeReader = reader;
        Type resolvedType = targetType;
        
        if (typeReader.TokenType == JsonTokenType.StartObject)
        {
            while (typeReader.Read() && typeReader.TokenType != JsonTokenType.EndObject)
            {
                if (typeReader.TokenType == JsonTokenType.PropertyName && typeReader.GetString() == "$type")
                {
                    typeReader.Read();
                    string? typeName = typeReader.GetString();
                
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        Type? concreteType = Type.GetType(typeName);
                        if (concreteType != null)
                        {
                            resolvedType = concreteType;
                        }
                    }
                    break;
                }
                typeReader.Skip();
            }
        }
        
        // Shade: If it lacks [Serialize], skip the JSON block entirely
        if (!IsTypeSerializable(resolvedType))
        {
            reader.Skip(); 
            return null;
        }
        
        // Shade: Instantiate a new object/struct of targetType and populate it
        object? instance = Activator.CreateInstance(resolvedType, nonPublic: true);
        if (instance == null) return null;
        
        PopulateObject(ref reader, ref instance);
        return instance;
    }
    #endregion
}
