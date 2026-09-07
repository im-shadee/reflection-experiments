using System.Reflection;
using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.DIInjection;

public class Container
{
    public T Resolve<T>() => (T)Resolve(typeof(T));

    public object Resolve(Type type)
    {
        // Shade: Cache all constructors. The system will try each of them in order of priority:
        // - Constructors with [Inject] => biggest to lowest number of params until one works
        // - Else, constructors without [Inject] => biggest to lowest number of params until one works
        // - Otherwise, throw an error; 'T' was unresolvable
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        
        // Shade: Try constructors with [Inject], ordered by parameter count (descending)
        IOrderedEnumerable<ConstructorInfo> injectedConstructors = constructors
            .Where(c => c.GetCustomAttribute<InjectAttribute>() != null)
            .OrderByDescending(c => c.GetParameters().Length);

        object? resolvedType = BuildObject(type, injectedConstructors);
        if (resolvedType != null) return resolvedType;

        // Shade: Else, try with regular constructors
        IOrderedEnumerable<ConstructorInfo> normalConstructors = constructors
            .Where(c => c.GetCustomAttribute<InjectAttribute>() == null)
            .OrderByDescending(c => c.GetParameters().Length);

        resolvedType = BuildObject(type, normalConstructors);
        if (resolvedType != null) return resolvedType;
        
        // Shade: Else, the type could not be resolved. Throw an error
        throw new Exception($"Container@Resolved: Could not resolve type {type.Name}.");
    }

    private object? BuildObject(Type type, IEnumerable<ConstructorInfo> constructors)
    {
        List<object> builtParams = new List<object>();
        
        foreach (ConstructorInfo info in constructors)
        {
            builtParams.Clear();
            bool constructorFailed = false;

            foreach (ParameterInfo parameter in info.GetParameters())
            {
                try
                {
                    // Shade: Recursively resolve the dependency
                    object resolvedParam = Resolve(parameter.ParameterType);
                    builtParams.Add(resolvedParam);
                }
                catch
                {
                    // Shade: If any parameter fails to resolve, this constructor is invalid.
                    // Break out and try the next constructor.
                    constructorFailed = true;
                    break;
                }
            }

            // Shade: If we successfully built all parameters for this constructor, invoke it
            if (!constructorFailed)
            {
                return info.Invoke(builtParams.ToArray());
            }
        }

        return null;
    }
}
