using System.Collections.Concurrent;
using System.Reflection;
using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.DIInjection;

public class Container
{
    private readonly ConcurrentDictionary<Type, Type> m_registeredPairs = new ConcurrentDictionary<Type, Type>();

    public void Register<I, T>() where T : I
    {
        Type interfaceType = typeof(I);
        Type implementationType = typeof(T);
        
        // Shade: Create an entry if the key is not present in the dictionary yet
        if (!m_registeredPairs.TryAdd(interfaceType, implementationType))
        {
            // Shade: Else update the value
            m_registeredPairs[interfaceType] = implementationType;   
        }
    }
    
    public T Resolve<T>() => (T)Resolve(typeof(T));

    public object Resolve(Type type)
    {
        bool isInstantiable = type.IsInterface || type.IsAbstract;
        
        if (!isInstantiable)
        {
            return ResolveInternal(type, new Stack<Type>());
        }

        // Shade: If the type is an interface, try to get a registered implementation, else throw
        return m_registeredPairs.TryGetValue(type, out Type? value) 
            ? ResolveInternal(value, new Stack<Type>()) 
            : throw new Exception($"Container@Resolve: type '{type.Name}' is not instantiable and no registered implementation was found.");
    }

    private object ResolveInternal(Type type, Stack<Type> resolutionStack)
    {
        // Shade: Check for circular dependency
        if (resolutionStack.Contains(type))
        {
            string path = string.Join(" -> ", resolutionStack.Reverse().Select(t => t.Name));
            throw new Exception($"Circular dependency detected: {path} -> {type.Name}");
        }
        
        // Shade: Push current type onto the active path stack
        resolutionStack.Push(type);
        
        // Shade: Cache all constructors. The system will try each of them in order of priority:
        // - Constructors with [Inject] => biggest to lowest number of params until one works
        // - Else, constructors without [Inject] => biggest to lowest number of params until one works
        // - Otherwise, throw an error; 'T' was unresolvable
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        try
        {
            // Shade: Try constructors with [Inject], ordered by parameter count (descending)
            IOrderedEnumerable<ConstructorInfo> injectedConstructors = constructors
                .Where(c => c.GetCustomAttribute<InjectAttribute>() != null)
                .OrderByDescending(c => c.GetParameters().Length);

            object? resolvedType = BuildObject(type, injectedConstructors, resolutionStack);
            if (resolvedType != null) return resolvedType;

            // Shade: Else, try with regular constructors
            IOrderedEnumerable<ConstructorInfo> normalConstructors = constructors
                .Where(c => c.GetCustomAttribute<InjectAttribute>() == null)
                .OrderByDescending(c => c.GetParameters().Length);

            resolvedType = BuildObject(type, normalConstructors, resolutionStack);
            
            // Shade: Return the resolved type if non-null else throw
            return resolvedType ?? throw new Exception($"Container@Resolved: Could not resolve type {type.Name}.");
        }
        finally
        {
            // Shade:Pop the type off the stack when leaving this scope so sibling branches can safely reuse
            // dependencies (preventing false positives).
            resolutionStack.Pop();
        }
    }

    private object? BuildObject(Type type, IEnumerable<ConstructorInfo> constructors, Stack<Type> resolutionStack)
    {
        List<object> builtParams = new List<object>();
        
        foreach (ConstructorInfo info in constructors)
        {
            builtParams.Clear();
            bool constructorFailed = false;

            foreach (ParameterInfo parameter in info.GetParameters())
            {
                Type paramType = parameter.ParameterType;

                try
                {
                    // Shade: Recursively resolve the dependency
                    object resolvedParam = ResolveInternal(paramType, resolutionStack);
                    builtParams.Add(resolvedParam);
                }
                catch (Exception)
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
