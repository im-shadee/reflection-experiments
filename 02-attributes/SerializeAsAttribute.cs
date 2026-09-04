namespace ReflectionExperiments.Attributes;

/// <summary>
/// Shade: Attribute that serializes a field to a certain name on disk
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SerializeAsAttribute : Attribute
{
    private string m_serializedName;
    public string SerializedName => m_serializedName;

    public SerializeAsAttribute(string name)
    {
        m_serializedName = name;
    }
}
