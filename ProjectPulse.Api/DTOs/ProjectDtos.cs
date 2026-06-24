namespace ProjectPulse.Api.DTOs;

// Lo que el cliente ENVÍA para crear
public record ProjectCreateDto(string Name, string? Description);

// Lo que el cliente ENVÍA para actualizar
public record ProjectUpdateDto(string Name, string? Description);

// Lo que la API DEVUELVE al cliente
public record ProjectDto(Guid Id, string Name, string? Description, DateTime CreatedAt);

// Parámetros de query para listar (filtros/paginación)
public record ProjectQueryDto(
    string? Query,
    int Page = 1,
    int PageSize = 20,
    string? Sort = "-createdAt"
);
