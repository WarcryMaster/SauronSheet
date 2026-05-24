namespace SauronSheet.Domain.ValueObjects;

/// <summary>
/// Source of a transaction's category assignment.
/// Tracks how the category was determined for resolution auditing.
/// </summary>
public enum CategorySource
{
    /// <summary>
    /// Transaction was created before the category resolution feature.
    /// No resolution was performed — bank values may be null.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Raw bank category/subcategory saved, but no match was found.
    /// CategoryId is null — user needs to categorize manually.
    /// </summary>
    RawOnly = 1,

    /// <summary>
    /// Category was automatically matched by the resolution service
    /// via name comparison (direct or translated).
    /// </summary>
    AutoMatched = 2,

    /// <summary>
    /// Category was manually assigned (or changed) by the user.
    /// Set by Categorize() when a non-null CategoryId is provided.
    /// </summary>
    UserOverride = 3
}
