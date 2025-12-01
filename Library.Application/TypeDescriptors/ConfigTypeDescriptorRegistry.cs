using Library.Domain.TypeDescriptors;
using Microsoft.Extensions.Configuration;

namespace Library.Application.TypeDescriptors;

/// <summary>
/// A configuration-based implementation of <see cref="ITypeDescriptorRegistry"/>
/// that loads type descriptors from the application configuration.
/// </summary>
public class ConfigTypeDescriptorRegistry : ITypeDescriptorRegistry
{
    private readonly Dictionary<string, TypeDescriptor> _descriptors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigTypeDescriptorRegistry"/> class.
    /// Loads type descriptors from the "TypeDescriptors" configuration section.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a descriptor has an empty TypeKey or duplicate keys exist.</exception>
    public ConfigTypeDescriptorRegistry(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _descriptors = new Dictionary<string, TypeDescriptor>(StringComparer.OrdinalIgnoreCase);

        var section = configuration.GetSection("TypeDescriptors");

        foreach (var childSection in section.GetChildren())
        {
            var descriptor = BindDescriptor(childSection);

            if (string.IsNullOrWhiteSpace(descriptor.TypeKey))
            {
                throw new InvalidOperationException(
                    $"Type descriptor at configuration key '{childSection.Key}' has an empty TypeKey.");
            }

            var normalizedKey = descriptor.TypeKey.ToLowerInvariant();

            if (_descriptors.ContainsKey(normalizedKey))
            {
                throw new InvalidOperationException(
                    $"Duplicate type descriptor key '{descriptor.TypeKey}' found in configuration.");
            }

            _descriptors[normalizedKey] = descriptor;
        }
    }

    /// <inheritdoc />
    public TypeDescriptor? GetDescriptorOrDefault(string typeKey)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            return null;
        }

        return _descriptors.TryGetValue(typeKey, out var descriptor) ? descriptor : null;
    }

    /// <inheritdoc />
    public TypeDescriptor GetRequiredDescriptor(string typeKey)
    {
        var descriptor = GetDescriptorOrDefault(typeKey);

        if (descriptor is null)
        {
            throw new KeyNotFoundException(
                $"No type descriptor found for type key '{typeKey}'.");
        }

        return descriptor;
    }

    /// <summary>
    /// Binds a configuration section to a TypeDescriptor object.
    /// </summary>
    private static TypeDescriptor BindDescriptor(IConfigurationSection section)
    {
        var typeKey = section.GetValue<string>("typeKey") ?? section.Key;
        var displayName = section.GetValue<string>("displayName") ?? typeKey;
        var schemaVersion = section.GetValue<int>("schemaVersion");

        var fields = BindFields(section.GetSection("fields"));
        var indexing = BindIndexing(section.GetSection("indexing"));
        var policy = BindPolicy(section.GetSection("policy"));
        var uiHints = BindUiHints(section.GetSection("uiHints"));

        return new TypeDescriptor(
            typeKey: typeKey,
            displayName: displayName,
            schemaVersion: schemaVersion == 0 ? 1 : schemaVersion,
            fields: fields,
            indexing: indexing,
            policy: policy,
            uiHints: uiHints);
    }

    /// <summary>
    /// Binds the fields array from configuration.
    /// </summary>
    private static List<FieldDefinition> BindFields(IConfigurationSection section)
    {
        var fields = new List<FieldDefinition>();

        foreach (var fieldSection in section.GetChildren())
        {
            var name = fieldSection.GetValue<string>("name") ?? string.Empty;
            var dataTypeString = fieldSection.GetValue<string>("dataType") ?? "String";
            var isRequired = fieldSection.GetValue<bool>("isRequired");
            var maxLength = fieldSection.GetValue<int?>("maxLength");
            var pattern = fieldSection.GetValue<string?>("pattern");

            if (!Enum.TryParse<FieldDataType>(dataTypeString, ignoreCase: true, out var dataType))
            {
                dataType = FieldDataType.String;
            }

            fields.Add(new FieldDefinition
            {
                Name = name,
                DataType = dataType,
                IsRequired = isRequired,
                MaxLength = maxLength,
                Pattern = pattern
            });
        }

        return fields;
    }

    /// <summary>
    /// Binds the indexing definition from configuration.
    /// </summary>
    private static IndexingDefinition? BindIndexing(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return null;
        }

        return new IndexingDefinition
        {
            FilterableFields = section.GetSection("filterableFields").Get<List<string>>() ?? [],
            SortableFields = section.GetSection("sortableFields").Get<List<string>>() ?? [],
            FullTextFields = section.GetSection("fullTextFields").Get<List<string>>() ?? []
        };
    }

    /// <summary>
    /// Binds the policy definition from configuration.
    /// </summary>
    private static PolicyDefinition? BindPolicy(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return null;
        }

        return new PolicyDefinition
        {
            AllowedCreateRoles = section.GetSection("allowedCreateRoles").Get<List<string>>() ?? [],
            AllowedReadRoles = section.GetSection("allowedReadRoles").Get<List<string>>() ?? [],
            AllowedUpdateRoles = section.GetSection("allowedUpdateRoles").Get<List<string>>() ?? [],
            AllowedDeleteRoles = section.GetSection("allowedDeleteRoles").Get<List<string>>() ?? []
        };
    }

    /// <summary>
    /// Binds the UI hints from configuration.
    /// </summary>
    private static UiHints? BindUiHints(IConfigurationSection section)
    {
        if (!section.Exists())
        {
            return null;
        }

        return new UiHints
        {
            TitleField = section.GetValue<string?>("titleField"),
            ListFields = section.GetSection("listFields").Get<List<string>>() ?? []
        };
    }
}
