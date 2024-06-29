using Microsoft.EntityFrameworkCore;

namespace DataBaseAccess
{
    public class DbAccess : IDataBaseAccess
    {
        protected readonly DbContext context;

        public DbAccess(DbContext context)
        {
            this.context = context;
        }

        public async Task UpdateEntitiesAsync<TEnity>(List<TEnity> updatedEntities) where TEnity : class, new()
        {
            var dbSet = context.Set<TEnity>();
            var existingEntities = await dbSet.ToListAsync();

            foreach (var updatedEntity in updatedEntities)
            {
                var entityKey = context.Entry(updatedEntity).Property("Id").CurrentValue;
                var existingEntity = existingEntities.FirstOrDefault(e => context.Entry(e).Property("Id").CurrentValue!.Equals(entityKey));

                // if exist -> updating
                if (existingEntity != null)
                {
                    context.Entry(existingEntity).CurrentValues.SetValues(updatedEntity);
                }
                // otherwise -> adding
                else
                {
                    dbSet.Add(updatedEntity);
                }
            }

            // deleting
            foreach (var existingEntity in existingEntities)
            {
                var entityKey = context.Entry(existingEntity).Property("Id").CurrentValue;
                if (!updatedEntities.Any(e => context.Entry(e).Property("Id").CurrentValue!.Equals(entityKey)))
                {
                    dbSet.Remove(existingEntity);
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adding an entity of type TEntity to the corresponding table.
        /// </summary>
        /// <param name="entity"></param>
        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            context.Set<TEntity>().Add(entity);
            context.SaveChanges();
        }

        /// <summary>
        /// Adding a collection of entities of type TEntity to the corresponding table.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            context.Set<TEntity>().AddRange(entities);
            context.SaveChanges();
        }

        /// <summary>
        /// Getting an entity of TEntity type by primary key.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TEntity? Get<TEntity>(int id) where TEntity : class
        {
            return context.Set<TEntity>().Find(id);
        }

        /// <summary>
        /// Getting an entity of TEntity type by the predicate
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public TEntity? Get<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            return context.Set<TEntity>().FirstOrDefault(predicate);
        }

        /// <summary>
        /// Getting a collection of entities of type TEntity.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TEntity> GetAll<TEntity>() where TEntity : class
        {
            return context.Set<TEntity>();
        }

        /// <summary>
        /// Removing an entity of type TEntity from the corresponding table.
        /// </summary>
        /// <param name="entity"></param>
        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            context.Set<TEntity>().Remove(entity);
            context.SaveChanges();
        }

        /// <summary>
        /// Removing a collection of entities of type TEntity from the corresponding table.
        /// </summary>
        /// <param name="entities"></param>
        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            context.Set<TEntity>().RemoveRange(entities);
            context.SaveChanges();
        }

        /// <summary>
        /// Updating an entity of type TEntity with a new instance of the same type.
        /// </summary>
        /// <param name="entity"></param>
        public void Update<TEntity>(TEntity entity) where TEntity : class
        {
            context.Set<TEntity>().Update(entity);
            context.SaveChanges();
        }
    }
}
