using Microsoft.Data.Entity;
using System;
using System.Linq;
using Microsoft.Data.Entity.Infrastructure;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;

namespace Books.Repository
{
    public interface IReadOnlyRepository<out T> : IDisposable
    {
        T FindById(int id);
        IQueryable<T> All();
    }

    public interface IWriteOnlyRepository<in T> : IDisposable
    {
        void Add(T newEntity);
        void Remove(T entity);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IRepository<T> : IReadOnlyRepository<T>, IWriteOnlyRepository<T>, IDisposable { }

    public class GenericRepository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _ctx;
        protected readonly DbSet<T> _set;

        public GenericRepository(DbContext ctx)
        {
            _ctx = ctx;
            _set = _ctx.Set<T>();
        }

        public void Add(T newEntity)
        {
            _set.Add(newEntity);
        }

        public void Remove(T entity)
        {
            _set.Remove(entity);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public IQueryable<T> All()
        {
            return _set.AsQueryable();
        }

        public T FindById(int id)
        {
            return _set.Find(id); //https://github.com/aspnet/EntityFramework/issues/2012
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _ctx.SaveChangesAsync(cancellationToken);
        }
    }

    public static class Extensions
    {
        public static TEntity Find<TEntity>(this DbSet<TEntity> set, params object[] keyValues) where TEntity : class
        {
            var context = ((IInfrastructure<IServiceProvider>)set).GetService<DbContext>();

            var entityType = context.Model.FindEntityType(typeof(TEntity));
            var key = entityType.FindPrimaryKey();

            var entries = context.ChangeTracker.Entries<TEntity>();

            var i = 0;
            foreach (var property in key.Properties)
            {
                entries = entries.Where(e => e.Property(property.Name).CurrentValue == keyValues[i]);
                i++;
            }

            var entry = entries.FirstOrDefault();
            if (entry != null)
            {
                // Return the local object if it exists.
                return entry.Entity;
            }

            // TODO: Build the real LINQ Expression
            // set.Where(x => x.Id == keyValues[0]);
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var query = set.Where((Expression<Func<TEntity, bool>>)
                Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, "Id"),
                        Expression.Constant(keyValues[0])),
                    parameter));

            // Look in the database
            return query.FirstOrDefault();
        }
    }
}


