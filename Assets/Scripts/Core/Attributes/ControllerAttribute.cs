using System;
using JetBrains.Annotations;

namespace BaseTemplate.Attributes
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class ControllerAttribute : Attribute { }
}