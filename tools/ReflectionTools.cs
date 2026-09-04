using System.ComponentModel;

public static class ReflectionTools
{
    public static object? ConvertTo(Type targetType, string input)
    {
        // Shade: Throw exception if input is blank or null
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException(
                $"ReflectionTools@ConvertTo: Argument '{input.GetType().Name}' is empty or null. " +
                $"Cannot cast to any valid type."
            );
        }
        
        // Shade: Handles "null" string case
        // Return null if the target type is a class or a nullable
        if (input.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
            {
                return null;
            }

            throw new InvalidCastException("ReflectionTools@ConvertTo: 'null' is not a valid input for non-nullable types.");
        }
        
        // Shade: Handles nullable types (e.g. int?)
        // Get the underlying type (in that case, int) to cast to the correct type
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        // Shade: Cast to enum if the underlying type is an enum
        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, input, ignoreCase: true);
        }
        
        // Shade: Finally, use basic convertor for primitive types
        try
        {
            TypeConverter converter = TypeDescriptor.GetConverter(underlyingType);
            return converter.ConvertFromString(input);
        }
        catch
        {
            // Shade: Default to standard .NET convertor
            return Convert.ChangeType(input, underlyingType);
        }
    }
}