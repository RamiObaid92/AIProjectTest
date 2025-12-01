using Library.Domain.TypeDescriptors;

namespace Library.Application.TypeDescriptors;

/// <summary>
/// Provides access to type descriptors that define resource type schemas,
/// validation rules, and metadata.
/// </summary>
public interface ITypeDescriptorRegistry
{
    /// <summary>
    /// Gets a type descriptor by its type key.
    /// </summary>
    /// <param name="typeKey">The type key to look up (case-insensitive).</param>
    /// <returns>The type descriptor if found; otherwise, null.</returns>
    TypeDescriptor? GetDescriptorOrDefault(string typeKey);

    /// <summary>
    /// Gets a type descriptor by its type key, throwing if not found.
    /// </summary>
    /// <param name="typeKey">The type key to look up (case-insensitive).</param>
    /// <returns>The type descriptor.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no descriptor exists for the specified type key.</exception>
    TypeDescriptor GetRequiredDescriptor(string typeKey);
}
