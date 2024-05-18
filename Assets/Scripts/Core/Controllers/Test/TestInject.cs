#if STRESSTEST
using BaseTemplate.Attributes;
using JetBrains.Annotations;

namespace BaseTemplate.Controllers
{
    public interface ITestInject
    {
        
    }
    
    [Controller, UsedImplicitly]
    public class TestInject : ITestInject
    {
        public const string TEST = "TESTER";
    }
}
#endif