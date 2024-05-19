#if STRESSTEST
using BaseTemplate.Attributes;
using JetBrains.Annotations;

namespace BaseTemplate.Controllers
{
    public interface ITestInject
    {
        
    }
    
    [Controller]
    public class TestInject : ITestInject
    {
        public const string TEST = "TESTER";
    }
}
#endif