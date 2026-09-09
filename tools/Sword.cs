using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.tools;

[Serialize]
public class Sword : IWeapon
{
    public event Action OnDispose;
    public string Name => "Sword";
    
    private InventoryEntry m_entry = new InventoryEntry("Sword", 1, 1);
    public InventoryEntry Entry => m_entry;

    public void Use()
    {
        Console.WriteLine($"Swung {Name}");
    }

    public void Dispose()
    {
        Console.WriteLine($"Disposing {Name}");
        OnDispose?.Invoke();
    }
}
