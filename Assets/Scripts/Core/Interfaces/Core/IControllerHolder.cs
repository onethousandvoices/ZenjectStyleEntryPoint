namespace BaseTemplate.Interfaces
{
    public interface IControllerHolder
    {
        public void AddController<T>(T controller);
    }
}