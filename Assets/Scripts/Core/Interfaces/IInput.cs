using System;
using BaseTemplate.Enums;

namespace BaseTemplate.Interfaces
{
    public interface IInput
    {
        public void SubscribeTo(InputType type, Action callback);
        public void UnsubscribeFrom(InputType type, Action callback);
    }
}