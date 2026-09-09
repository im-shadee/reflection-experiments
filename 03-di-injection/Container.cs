using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
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

    public object Resolve(Type type) => ResolveInternal(type, new Stack<Type>());

    private object ResolveInternal(Type type, Stack<Type> resolutionStack)
    {
        // Shade: Resolve registered interface or abstract class mapping
        if (type.IsInterface || type.IsAbstract)
        {
            if (!m_registeredPairs.TryGetValue(type, out Type? registered))
            {
                throw new Exception($"Container@Resolve: type '{type.Name}' is not instantiable and no registered implementation was found.");
            }
            type = registered;
        }
        
        // Shade: Check for circular dependency
        if (resolutionStack.Contains(type))
        {
            string path = string.Join(" -> ", resolutionStack.Reverse().Select(t => t.Name));
            throw new CircularDependencyException($"Circular dependency detected: {path} -> {type.Name}");
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
                .Where(c => c.IsDefined(typeof(InjectAttribute), inherit: true))
                .OrderByDescending(c => c.GetParameters().Length);

            object? resolvedType = BuildObject(type, injectedConstructors, resolutionStack);
            if (resolvedType != null) return resolvedType;

            // Shade: Else, try with regular constructors
            IOrderedEnumerable<ConstructorInfo> normalConstructors = constructors
                .Where(c => !c.IsDefined(typeof(InjectAttribute), inherit: true))
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
                catch (CircularDependencyException)
                {
                    // Shade: Circular dependencies are fatal errors: abort constructor search and bubble up immediately
                    throw;
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
    
    public string GetDependencyGraph<T>() => GetDependencyGraph(typeof(T));

    public string GetDependencyGraph(Type type)
    {
        StringBuilder builder = new StringBuilder();
        BuildDependencyGraphInternal(type, builder, indent: "", isLast: true, activeStack: new Stack<Type>());
        return builder.ToString();
    }

    private void BuildDependencyGraphInternal(
        Type type,
        StringBuilder builder,
        string indent,
        bool isLast,
        Stack<Type> activeStack)
    {
        // Shade: Determine the actual type to instantiate if an interface/abstract class is passed
        Type actualType = type;
        string interfacePrefix = "";

        if (type.IsInterface || type.IsAbstract)
        {
            if (m_registeredPairs.TryGetValue(type, out Type? registered))
            {
                interfacePrefix = $"{type.Name} -> ";
                actualType = registered;
            }
            else
            {
                builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{type.Name} [UNRESOLVED INTERFACE]");
                return;
            }
        }

        // Shade: Node header display line
        string nodeLabel = $"{interfacePrefix}{actualType.Name}";
        builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{nodeLabel}");

        // Shade: Check for circular dependency to avoid infinite recursion
        if (activeStack.Contains(actualType))
        {
            string childIndent = indent + (isLast ? "    " : "│   ");
            builder.AppendLine($"{childIndent}└── [CIRCULAR DEPENDENCY DETECTED]");
            return;
        }

        activeStack.Push(actualType);

        try
        {
            // Shade: Select the constructor using the same priority logic as ResolveInternal
            ConstructorInfo[] constructors = actualType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            ConstructorInfo? targetConstructor = constructors
                                                     .Where(c => c.GetCustomAttribute<InjectAttribute>() != null)
                                                     .OrderByDescending(c => c.GetParameters().Length)
                                                     .FirstOrDefault()
                                                 ?? constructors
                                                     .Where(c => c.GetCustomAttribute<InjectAttribute>() == null)
                                                     .OrderByDescending(c => c.GetParameters().Length)
                                                     .FirstOrDefault();

            if (targetConstructor == null)
            {
                return;
            }

            ParameterInfo[] parameters = targetConstructor.GetParameters();
            string nextIndent = indent + (isLast ? "    " : "│   ");

            for (int i = 0; i < parameters.Length; i++)
            {
                bool lastParam = i == parameters.Length - 1;
                BuildDependencyGraphInternal(parameters[i].ParameterType, builder, nextIndent, lastParam, activeStack);
            }
        }
        finally
        {
            activeStack.Pop();
        }
    }
}
