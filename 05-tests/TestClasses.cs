using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.Serializer.Tests;

public enum TestStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}

[Serialize]
public class SimpleModel
{
    public int Id;
    public string Name = string.Empty;
    public bool IsActive;
}

[Serialize]
public class AttributeModel
{
    public string VisibleField = string.Empty;

    [Ignore]
    public string IgnoredField = string.Empty;

    [SerializeAs("custom_name")]
    public string RenamedField = string.Empty;

    [SerializeField]
    private string m_privateField = string.Empty;

    public void SetPrivateField(string val) => m_privateField = val;
    public string GetPrivateField() => m_privateField;
}

[Serialize]
public class ComplexModel
{
    public SimpleModel Inner = new();
    public List<int> Numbers = new();
    public TestStatus Status;
    public Dictionary<TestStatus, string> EnumDict = new();
    public Dictionary<Guid, int> GuidDict = new();
}

[Serialize]
internal class InternalModel
{
    public string InternalData = string.Empty;
}

public class UnserializableModel
{
    public string SecretData = "Should not be seen";
}

[Serialize]
public class WrapperModel
{
    public string AllowedData = string.Empty;
    public UnserializableModel HiddenObject = new(); // Should be skipped
}
