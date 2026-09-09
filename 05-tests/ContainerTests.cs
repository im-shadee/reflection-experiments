using NUnit.Framework;
using ReflectionExperiments.Attributes;
using ReflectionExperiments.tools;

namespace ReflectionExperiments.DIInjection.Tests;

[TestFixture]
public class ContainerTests
{
    private Container m_container;

    [SetUp]
    public void SetUp()
    {
        m_container = new Container();
    }

    #region Basic Registration & Resolution Tests

    [Test]
    public void Resolve_RegisteredInterface_ReturnsImplementation()
    {
        m_container.Register<IInventoryService, InventoryTest>();

        IInventoryService resolved = m_container.Resolve<IInventoryService>();

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved, Is.TypeOf<InventoryTest>());
    }

    [Test]
    public void Register_OverwritesPreviousRegistration()
    {
        m_container.Register<IInventoryService, InventoryTest>();
        m_container.Register<IInventoryService, MockInventoryService>();

        IInventoryService resolved = m_container.Resolve<IInventoryService>();

        Assert.That(resolved, Is.TypeOf<MockInventoryService>());
    }

    [Test]
    public void Resolve_ConcreteTypeWithoutRegistration_InstantiatesSuccessfully()
    {
        // Shade: Concrete types with parameterless constructors don't require registration
        Sword sword = m_container.Resolve<Sword>();

        Assert.That(sword, Is.Not.Null);
        Assert.That(sword.Name, Is.EqualTo("Sword"));
    }

    [Test]
    public void Resolve_UnregisteredInterface_ThrowsException()
    {
        Exception? ex = Assert.Throws<Exception>(() => m_container.Resolve<IInventoryService>());
        Assert.That(ex!.Message, Contains.Substring("not instantiable and no registered implementation was found"));
    }

    #endregion

    #region Constructor Selection & Injection Priority Tests

    [Test]
    public void Resolve_PrefersInjectedConstructorOverNormalConstructor()
    {
        m_container.Register<IInventoryService, InventoryTest>();
        m_container.Register<IWeapon, Sword>();

        Player player = m_container.Resolve<Player>();

        Assert.That(player, Is.Not.Null);
        // Shade: Player should have been created using the constructor with [Inject]
    }

    [Test]
    public void Resolve_MultipleInjectedConstructors_SelectsMostParameters()
    {
        m_container.Register<IServiceA, ServiceA>();
        m_container.Register<IServiceB, ServiceB>();

        MultipleInjectConstructors target = m_container.Resolve<MultipleInjectConstructors>();

        Assert.That(target.ResolvedVia, Is.EqualTo("Inject_TwoParams"));
    }

    [Test]
    public void Resolve_InjectedConstructorFails_FallsBackToSmallerInjectedConstructor()
    {
        // Shade: ServiceB is NOT registered, so the 2-parameter [Inject] constructor will fail
        m_container.Register<IServiceA, ServiceA>();

        MultipleInjectConstructors target = m_container.Resolve<MultipleInjectConstructors>();

        Assert.That(target.ResolvedVia, Is.EqualTo("Inject_OneParam"));
    }

    [Test]
    public void Resolve_AllInjectedConstructorsFail_FallsBackToNormalConstructor()
    {
        // Shade: Neither ServiceA nor ServiceB registered -> [Inject] constructors fail
        NormalConstructorFallback target = m_container.Resolve<NormalConstructorFallback>();

        Assert.That(target.ResolvedVia, Is.EqualTo("Normal_Parameterless"));
    }

    [Test]
    public void Resolve_TypeWithNoResolvableConstructors_ThrowsException()
    {
        Exception? ex = Assert.Throws<Exception>(() => m_container.Resolve<UnresolvableType>());
        Assert.That(ex!.Message, Contains.Substring("Could not resolve type"));
    }

    #endregion

    #region Circular Dependency Tests

    [Test]
    public void Resolve_DirectCircularDependency_ThrowsExceptionWithCorrectPath()
    {
        m_container.Register<ICircularA, CircularA>();
        m_container.Register<ICircularB, CircularB>();

        CircularDependencyException? ex = Assert.Throws<CircularDependencyException>(() => m_container.Resolve<ICircularA>());
        Assert.That(ex!.Message, Contains.Substring("Circular dependency detected: CircularA -> CircularB -> CircularA"));
    }

    [Test]
    public void Resolve_SelfReferencingDependency_ThrowsException()
    {
        CircularDependencyException? ex = Assert.Throws<CircularDependencyException>(() => m_container.Resolve<SelfReferencingClass>());
        Assert.That(ex!.Message, Contains.Substring("Circular dependency detected: SelfReferencingClass -> SelfReferencingClass"));
    }

    [Test]
    public void Resolve_DiamondDependency_SucceedsWithoutFalsePositiveCircularError()
    {
        // Shade: A depends on B and C; both B and C depend on D.
        // D should resolve cleanly twice across different sub-branches.
        m_container.Register<IServiceD, ServiceD>();

        DiamondA resolved = m_container.Resolve<DiamondA>();

        Assert.That(resolved, Is.Not.Null);
        Assert.That(resolved.B, Is.Not.Null);
        Assert.That(resolved.C, Is.Not.Null);
    }

    #endregion

    #region Dependency Graph Tests

    [Test]
    public void GetDependencyGraph_RendersExpectedStructure()
    {
        m_container.Register<IInventoryService, InventoryTest>();
        m_container.Register<IWeapon, Sword>();

        string graph = m_container.GetDependencyGraph<Player>();

        Assert.That(graph, Contains.Substring("└── Player"));
        Assert.That(graph, Contains.Substring("├── IInventoryService -> InventoryTest"));
        Assert.That(graph, Contains.Substring("└── IWeapon -> Sword"));
    }

    [Test]
    public void GetDependencyGraph_UnresolvedInterface_DisplaysUnresolvedTag()
    {
        string graph = m_container.GetDependencyGraph<IInventoryService>();

        Assert.That(graph, Contains.Substring("└── IInventoryService [UNRESOLVED INTERFACE]"));
    }

    [Test]
    public void GetDependencyGraph_CircularDependency_DisplaysCircularTag()
    {
        m_container.Register<ICircularA, CircularA>();
        m_container.Register<ICircularB, CircularB>();

        string graph = m_container.GetDependencyGraph<ICircularA>();

        Assert.That(graph, Contains.Substring("[CIRCULAR DEPENDENCY DETECTED]"));
    }

    #endregion

    #region Test Mocks & Helper Classes

    public interface IServiceA { }
    public class ServiceA : IServiceA { }

    public interface IServiceB { }
    public class ServiceB : IServiceB { }

    public interface IServiceD { }
    public class ServiceD : IServiceD { }

    public class MockInventoryService : IInventoryService
    {
        public int Add(InventoryEntry entry, int qty) => 0;
        public void Remove(string name, int qty) { }
        public InventoryEntry? Get(string name) => null;
    }

    public class MultipleInjectConstructors
    {
        public string ResolvedVia { get; }

        [Inject]
        public MultipleInjectConstructors(IServiceA a, IServiceB b)
        {
            ResolvedVia = "Inject_TwoParams";
        }

        [Inject]
        public MultipleInjectConstructors(IServiceA a)
        {
            ResolvedVia = "Inject_OneParam";
        }

        public MultipleInjectConstructors()
        {
            ResolvedVia = "Normal_Parameterless";
        }
    }

    public class NormalConstructorFallback
    {
        public string ResolvedVia { get; }

        [Inject]
        public NormalConstructorFallback(IServiceA a)
        {
            ResolvedVia = "Inject_OneParam";
        }

        public NormalConstructorFallback()
        {
            ResolvedVia = "Normal_Parameterless";
        }
    }

    public class UnresolvableType
    {
        public UnresolvableType(IServiceA unregistered) { }
    }

    public interface ICircularA { }
    public interface ICircularB { }

    public class CircularA : ICircularA
    {
        public CircularA(ICircularB b) { }
    }

    public class CircularB : ICircularB
    {
        public CircularB(ICircularA a) { }
    }

    public class SelfReferencingClass
    {
        public SelfReferencingClass(SelfReferencingClass self) { }
    }

    public class DiamondB
    {
        public IServiceD D { get; }
        public DiamondB(IServiceD d) { D = d; }
    }

    public class DiamondC
    {
        public IServiceD D { get; }
        public DiamondC(IServiceD d) { D = d; }
    }

    public class DiamondA
    {
        public DiamondB B { get; }
        public DiamondC C { get; }

        public DiamondA(DiamondB b, DiamondC c)
        {
            B = b;
            C = c;
        }
    }

    #endregion
}