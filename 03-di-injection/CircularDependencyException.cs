namespace ReflectionExperiments.DIInjection;

public class CircularDependencyException : Exception
{
    public CircularDependencyException(string message) : base(message) { }
}
