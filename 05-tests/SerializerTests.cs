using System.Text;
using NUnit.Framework;
using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.Serializer.Tests;

[TestFixture]
public class SerializerTests
{
    private static bool ShouldLogDebug => 
        Environment.GetEnvironmentVariable("LOG_DEBUG") == "true";
    
    [Serialize]
    private class PrivateModel
    {
        public string PrivateData = string.Empty;
    }

    [Serialize]
    protected class ProtectedModel
    {
        public string ProtectedData = string.Empty;
    }
    
    private static void Log(string message)
    {
        if (ShouldLogDebug)
        {
            Console.WriteLine(message);
        }
    }
    
    private static string SerializeToString(object obj, string rootName)
    {
        using MemoryStream ms = new MemoryStream();
        Serializer.SerializeToJson(obj, ms, rootName);
        
        string json = Encoding.UTF8.GetString(ms.ToArray());

        Log($"--- [SERIALIZED JSON] ({rootName}) ---");
        Log(json);
        
        return json;
    }

    [Test]
    public void SerializeToJson_SimpleObject_SerializesCorrectly()
    {
        SimpleModel model = new SimpleModel { Id = 42, Name = "Test", IsActive = true };

        string json = SerializeToString(model, nameof(model));

        Assert.That(json, Does.Contain("\"model\":"));
        Assert.That(json, Does.Contain("\"Id\": 42"));
        Assert.That(json, Does.Contain("\"Name\": \"Test\""));
        Assert.That(json, Does.Contain("\"IsActive\": true"));
    }

