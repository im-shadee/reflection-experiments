using ReflectionExperiments.Attributes;

namespace ReflectionExperiments.tools;

[Serialize]
public class InventoryTest : IInventoryService
{
    private readonly Dictionary<string, InventoryEntry> m_entries = new Dictionary<string, InventoryEntry>();
    
    public int Add(InventoryEntry? entry, int qty)
    {
        if (entry == null) return 0;
        
        if (m_entries.TryGetValue(entry.Name, out InventoryEntry? existing))
        {
            entry = existing;
        }

        if (entry.Quantity >= entry.MaxQty)
        {
            Console.WriteLine($"InventoryTest: Cannot add item '{entry.Name}'. Stock is maxed out.");
            return qty;
        }

        int total = entry.Quantity + qty;
        int overflow = Math.Max(0, total - entry.MaxQty);
        int newQuantity = Math.Min(total, entry.MaxQty);

        m_entries[entry.Name] = entry with { Quantity = newQuantity };

        Console.WriteLine($"InventoryTest: Added item '{entry.Name}'. Current quantity: {newQuantity}/{entry.MaxQty}. Overflow: {overflow}");
        return overflow;
    }

    public void Remove(string entryName, int qty)
    {
        if (!m_entries.TryGetValue(entryName, out InventoryEntry? entry))
        {
            Console.WriteLine($"InventoryTest: Item '{entryName}' not found.");
            return;
        }

        int actualRemoved = Math.Min(entry.Quantity, qty);
        int newQuantity = entry.Quantity - actualRemoved;

        if (newQuantity <= 0)
        {
            m_entries.Remove(entryName);
            Console.WriteLine($"InventoryTest: Fully depleted and removed '{entryName}'.");
        }
        else
        {
            m_entries[entryName] = entry with { Quantity = newQuantity };
            Console.WriteLine($"InventoryTest: Successfully removed {actualRemoved} of item '{entry.Name}'. Remaining: {newQuantity}");
        }
    }

    public InventoryEntry? Get(string name)
    {
        return m_entries.TryGetValue(name, out InventoryEntry? value) ? value : null;
    }

    public bool TryGet(string name, out InventoryEntry? entry)
    {
        return m_entries.TryGetValue(name, out entry);
    }
}
