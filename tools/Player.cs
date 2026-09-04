using ReflectionExperiments.Attributes;

[Serialize]
public class Player : IEquatable<Player>
{
    [SerializeField, SerializeAs("m_playerName")]
    private string m_name = "Player1";
    
    [SerializeField]
    private int m_health = 20;
    
    private float m_dmg = 10f;

    public void TakeDamage(float amount) => m_health = (int)MathF.Max(m_health - amount, 0f);

    public void DisplayPlayerInfo()
    {
        Console.WriteLine($"Player: '{m_name}' | Health: {m_health} | Damage: {m_dmg}");
    }

    private void SetName(string name, bool displayChangeInConsole = true)
    {
        if (string.IsNullOrEmpty(name)) return;
        m_name = name;

        if (displayChangeInConsole)
        {
            Console.Write($"New player info:");
            DisplayPlayerInfo();
        }
    }
    
    public bool Equals(Player? other)
    {
        if (other == null) return false;
        
        return this.m_health == other.m_health
               && this.m_dmg.Equals(other.m_dmg);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(m_health, m_dmg);
    }
}