    [Test]
    public void DeserializeFromJson_SimpleObject_DeserializesCorrectly()
    {
        string json = """
        {
          "model": {
            "Id": 100,
            "Name": "Deserialized",
            "IsActive": false
          }
        }
        """;

        SimpleModel? result = Serializer.DeserializeFromJson<SimpleModel>(json, "model");
        
        Log($"[AFTER DESERIALIZATION] Id: {result?.Id}, Name: {result?.Name}, IsActive: {result?.IsActive}");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(100));
        Assert.That(result.Name, Is.EqualTo("Deserialized"));
        Assert.That(result.IsActive, Is.False);
    }

    [Test]
    public void Attributes_Ignore_SerializeAs_and_SerializeField_WorkAsExpected()
    {
        AttributeModel model = new AttributeModel
        {
            VisibleField = "Visible",
            IgnoredField = "Secret",
            RenamedField = "RenamedValue"
        };
        model.SetPrivateField("PrivateValue");
        
        string json = SerializeToString(model, "data");
        
        Assert.That(json, Does.Contain("\"VisibleField\": \"Visible\""));
        Assert.That(json, Does.Not.Contain("IgnoredField"));
        Assert.That(json, Does.Contain("\"custom_name\": \"RenamedValue\""));
        Assert.That(json, Does.Contain("\"m_privateField\": \"PrivateValue\""));

        AttributeModel? deserialized = Serializer.DeserializeFromJson<AttributeModel>(json, "data");
        
        Log($"[DESERIALIZED OBJECT] Visible: '{deserialized?.VisibleField}', Ignored: '{deserialized?.IgnoredField}', Renamed: '{deserialized?.RenamedField}', Private: '{deserialized?.GetPrivateField()}'");
        
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.VisibleField, Is.EqualTo("Visible"));
        Assert.That(deserialized.IgnoredField, Is.EqualTo(string.Empty));
        Assert.That(deserialized.RenamedField, Is.EqualTo("RenamedValue"));
        Assert.That(deserialized.GetPrivateField(), Is.EqualTo("PrivateValue"));
    }

    [Test]
    public void Dictionary_WithEnumAndGuidKeys_DeserializesCorrectly()
    {
        Guid guid1 = Guid.NewGuid();
        string json = $$"""
        {
          "root": {
            "Inner": {
              "Id": 1,
              "Name": "Nested",
              "IsActive": true
            },
            "Numbers": [10, 20, 30],
            "Status": 1,
            "EnumDict": {
              "Active": "User is active",
              "Pending": "User is pending"
            },
            "GuidDict": {
              "{{guid1}}": 999
            }
          }
        }
        """;

        Log("--- [INPUT JSON] ---");
        Log(json);

        ComplexModel? result = Serializer.DeserializeFromJson<ComplexModel>(json, "root");

        Log($"[DESERIALIZED COMPLEX] Inner.Id: {result?.Inner.Id}, EnumDict Count: {result?.EnumDict.Count}, GuidDict Count: {result?.GuidDict.Count}");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Inner.Id, Is.EqualTo(1));
        Assert.That(result.Numbers, Has.Count.EqualTo(3));
        Assert.That(result.Numbers[1], Is.EqualTo(20));
        Assert.That(result.Status, Is.EqualTo(TestStatus.Active));

        // Shade: Verify Enum Dictionary Keys
        Assert.That(result.EnumDict.ContainsKey(TestStatus.Active), Is.True);
        Assert.That(result.EnumDict[TestStatus.Active], Is.EqualTo("User is active"));

        // Shade: Verify Guid Dictionary Keys
        Assert.That(result.GuidDict.ContainsKey(guid1), Is.True);
        Assert.That(result.GuidDict[guid1], Is.EqualTo(999));
    }

    [Test]
    public void Serialize_And_Deserialize_NullValues_HandledGracefully()
    {
        SimpleModel model = new SimpleModel { Id = 1, Name = null!, IsActive = false };

        string json = SerializeToString(model, "model");
        Assert.That(json, Does.Contain("\"Name\": null"));

        SimpleModel? result = Serializer.DeserializeFromJson<SimpleModel>(json, "model");

        Log($"[DESERIALIZED NULL MODEL] Id: {result?.Id}, Name: {result?.Name ?? "null"}");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.Null);
    }
    
    [Test]
    public void NonPublicClasses_WithSerializeAttribute_SerializeAndDeserializeCorrectly()
    {
        InternalModel internalModel = new() { InternalData = "Internal" };
        PrivateModel privateModel = new() { PrivateData = "Private" };
        ProtectedModel protectedModel = new() { ProtectedData = "Protected" };

        string internalJson = SerializeToString(internalModel, "internal");
        string privateJson = SerializeToString(privateModel, "private");
        string protectedJson = SerializeToString(protectedModel, "protected");

        Assert.That(internalJson, Does.Contain("\"InternalData\": \"Internal\""));
        Assert.That(privateJson, Does.Contain("\"PrivateData\": \"Private\""));
        Assert.That(protectedJson, Does.Contain("\"ProtectedData\": \"Protected\""));

        InternalModel? desInternal = Serializer.DeserializeFromJson<InternalModel>(internalJson, "internal");
        PrivateModel? desPrivate = Serializer.DeserializeFromJson<PrivateModel>(privateJson, "private");
        ProtectedModel? desProtected = Serializer.DeserializeFromJson<ProtectedModel>(protectedJson, "protected");

        Assert.That(desInternal?.InternalData, Is.EqualTo("Internal"));
        Assert.That(desPrivate?.PrivateData, Is.EqualTo("Private"));
        Assert.That(desProtected?.ProtectedData, Is.EqualTo("Protected"));
    }

    [Test]
    public void RootClass_WithoutSerializeAttribute_HandledGracefully()
    {
        UnserializableModel model = new();

        // Shade: Should not throw, should early return
        string json = SerializeToString(model, "model");
        Assert.That(json, Is.Empty);

        // Shade: Should return default instead of throwing
        UnserializableModel? result = Serializer.DeserializeFromJson<UnserializableModel>(json, "model");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NestedClass_WithoutSerializeAttribute_IsIgnored()
    {
        WrapperModel wrapper = new WrapperModel
        {
            AllowedData = "Public Info",
            HiddenObject = new UnserializableModel { SecretData = "Super Secret" }
        };

        string json = SerializeToString(wrapper, "wrapper");
        
        Log($"[WRAPPER JSON] {json}");

        // The primitive string field should be serialized
        Assert.That(json, Does.Contain("\"AllowedData\": \"Public Info\""));
        
        // The nested object without [Serialize] should be completely skipped
        Assert.That(json, Does.Not.Contain("HiddenObject"));
        Assert.That(json, Does.Not.Contain("SecretData"));
        Assert.That(json, Does.Not.Contain("Super Secret"));
    }
}
