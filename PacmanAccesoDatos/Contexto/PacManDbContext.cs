using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PacmanDominio.Entidades;
using PacmanDominio.Entidades.Vistas;

namespace PacmanAccesoDatos.Contexto;

public partial class PacManDbContext : DbContext
{
    public PacManDbContext()
    {
    }

    public PacManDbContext(DbContextOptions<PacManDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Dificultad> Dificultades { get; set; }

    public virtual DbSet<Fruta> Frutas { get; set; }

    public virtual DbSet<Nivel> Niveles { get; set; }

    public virtual DbSet<ParticipantePartida> ParticipantesPartida { get; set; }

    public virtual DbSet<Partida> Partidas { get; set; }

    public virtual DbSet<Personaje> Personajes { get; set; }

    public virtual DbSet<SesionMovil> SesionesMoviles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<HistorialEntrada> VwHistorialUsuario { get; set; }

    public virtual DbSet<ProgresoJugador> VwProgresoJugador { get; set; }

    public virtual DbSet<RankingEntrada> VwRanking { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:PacmanDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dificultad>(entity =>
        {
            entity.HasKey(e => e.IdDificultad).HasName("PK__dificult__BD8B03597F2E0D4F");

            entity.ToTable("dificultades");

            entity.HasIndex(e => e.Nombre, "UQ__dificult__72AFBCC668A1B9DA").IsUnique();

            entity.Property(e => e.IdDificultad).HasColumnName("id_dificultad");
            entity.Property(e => e.DuracionAsustadoSeg).HasColumnName("duracion_asustado_seg");
            entity.Property(e => e.MultiplicadorVelocidadIa)
                .HasColumnType("decimal(3, 2)")
                .HasColumnName("multiplicador_velocidad_ia");
            entity.Property(e => e.Nombre)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Fruta>(entity =>
        {
            entity.HasKey(e => e.IdFruta).HasName("PK__frutas__64905FA1156F38AB");

            entity.ToTable("frutas");

            entity.HasIndex(e => e.Nombre, "UQ__frutas__72AFBCC6F97EC0D1").IsUnique();

            entity.Property(e => e.IdFruta).HasColumnName("id_fruta");
            entity.Property(e => e.DuracionSeg).HasColumnName("duracion_seg");
            entity.Property(e => e.Efecto)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("efecto");
            entity.Property(e => e.Nombre)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Puntos).HasColumnName("puntos");
        });

        modelBuilder.Entity<Nivel>(entity =>
        {
            entity.HasKey(e => e.IdNivel).HasName("PK__niveles__9CAF1C53BEF2D6BC");

            entity.ToTable("niveles");

            entity.HasIndex(e => e.Numero, "UQ__niveles__FC77F211EA07036B").IsUnique();

            entity.Property(e => e.IdNivel).HasColumnName("id_nivel");
            entity.Property(e => e.LaberintoJson).HasColumnName("laberinto_json");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Numero).HasColumnName("numero");
            entity.Property(e => e.VelocidadBase)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("velocidad_base");
        });

        modelBuilder.Entity<ParticipantePartida>(entity =>
        {
            entity.HasKey(e => e.IdParticipante).HasName("PK__particip__A88054DC9A7754EA");

            entity.ToTable("participantes_partida");

            entity.HasIndex(e => e.IdUsuario, "IX_participantes_usuario");

            entity.HasIndex(e => new { e.IdPartida, e.IdPersonaje }, "UQ_participantes_personaje").IsUnique();

            entity.HasIndex(e => new { e.IdPartida, e.IdUsuario }, "UX_participantes_usuario")
                .IsUnique()
                .HasFilter("([id_usuario] IS NOT NULL)");

            entity.Property(e => e.IdParticipante).HasColumnName("id_participante");
            entity.Property(e => e.Abandono).HasColumnName("abandono");
            entity.Property(e => e.EsIa).HasColumnName("es_ia");
            entity.Property(e => e.FantasmasComidos).HasColumnName("fantasmas_comidos");
            entity.Property(e => e.FrutasComidas).HasColumnName("frutas_comidas");
            entity.Property(e => e.IdPartida).HasColumnName("id_partida");
            entity.Property(e => e.IdPersonaje).HasColumnName("id_personaje");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.PacmansAtrapados).HasColumnName("pacmans_atrapados");
            entity.Property(e => e.Puntuacion).HasColumnName("puntuacion");

            entity.HasOne(d => d.IdPartidaNavigation).WithMany(p => p.ParticipantesPartida)
                .HasForeignKey(d => d.IdPartida)
                .HasConstraintName("FK_participantes_partidas");

            entity.HasOne(d => d.IdPersonajeNavigation).WithMany(p => p.ParticipantesPartida)
                .HasForeignKey(d => d.IdPersonaje)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_participantes_personajes");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ParticipantesPartida)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_participantes_usuarios");
        });

