
namespace DataBaseAccess
{
    internal interface IDataBaseAccess
    {
        TEntity? Get<TEntity>(int id) where TEntity : class;
        IEnumerable<TEntity>? GetAll<TEntity>() where TEntity : class;
        void Add<TEntity>(TEntity entity) where TEntity : class;
        void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;
        void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        void Update<TEntity>(TEntity entity) where TEntity : class;
    }
}
