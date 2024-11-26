using System;
using Friflo.Engine.ECS.Systems;
using NUnit.Framework;

namespace Tests.ECS.Systems {
    
    public static class Test_SystemSort
    {
        class SystemA : BaseSystem
        {
            public override Type[] SortBefore { get; } = new Type[] { typeof(SystemC) };
        }
        class SystemB : BaseSystem
        {
            public override Type[] SortAfter { get; } = new Type[] { typeof(SystemA) };
            public override Type[] SortBefore { get; } = new Type[] { typeof(SystemC) };
        }
        class SystemC : BaseSystem
        {
            public override Type[] SortAfter { get; } = new Type[] { typeof(SystemA) };
        }
        
        [Test]
        public static void TestSystemSort()
        {
            var root = new SystemRoot();
            root.Add(new SystemC());
            root.Add(new SystemB());
            root.Add(new SystemA());
            
            var systems = root.ChildSystems;
            Assert.AreEqual(3, systems.Count);
            Assert.AreEqual(typeof(SystemA), systems[0].GetType());
            Assert.AreEqual(typeof(SystemB), systems[1].GetType());
            Assert.AreEqual(typeof(SystemC), systems[2].GetType());
        }
    }
}