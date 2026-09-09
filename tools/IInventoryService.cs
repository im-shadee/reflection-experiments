namespace ReflectionExperiments.tools;

/// <summary>
/// Shade: Inventory entry with a name and a quantity.
/// </summary>
public record InventoryEntry(string Name, int Quantity, int MaxQty)
{
    public override string ToString() => $"InventoryEntry '{Name}' ({Quantity}/{MaxQty})";
}

public interface IInventoryService
{
    /// <summary>
    /// Shade: Adds a quantity <paramref name="qty"/> of <paramref name="entry"/> to the inventory.  
    /// </summary>
    /// <param name="entry">The inventory entry to add to the collection of entries</param>
    /// <param name="qty">The amount to add</param>
    /// <returns>The overflow caused by adding item over the quantity allowed by the entry</returns>
    public int Add(InventoryEntry entry, int qty);
    
    /// <summary>
    /// Shade: Removes a quantity <paramref name="qty"/> of entry with name <paramref name="name"/> from the inventory.
    /// </summary>
    /// <param name="name">The name of the entry to remove</param>
    /// <param name="qty">The quantity to remove</param>
    public void Remove(string name, int qty);
    
    /// <summary>
    /// Shade: Tries getting the <see cref="InventoryEntry"/> tied to <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name associated to the entry to retrieve</param>
    /// <returns>
    /// The instance of <see cref="InventoryEntry"/> associated with <paramref name="name"/> if found, null otherwise
    /// </returns>
    public InventoryEntry? Get(string name);
}
