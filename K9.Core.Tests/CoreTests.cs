using NUnit.Framework;

namespace K9.Tests
{
    public class CoreTests
    {
        public static Program Instance;

        [SetUp]
        public void Setup()
        {
            Instance = new Program();
            Core.Init(Instance);
        }
    }
}