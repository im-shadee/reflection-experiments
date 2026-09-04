using System.Reflection;

class Test
{
    private BindingFlags m_flags = BindingFlags.NonPublic
                                   | BindingFlags.Public
                                   | BindingFlags.Static
                                   | BindingFlags.Instance;
    
    public void Test1()
    {
        // Shade: Straightforward; typeof returns a Type, type has a name attribute that we print
        Type type = typeof(Player);
        Console.WriteLine(type.Name);
        Console.WriteLine(type); // Shade: Returns namespace + class name (same as FullName)
        Console.WriteLine(type.FullName);
        
        // Shade: Get fields alone returns nothing (because all variables in Player are private).
        // Hypothesis: If I add a public variable, it will print
        // After test: GetFields gets public fields by default
        // Test: Use binding flags to get private fields
        // Result: BindingFlags.NonPublic (or instance) alone is not enough. Combining it with Instance works. Why?
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            Console.WriteLine(field.Name);
        }

        // Shade: GetMethods returns the methods; straightforward
        // The method type is MethodInfo
        // Test: Will GetMethods get private methods by default? How about static?
        // Results: No; I think I need to add binding flags for each type of method/field I want
        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        foreach (MethodInfo method in methods)
        {
            Console.WriteLine(method.Name);
        }
        
        // Shade: I wonder if I can filter out methods to only keep methods from the class
        // Looks like that's what BindingFlags.DeclaredOnly does
        MethodInfo[] methods2 = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        foreach (MethodInfo method in methods2)
        {
            Console.WriteLine(method.Name);
        }

        return;
    }

    public void DisplayClassInfo(string className, string namespaceName = "", bool showClassInterfaces = false, bool showParent = false)
    {
        Type? type = null;

        
        
        PrintTypeInfo(type, showClassInterfaces, showParent);
        Console.WriteLine();
        
        PrintFields(type);
        Console.WriteLine();
        
        PrintMethods(type);
    }

    private bool TryGetTypeFromName(out Type? type, string className, string namespaceName = "")
    {
        type = null;

        try
        {
            string fullClassName = string.IsNullOrWhiteSpace(namespaceName) ? "" : namespaceName + '.';
            fullClassName += className;

            type = Type.GetType(fullClassName, throwOnError: true, ignoreCase: true);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private void PrintTypeInfo(Type? type, bool showClassInterfaces, bool showParent)
    {
        if (type == null) return;
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

    public static string GetInterfaces(Type type, string sep)
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

    private void PrintFields(Type? type)
    {
        if (type == null) return;
        string prefix = "";
        
        Console.WriteLine("Fields:");
        foreach (FieldInfo field in type.GetFields(bindingAttr: m_flags))
        {
            if (field.IsPublic) prefix = "public ";
            else prefix = "private ";

            string suffix = field.FieldType.Name;
            Console.WriteLine($"{prefix}{field.Name}: {suffix}");
        }
    }

    private void PrintMethods(Type? type)
    {
        if (type == null) return;
        
        Console.WriteLine("Methods:");
        foreach (MethodInfo methodInfo in type.GetMethods(bindingAttr: m_flags | BindingFlags.DeclaredOnly))
        {
            Console.WriteLine(methodInfo.Name);
        }
    }
    
    /*static void Main(string[] args)
    {
        Program program = new Program();

        program.DisplayClassInfo("Player", showClassInterfaces: true, showParent: true);
        program.SetField("Player", "", "aaa", 15);
        program.SetField("Player", "", "_health", 15);
    }
    */
}
