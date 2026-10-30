using System.Collections.Generic;
namespace AshkanIndustrialAccounting.Core.Interfaces
{
    public interface IRepository<T>
    {
        IReadOnlyList<T> GetAll();
        T GetById(int id);
        int Add(T entity);
        void Update(T entity);
        void Delete(int id);
    }
}
