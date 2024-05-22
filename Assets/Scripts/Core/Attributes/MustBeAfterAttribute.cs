using System;

namespace BaseTemplate.Attributes
{
    public class MustBeAfterAttribute : Attribute
    {
        public readonly Type InitAfter;

        public MustBeAfterAttribute(Type initAfter)
            => InitAfter = initAfter;
    }
}