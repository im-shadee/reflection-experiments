using ReflectionExperiments.Attributes;

// Shade: Test mockup player class to test the inspector (invoking private methods, etc.) and serialization
// with custom attributes testing
[Serialize]
public class Player : IEquatable<Player>
{
    /// <summary>
    /// Shade: Serializable structs containing stats for a player to test struct serialization.
    /// </summary>
    [Serialize]
    private struct Stats : IEquatable<Stats>
    {
        [SerializeField]
        private int m_health;
        
        [SerializeField]
        private int m_maxHealth;
        
        [SerializeField]
        private float m_damage;
        
        [SerializeField]
        private float m_defense;
        
        public int Health { get => m_health; set => m_health = (int)Math.Clamp(m_health - value, 0f, m_maxHealth); }
        public int MaxHealth { get => m_maxHealth; set => m_maxHealth = value; }
        public float Damage { get => m_damage; set => m_damage = value; }
        public float Defense { get => m_defense; set => m_defense = value; }

        public Stats(int health, int maxHealth, float damage, float defense)
        {
            m_health = health;
            m_damage = damage;
            m_maxHealth = maxHealth;
            m_defense = defense;
        }

        public bool Equals(Stats other)
        {
            return Health == other.Health 
                   && MaxHealth == other.MaxHealth 
                   && Damage.Equals(other.Damage) 
                   && Defense.Equals(other.Defense);
        }

        public override bool Equals(object? obj) => obj is Stats other && Equals(other);

        public override int GetHashCode()
        {
            return HashCode.Combine(Health, MaxHealth, Damage, Defense);
        }
        
        public static bool operator ==(Stats left, Stats right) => left.Equals(right);
        public static bool operator !=(Stats left, Stats right) => !left.Equals(right);
    }
    
    [SerializeField, SerializeAs("m_playerName")]
    private string m_name = "Player1";

    [SerializeField]
    private Stats m_playerStats = new(10, 10, 15f, 7f);
    
    [SerializeField]
    private List<int> m_myList = new List<int>() {1, 2, 3, 4, 5};

    [SerializeField] private Dictionary<int, int> m_myDict = new Dictionary<int, int>()
    {
        { 1, 1 },
        { 2, 2 },
        { 3, 3 },
        { 4, 4 },
    };
    
    [Ignore]
    public float m_publicFieldToIgnore = 5f;

    public float m_publicFieldToSerialize = 5f;

    public void DisplayPlayerInfo()
    {
        Console.WriteLine(_GetPlayerInfo());
    }

    private string _GetPlayerInfo()
    {
        return $"Player: '{m_name}' | Health: {m_playerStats.Health} | Max Health: {m_playerStats.MaxHealth} " +
               $"| Damage: {m_playerStats.Damage} | Defense: {m_playerStats.Defense}";
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

        return m_name.Equals(other.m_name)
               && m_playerStats == other.m_playerStats;
    }

    public override int GetHashCode() => HashCode.Combine(m_name, m_playerStats);

    public override string ToString() => _GetPlayerInfo();
}
