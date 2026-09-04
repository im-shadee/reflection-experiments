using System.Reflection;
using System.Text;
using System.Text.Json;
using reflection_experiments.tools;
using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.Serializer;

public class Serializer
{
    private static readonly BindingFlags m_displayFlags = BindingFlags.NonPublic
                                                          | BindingFlags.Public
                                                          | BindingFlags.Static
                                                          | BindingFlags.Instance;
    
    /*public void Write()
    {
        JsonWriterOptions options = new JsonWriterOptions()
        {
            Indented = true,
        };

        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, options);
        
        writer.WriteStartObject();
        writer.WriteNull("myProp");
        writer.WriteNumber("myProp2", 15);
        writer.WriteBoolean("myBool", true);
        writer.WriteNumber("myEnum", 1);
        writer.WriteEndObject();
        writer.Flush();

        string json = Encoding.UTF8.GetString(stream.ToArray());
        FileWriter fileWriter = new FileWriter();
        fileWriter.WriteAtRoot("myJson", ".json", json);
        Console.WriteLine(json);
    }*/

    public void WriteTest(string content, object target)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Serializer@WriteTest: content is null or blank.");
        }
        
        JsonWriterOptions options = new JsonWriterOptions()
        {
            Indented = true,
        };

        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, options);
        
        // Shade: Write the first bracket
        writer.WriteStartObject();

        // Shade: Write the content
        writer.WritePropertyName(target.GetType().Name);
        writer.WriteRawValue(content, skipInputValidation: true);
        
        writer.WriteEndObject();
        writer.Flush();
        
        string json = Encoding.UTF8.GetString(stream.ToArray());
        FileWriter fileWriter = new FileWriter();
        fileWriter.WriteAtRoot("myJson", ".json", json);
        Console.WriteLine(json);
    }

    public string Serialize(object target)
    {
        JsonWriterOptions options = new JsonWriterOptions()
        {
            Indented = true,
        };

        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, options);
        
        writer.WriteStartObject();
        
        Type targetType = target.GetType();

        foreach (FieldInfo field in targetType.GetFields(m_displayFlags))
        {
            string? serializedField = GetSerializedField(field);
            
            // Shade: If GetSerializedField returns null, the field is not meant to be serialized;
            // don't write it and continue with the next field instead
            if (serializedField == null) continue;
            
            WriteSerializedString(serializedField, field.GetValue(target), writer);
        }
        
        writer.WriteEndObject();
        writer.Flush();
        
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private string? GetSerializedField(FieldInfo field)
    {
        Console.WriteLine($"Inspecting field {field.Name}.");
        
        // Shade: Evaluate to an array once to prevent multiple enumeration
        Attribute[] fieldAttributesEnumerable = field.GetCustomAttributes().ToArray();

        bool hasSerializeField = false;
        SerializeAsAttribute? serializeAs = null;
        
        if (fieldAttributesEnumerable.Length > 0)
        {
            foreach (Attribute attribute in fieldAttributesEnumerable)
            {
                switch (attribute)
                {
                    // Shade: If the field has an ignore attribute, do not serialize it and escape immediately
                    case IgnoreAttribute: return null;
                    
                    case SerializeFieldAttribute:
                        hasSerializeField = true;
                        break;
                    
                    // Shade: Get the custom name to serialize this field to if it has one
                    case SerializeAsAttribute serializeAsAttribute:
                        serializeAs = serializeAsAttribute;
                        break;
                }
            }
        }
        
        // Shade: If the field is private and serialized, serialize it, else leave it unchanged
        if (field.IsPrivate && !hasSerializeField) return null;
        
        // Shade: Set custom name if found, otherwise fall back to field name
        return serializeAs != null ? serializeAs.SerializedName : field.Name;
    }

    private void WriteSerializedString(string serializedFieldName, object? value, Utf8JsonWriter writer)
    {
        switch (value)
        {
            case null:
                writer.WriteNull(serializedFieldName);
                break;
            
            case bool boolValue:
                writer.WriteBoolean(serializedFieldName, boolValue);
                break;
            
            case int intValue:
                writer.WriteNumber(serializedFieldName, intValue);
                break;
            
            case float floatValue:
                writer.WriteNumber(serializedFieldName, floatValue);
                break;
            
            case double doubleValue:
                writer.WriteNumber(serializedFieldName, doubleValue);
                break;
                
            case decimal decimalValue:
                writer.WriteNumber(serializedFieldName, decimalValue);
                break;
            
            case string stringValue:
                writer.WriteString(serializedFieldName, stringValue);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(
                    $"Serializer@WriteSerializedString: value '{value}' is not a valid object to serialize.");
        }
    }
}
