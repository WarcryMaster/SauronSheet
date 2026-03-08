namespace SauronSheet.Infrastructure.Assets;

using System.Collections.Generic;

/// <summary>
/// Approved list of Bootstrap icon names available for category selection.
/// Used for form validation and dropdown population.
/// 
/// Reference: https://icons.getbootstrap.com/
/// </summary>
public static class AllowedBootstrapIcons
{
    /// <summary>
    /// Complete list of allowed Bootstrap icons (alphabetically sorted).
    /// </summary>
    public static readonly IReadOnlySet<string> Icons = new HashSet<string>
    {
        // Common category icons
        "building-dollar",      // Income/salary
        "storefront",           // Sales/business
        "trending-up",          // Investments
        "gift",                 // Gifts
        "inbox-in",             // Incoming money
        "home",                 // Housing
        "lightning-bolt",       // Utilities
        "shield-check",         // Insurance
        "ticket",               // Subscriptions
        "graduation-cap",       // Education
        "basket",               // Groceries
        "car",                  // Transportation
        "soap",                 // Personal care
        "hammer-wrench",        // Home maintenance
        "paw",                  // Pets
        "utensils",             // Restaurants/food
        "film",                 // Entertainment
        "shopping-bag",         // Shopping
        "airplane",             // Travel
        "heart",                // Health/wellness
        "credit-card",          // Debt payments
        "piggy-bank",           // Savings
        "hand-heart",           // Donations
        "exclamation-triangle", // Unexpected expenses
        
        // Additional common icons
        "tag",                  // Default/general
        "briefcase",            // Work/business
        "book",                 // Education/learning
        "folder",               // Files/documents
        "calendar",             // Dates/events
        "bell",                 // Notifications
        "chart-bar",            // Analytics
        "cloud",                // Cloud/storage
        "cog",                  // Settings
        "database",             // Database
        "download",             // Downloads
        "envelope",             // Email/messages
        "exclamation-circle",   // Alerts
        "fingerprint",          // Security
        "gear",                 // Configuration
        "headset",              // Support/help
        "info-circle",          // Information
        "lock",                 // Locked/secured
        "mail",                 // Mail
        "map",                  // Location
        "music",                // Music/audio
        "pencil",               // Edit/write
        "person",               // User/profile
        "phone",                // Call/phone
        "radio",                // Broadcast
        "search",               // Search
        "share",                // Share
        "star",                 // Favorite/rating
        "trash",                // Delete
        "upload",               // Uploads
        "user-circle",          // User avatar
        "volume-mute",          // Mute/silent
        "wallet",               // Digital wallet/payment
        "wifi",                 // Network/connectivity
        "wrench",               // Tools/repair
        "x-circle"              // Close/error
    };

    /// <summary>
    /// Validates if the provided icon name is in the approved list.
    /// </summary>
    /// <param name="iconName">Icon name to validate (case-sensitive).</param>
    /// <returns>True if icon is approved; false otherwise.</returns>
    public static bool IsValid(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return false;

        return Icons.Contains(iconName);
    }

    /// <summary>
    /// Gets all available icon names as a sorted list for UI dropdowns.
    /// </summary>
    /// <returns>Sorted list of all approved icon names.</returns>
    public static IReadOnlyList<string> GetAllIconsForDropdown()
    {
        var sorted = new List<string>(Icons);
        sorted.Sort();
        return sorted.AsReadOnly();
    }
}
