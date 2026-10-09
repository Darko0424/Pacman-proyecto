using AutoMapper;
using PacmanDominio.Entidades;
using PacmanDominio.EntidadesTipadas;

namespace PacmanDominio.DTO;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<TDificultad, Dificultad>().ReverseMap();
        CreateMap<TFruta, Fruta>().ReverseMap();
        CreateMap<TNivel, Nivel>().ReverseMap();
        CreateMap<TParticipantePartida, ParticipantePartida>().ReverseMap();
        CreateMap<TPartida, Partida>().ReverseMap();
        CreateMap<TPersonaje, Personaje>().ReverseMap();
        CreateMap<TSesionMovil, SesionMovil>().ReverseMap();
        CreateMap<Usuario, TUsuario>()
            .ForMember(destino => destino.Contrasena, opcion => opcion.Ignore());
    }
}
