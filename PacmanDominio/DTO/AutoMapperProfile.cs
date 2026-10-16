using AutoMapper;
using PacmanDominio.Entidades;
using PacmanDominio.EntidadesTipadas;

namespace PacmanDominio.DTO;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<TDificultad, Dificultad>().ReverseMap();
        CreateMap<TNivel, Nivel>().ReverseMap();
        CreateMap<TObjeto, Objeto>().ReverseMap();
        CreateMap<TParticipanteRonda, ParticipanteRonda>().ReverseMap();
        CreateMap<TPartida, Partida>().ReverseMap();
        CreateMap<TPersonaje, Personaje>().ReverseMap();
        CreateMap<TRonda, Ronda>().ReverseMap();
        CreateMap<TSesionMovil, SesionMovil>().ReverseMap();
        CreateMap<Usuario, TUsuario>()
            .ForMember(destino => destino.Contrasena, opcion => opcion.Ignore());
    }
}
