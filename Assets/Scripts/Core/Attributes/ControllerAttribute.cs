using System;
using JetBrains.Annotations;

namespace BaseTemplate.Attributes
{
    [UsedImplicitly]
    [AttributeUsage(AttributeTargets.Class)]
    public class ControllerAttribute : Attribute { }
}