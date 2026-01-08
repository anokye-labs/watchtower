namespace Avalonia.Mcp.Core.Services;

/// <summary>
/// Represents the result of an element search operation.
/// </summary>
public class ElementSearchResult
{
    /// <summary>
    /// Gets or sets whether the element was found.
    /// </summary>
    public bool Found { get; set; }

    /// <summary>
    /// Gets or sets the selector used to search for the element.
    /// </summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type name of the found element.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the name of the found element.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the automation ID of the found element.
    /// </summary>
    public string? AutomationId { get; set; }

    /// <summary>
    /// Gets or sets the bounds of the found element.
    /// </summary>
    public ElementBounds? Bounds { get; set; }

    /// <summary>
    /// Gets or sets whether the found element is visible.
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// Gets or sets the error message if the search failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Represents the bounds of an element.
/// </summary>
public class ElementBounds
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
