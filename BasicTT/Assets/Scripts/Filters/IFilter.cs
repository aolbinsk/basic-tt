namespace Filters
{
    public interface IFilter<T>
    {
        T Update(T input);
        void Reset();
    }
}