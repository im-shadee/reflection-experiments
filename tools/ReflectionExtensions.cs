using System.Reflection;

public static class ReflectionExtensions
{
    private static readonly BindingFlags m_displayFlags = BindingFlags.NonPublic
                                         | BindingFlags.Public
                                         | BindingFlags.Static
                                         | BindingFlags.Instance;
    
    public static void PrintInfo(this Type type, bool showClassInterfaces = false, bool showParent = false)
    {
        PrintTypeInfo(type, showClassInterfaces, showParent);
        Console.WriteLine();
        
        PrintFields(type);
        Console.WriteLine();
        
        PrintMethods(type);
    }
    
    public static void PrintTypeInfo(Type type, bool showClassInterfaces, bool showParent)
    {
        string typeInfo = type.Name;
        
        if (showClassInterfaces)
        {
            string interfaces = GetInterfaces(type, ",");
            if (!string.IsNullOrWhiteSpace(interfaces))
            {
                typeInfo += $"\nImplements: {interfaces}";
            }
        }
        
        if (showParent && type.BaseType != null) typeInfo += $"\nInherits: {type.BaseType.Name}";
            
        Console.WriteLine(typeInfo);
    }
    
    public static void PrintFields(Type type)
    {
        string prefix;
        
        Console.WriteLine("Fields:");
        foreach (FieldInfo field in type.GetFields(bindingAttr: m_displayFlags))
        {
            if (field.IsPublic) prefix = "public ";
            else if (field.IsAssembly) prefix = "internal "; // Shade: According to C# documentation, assembly <=> internal
            else if (field.IsFamily) prefix = "protected "; // Shade: According to C# documentation, family <=> protected
            else prefix = "private ";
            
            string staticStr = field.IsStatic ? "static " : "";

            string suffix = field.FieldType.Name;
            Console.WriteLine($"{prefix}{staticStr}{field.Name}: {suffix}");
        }
    }
    
    public static void PrintMethods(Type type)
    {
        string prefix;
        
        Console.WriteLine("Methods:");
        foreach (MethodInfo method in type.GetMethods(bindingAttr: m_displayFlags | BindingFlags.DeclaredOnly))
        {
            if (method.IsPublic) prefix = "public ";
            else if (method.IsAssembly) prefix = "internal ";
            else if (method.IsFamily) prefix = "protected ";
            else prefix = "private ";

            string staticStr = method.IsStatic ? "static " : "";

            string genericStr = method.IsGenericMethod
                ? $"<{string.Join(", ", method.GetGenericArguments())}>"
                : "";

            IEnumerable<string> parameters = method.GetParameters().Select(p =>
            {
                string modifier = p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "";
                return $"{modifier}{p.ParameterType.Name} {p.Name}";
            });

            string paramList = string.Join(", ", parameters);
            
            Console.WriteLine($"{prefix}{method.ReturnType.Name} {method.Name}({paramList})");
        }
    }
    
    private static string GetInterfaces(Type type, string sep)
    {
        string res = "";
        
        foreach (Type iface in type.GetInterfaces())
        {
            if (!iface.IsInterface) continue;

            string safeSep = string.IsNullOrWhiteSpace(sep) ? string.Empty : sep;
            res += $"{iface.Name}{safeSep} ";
        }

        res = res.Substring(0, (int)MathF.Max(0, res.Length - 2));
        return res;
    }
}
