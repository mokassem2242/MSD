namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Base repository interface for aggregate roots.
/// Enforces that only aggregate roots can have repositories at compile time.
/// </summary>
/// <typeparam name="T">The aggregate root type</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
public interface IRepository<T, TId> 
    where T : class, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate root by its unique identifier.
    /// </summary>
    Task<T?> GetByIdAsync(TId id);

    /// <summary>
    /// Adds a new aggregate root to the repository.
    /// </summary>
    Task AddAsync(T aggregate);

    /// <summary>
    /// Updates an existing aggregate root in the repository.
    /// </summary>
    Task UpdateAsync(T aggregate);

    /// <summary>
    /// Deletes an aggregate root from the repository.
    /// </summary>
    Task DeleteAsync(TId id);
}

/// <summary>
/// Read-only repository interface for aggregate roots.
/// Use this when you only need read operations.
/// </summary>
/// <typeparam name="T">The aggregate root type</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
public interface IReadRepository<T, TId> 
    where T : class, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Gets an aggregate root by its unique identifier.
    /// </summary>
    Task<T?> GetByIdAsync(TId id);

    /// <summary>
    /// Gets all aggregate roots.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();
}

/// <summary>
/// Write-only repository interface for aggregate roots.
/// Use this when you only need write operations.
/// </summary>
/// <typeparam name="T">The aggregate root type</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
public interface IWriteRepository<T, TId> 
    where T : class, IAggregateRoot
    where TId : notnull
{
    /// <summary>
    /// Adds a new aggregate root to the repository.
    /// </summary>
    Task AddAsync(T aggregate);

    /// <summary>
    /// Updates an existing aggregate root in the repository.
    /// </summary>
    Task UpdateAsync(T aggregate);

    /// <summary>
    /// Deletes an aggregate root from the repository.
    /// </summary>
    Task DeleteAsync(TId id);
}

