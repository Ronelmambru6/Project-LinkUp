using System.Linq.Expressions;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Domain.Common;
using LinkUpPro.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Persistence.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id) =>
            await _dbSet.FindAsync(id);

        public virtual async Task<List<TEntity>> GetAllAsync() =>
            await _dbSet.AsNoTracking().ToListAsync();

        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
            await _dbSet.Where(predicate).ToListAsync();

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate) =>
            await _dbSet.AnyAsync(predicate);

        public virtual async Task AddAsync(TEntity entity) =>
            await _dbSet.AddAsync(entity);

        public virtual void Update(TEntity entity) =>
            _dbSet.Update(entity);

        public virtual void Remove(TEntity entity) =>
            _dbSet.Remove(entity);
    }
}