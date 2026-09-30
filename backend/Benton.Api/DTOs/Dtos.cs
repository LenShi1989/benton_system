namespace Benton.Api.DTOs;

public record LoginDto(string Username, string Password);

public record AuthResponseDto(
    string Token,
    string Username,
    string FullName,
    string Role,
    List<string> AllowedMenus
);

public record UserDto(
    int Id,
    string Username,
    string FullName,
    string Role,
    int? RoleId,
    DateTime CreatedAt
);

public record CreateUserDto(
    string Username,
    string Password,
    string FullName,
    string Role,
    int? RoleId
);

public record UpdateUserDto(
    string FullName,
    string Role,
    int? RoleId
);

public record RoleDto(
    int Id,
    string Name,
    string? Description,
    List<string> AllowedMenus,
    DateTime CreatedAt
);

public record CreateRoleDto(
    string Name,
    string? Description,
    List<string> AllowedMenus
);

public record StoreDto(int Id, string Name, string? Phone, string? Address, bool IsActive, DateTime CreatedAt);
public record CreateStoreDto(string Name, string? Phone, string? Address, bool IsActive = true);

public record MenuItemDto(int Id, int StoreId, string Name, string? Description, decimal Price, bool IsActive);
public record CreateMenuItemDto(int StoreId, string Name, string? Description, decimal Price, bool IsActive = true);

public record OrderSessionDto(
    int Id,
    string Title,
    int StoreId,
    string StoreName,
    string Status,
    DateTime? Deadline,
    int CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    int TotalItems,
    decimal TotalAmount
);

public record CreateOrderSessionDto(string Title, int StoreId, DateTime? Deadline);

public record OrderItemDto(
    int Id,
    int OrderSessionId,
    int UserId,
    string UserName,
    int MenuItemId,
    string MenuItemName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    string? Note
);

public record CreateOrderItemDto(int OrderSessionId, int MenuItemId, int Quantity, string? Note);

public record AuditLogDto(
    int Id,
    int? UserId,
    string UserName,
    string Action,
    string? Details,
    string? IpAddress,
    DateTime CreatedAt
);
