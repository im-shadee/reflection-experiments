using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.Serializer.Tests;

public enum TestStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}

public class SimpleModel
{
    public int Id;
    public string Name = string.Empty;
    public bool IsActive;
}

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

public class ComplexModel
{
    public SimpleModel Inner = new();
    public List<int> Numbers = new();
    public TestStatus Status;
    public Dictionary<TestStatus, string> EnumDict = new();
    public Dictionary<Guid, int> GuidDict = new();
}
