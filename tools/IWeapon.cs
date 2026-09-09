namespace ReflectionExperiments.tools;

public interface IWeapon
{
    /// <summary>
    /// Shade: Broadcasts a notification when this weapon is disposed. Invoke on <see cref="Dispose"/>.
    /// </summary>
    public event Action OnDispose;
    
    /// <summary>
    /// Shade: The weapon's name
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Shade: The entry for this weapon to add to an IInventoryService instance.
    /// </summary>
    public InventoryEntry Entry { get; }
    
    /// <summary>
    /// Shade: Behaviour executing when this weapon is used.
    /// </summary>
    public void Use();
    
    /// <summary>
    /// Shade: Executes behaviour tied to disposing this weapon and invokes <see cref="OnDispose"/>
    /// </summary>
    public void Dispose();
}
