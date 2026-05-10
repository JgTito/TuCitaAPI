using TuCita.Application.Common;

namespace TuCita.Application.CategoriasServicio;

public interface ICategoriaServicioService
{
    Task<PagedResult<CategoriaServicioDto>> GetAllAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CategoriaServicioQuery query,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoriaServicioDto>> GetByIdAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoriaServicioDto>> CreateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        CreateCategoriaServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoriaServicioDto>> UpdateAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        UpdateCategoriaServicioRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoriaServicioDto>> SetActiveAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        bool activo,
        CancellationToken cancellationToken);

    Task<ServiceResult<CategoriaServicioDto>> DeleteAsync(
        CurrentUserContext currentUser,
        int idNegocio,
        int idCategoriaServicio,
        CancellationToken cancellationToken);
}
