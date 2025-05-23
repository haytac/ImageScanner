namespace ImageScanner.DataAccess.Entities;

/// <summary>
/// Base class for entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public int Id { get; set; }
}
