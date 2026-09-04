namespace ReflectionExperiments.Attributes;

/// <summary>
/// Shade: Attributes that target a field, allowing it not to get serialized
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class IgnoreAttribute : Attribute { }
