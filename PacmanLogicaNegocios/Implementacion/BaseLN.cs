using AutoMapper;
using Microsoft.Extensions.Logging;
using PacmanDominio.InterfazAD;

namespace PacmanLogicaNegocios.Implementacion
{
    public abstract class BaseLN<T>
    {
        protected readonly IUnidadTrabajoEF _unidadDeTrabajo;
        protected readonly IMapper _mapper;
        protected readonly ILogger<T> _logger;

        protected BaseLN(
            IUnidadTrabajoEF unidadTrabajo, IMapper mapper, ILogger<T> logger)
        {
            _unidadDeTrabajo = unidadTrabajo;
            _mapper = mapper;
            _logger = logger;
        }
    }
}