        modelBuilder.Entity<Partida>(entity =>
        {
            entity.HasKey(e => e.IdPartida).HasName("PK__partidas__42D83E72715C4CE9");

            entity.ToTable("partidas");

            entity.HasIndex(e => e.FechaInicio, "IX_partidas_fecha").IsDescending();

            entity.Property(e => e.IdPartida).HasColumnName("id_partida");
            entity.Property(e => e.BandoGanador)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("bando_ganador");
            entity.Property(e => e.CodigoSala)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("codigo_sala");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("EN_CURSO")
                .HasColumnName("estado");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_inicio");
            entity.Property(e => e.IdDificultad).HasColumnName("id_dificultad");
            entity.Property(e => e.IdNivelAlcanzado).HasColumnName("id_nivel_alcanzado");
            entity.Property(e => e.Modo)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("modo");

            entity.HasOne(d => d.IdDificultadNavigation).WithMany(p => p.Partidas)
                .HasForeignKey(d => d.IdDificultad)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_partidas_dificultades");

            entity.HasOne(d => d.IdNivelAlcanzadoNavigation).WithMany(p => p.Partidas)
                .HasForeignKey(d => d.IdNivelAlcanzado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_partidas_niveles");
        });

        modelBuilder.Entity<Personaje>(entity =>
        {
            entity.HasKey(e => e.IdPersonaje).HasName("PK__personaj__81949F40F118071E");

            entity.ToTable("personajes");

            entity.HasIndex(e => e.Nombre, "UQ__personaj__72AFBCC62B9BE222").IsUnique();

            entity.HasIndex(e => e.Color, "UQ__personaj__900DC6E9BA305120").IsUnique();

            entity.Property(e => e.IdPersonaje).HasColumnName("id_personaje");
            entity.Property(e => e.Color)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("color");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("rol");
        });

        modelBuilder.Entity<SesionMovil>(entity =>
        {
            entity.HasKey(e => e.IdSesion).HasName("PK__sesiones__8D3F9DFEEB1E3343");

            entity.ToTable("sesiones_moviles");

            entity.HasIndex(e => e.TokenDispositivo, "IX_sesiones_token");

            entity.Property(e => e.IdSesion).HasColumnName("id_sesion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.TokenDispositivo)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("token_dispositivo");
            entity.Property(e => e.UltimoAcceso)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("ultimo_acceso");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.SesionesMoviles)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("FK_sesiones_usuarios");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK__usuarios__4E3E04ADD70DB97A");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.CorreoElectronico, "UQ__usuarios__5B8A0682BA612AFE").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "UQ__usuarios__D4D22D74AD46A27B").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.ContrasenaHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("contrasena_hash");
            entity.Property(e => e.CorreoElectronico)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("correo_electronico");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre_usuario");
        });

        modelBuilder.Entity<HistorialEntrada>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_historial_usuario");

            entity.Property(e => e.Dificultad)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("dificultad");
            entity.Property(e => e.Estado)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("estado");
            entity.Property(e => e.FantasmasComidos).HasColumnName("fantasmas_comidos");
            entity.Property(e => e.FechaFin).HasColumnName("fecha_fin");
            entity.Property(e => e.FechaInicio).HasColumnName("fecha_inicio");
            entity.Property(e => e.FrutasComidas).HasColumnName("frutas_comidas");
            entity.Property(e => e.Gano).HasColumnName("gano");
            entity.Property(e => e.IdPartida).HasColumnName("id_partida");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Modo)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("modo");
            entity.Property(e => e.NivelAlcanzado).HasColumnName("nivel_alcanzado");
            entity.Property(e => e.PacmansAtrapados).HasColumnName("pacmans_atrapados");
            entity.Property(e => e.Personaje)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("personaje");
            entity.Property(e => e.Puntuacion).HasColumnName("puntuacion");
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("rol");
        });

        modelBuilder.Entity<ProgresoJugador>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_progreso_jugador");

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.MejorPuntuacion).HasColumnName("mejor_puntuacion");
            entity.Property(e => e.NivelMaximoAlcanzado).HasColumnName("nivel_maximo_alcanzado");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre_usuario");
            entity.Property(e => e.PartidasJugadas).HasColumnName("partidas_jugadas");
            entity.Property(e => e.PuntosTotales).HasColumnName("puntos_totales");
        });

        modelBuilder.Entity<RankingEntrada>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ranking");

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.MejorPuntuacion).HasColumnName("mejor_puntuacion");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre_usuario");
            entity.Property(e => e.PartidasEnRol).HasColumnName("partidas_en_rol");
            entity.Property(e => e.Posicion).HasColumnName("posicion");
            entity.Property(e => e.Rol)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("rol");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
