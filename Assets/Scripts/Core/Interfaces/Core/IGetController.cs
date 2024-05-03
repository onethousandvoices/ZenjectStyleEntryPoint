namespace BaseTemplate.Interfaces
{
    public interface IGetController
    {
        public T GetController<T>();
        public T[] GetControllers<T>();
    }
}