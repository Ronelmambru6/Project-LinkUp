using AutoMapper;
using LinkUpPro.Application.Interfaces.Repositories;
using LinkUpPro.Application.Interfaces.Services;
using LinkUpPro.Domain.Common;

namespace LinkUpPro.Application.Services
{
    // Operaciones comunes. Los servicios de cada módulo heredan de esta clase
    // y agregan sus reglas de negocio
    public abstract class GenericService<TEntity, TDto> : IGenericService<TDto>
        where TEntity : BaseEntity
        where TDto : class
    {
        protected readonly IGenericRepository<TEntity> _repository;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly IMapper _mapper;

        protected GenericService(IGenericRepository<TEntity> repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public virtual async Task<TDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<TDto>(entity);
        }

        public virtual async Task<List<TDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<List<TDto>>(entities);
        }

        public virtual async Task<TDto> AddAsync(TDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<TDto>(entity);
        }

        public virtual async Task<bool> UpdateAsync(int id, TDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null) return false;

            _mapper.Map(dto, entity);
            entity.Id = id; // el Id nunca debe cambiar por lo que venga en el DTO
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // Para entidades con borrado lógico (Post, Comment…), el servicio específico
        // sobrescribe este método y marca IsDeleted en lugar de borrar.
        public virtual async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity is null) return false;

            _repository.Remove(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}