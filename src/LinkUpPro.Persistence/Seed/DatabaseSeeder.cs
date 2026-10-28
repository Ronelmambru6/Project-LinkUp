using LinkUpPro.Application.Common;
using LinkUpPro.Application.Services;
using LinkUpPro.Domain.Entities;
using LinkUpPro.Domain.Enums;
using LinkUpPro.Persistence.Contexts;
using LinkUpPro.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Persistence.Seed
{
    // Datos de prueba: usuarios, amistades, publicaciones, comentarios, reacciones, notificaciones y partidas.
    // Es idempotente: si el usuario "ana" ya existe, no hace nada. Todo se guarda en una sola transacción,
    // así que si algo falla no queda la base a medias.
    public class DatabaseSeeder
    {
        // Contraseña de todas las cuentas de prueba (cumple las reglas del enunciado)
        public const string PasswordDePrueba = "Prueba123!";

        // Barcos de dos tableros distintos (tamaño, fila, columna, dirección). Todos válidos y sin superponerse.
        private static readonly (int Tamano, int Fila, int Columna, DireccionBarco Direccion)[] TableroA =
        {
            (5, 0, 0, DireccionBarco.Derecha),
            (4, 2, 0, DireccionBarco.Derecha),
            (3, 4, 0, DireccionBarco.Derecha),
            (3, 6, 0, DireccionBarco.Derecha),
            (2, 8, 0, DireccionBarco.Derecha)
        };

        private static readonly (int Tamano, int Fila, int Columna, DireccionBarco Direccion)[] TableroB =
        {
            (5, 0, 11, DireccionBarco.Abajo),
            (4, 0, 9, DireccionBarco.Abajo),
            (3, 0, 7, DireccionBarco.Abajo),
            (3, 5, 7, DireccionBarco.Abajo),
            (2, 10, 0, DireccionBarco.Derecha)
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Devuelve false si los datos ya estaban cargados
        public async Task<bool> SeedAsync()
        {
            // Si todavía no se aplicaron las migraciones, se aplican aquí (no hace nada si la base ya está al día)
            await _context.Database.MigrateAsync();

            if (await _userManager.FindByNameAsync("ana") is not null)
            {
                _logger.LogInformation("Seed: los datos de prueba ya estaban cargados, no se hizo nada.");
                return false;
            }

            var ahora = DateTime.UtcNow;
            await using var transaccion = await _context.Database.BeginTransactionAsync();

            // ── Usuarios (diego queda sin activar a propósito, para probar la activación por correo)
            var ana = await CrearUsuarioAsync("ana", "Ana", "Pérez", "809-555-0101", true, ahora.AddDays(-30));
            var carlos = await CrearUsuarioAsync("carlos", "Carlos", "Rodríguez", "809-555-0102", true, ahora.AddDays(-28));
            var maria = await CrearUsuarioAsync("maria", "María", "Gómez", "809-555-0103", true, ahora.AddDays(-26));
            var luis = await CrearUsuarioAsync("luis", "Luis", "Fernández", "809-555-0104", true, ahora.AddDays(-24));
            var sofia = await CrearUsuarioAsync("sofia", "Sofía", "Martínez", "809-555-0105", true, ahora.AddDays(-20));
            var pedro = await CrearUsuarioAsync("pedro", "Pedro", "Núñez", "809-555-0106", true, ahora.AddDays(-3));
            await CrearUsuarioAsync("diego", "Diego", "Jiménez", "809-555-0107", false, ahora.AddDays(-1));

            // ── Amistades y solicitudes
            // Amigos: ana con carlos, maria y luis; carlos con maria y sofia; luis con sofia.
            // ana y sofia NO son amigos (sirve para ver a un autor sin enlace al perfil).
            var amistades = new[]
            {
                Amistad(ana, carlos, ahora.AddDays(-25)),
                Amistad(ana, maria, ahora.AddDays(-24)),
                Amistad(ana, luis, ahora.AddDays(-22)),
                Amistad(carlos, maria, ahora.AddDays(-21)),
                Amistad(carlos, sofia, ahora.AddDays(-15)),
                Amistad(luis, sofia, ahora.AddDays(-12))
            };

            var solicitudes = new List<FriendRequest>
            {
                // Las que dieron origen a las amistades
                Solicitud(ana, carlos, EstadoSolicitud.Aceptada, ahora.AddDays(-25)),
                Solicitud(maria, ana, EstadoSolicitud.Aceptada, ahora.AddDays(-24)),
                Solicitud(ana, luis, EstadoSolicitud.Aceptada, ahora.AddDays(-22)),
                Solicitud(carlos, maria, EstadoSolicitud.Aceptada, ahora.AddDays(-21)),
                Solicitud(sofia, carlos, EstadoSolicitud.Aceptada, ahora.AddDays(-15)),
                Solicitud(luis, sofia, EstadoSolicitud.Aceptada, ahora.AddDays(-12)),
                // Pendientes y otros estados para probar la pantalla de solicitudes
                Solicitud(pedro, ana, EstadoSolicitud.EnEspera, ahora.AddDays(-1)),   // ana la tiene pendiente por responder
                Solicitud(ana, sofia, EstadoSolicitud.EnEspera, ahora.AddDays(-2)),   // ana la envió y espera respuesta
                Solicitud(pedro, carlos, EstadoSolicitud.Rechazada, ahora.AddDays(-2)),
                Solicitud(maria, sofia, EstadoSolicitud.Cancelada, ahora.AddDays(-5))
            };

            _context.Friendships.AddRange(amistades);
            _context.Set<FriendRequest>().AddRange(solicitudes);

            // ── Publicaciones (todas llevan imagen o enlace de YouTube, como pide el enunciado)
            var p1 = NuevoPost(ana, "¡Bienvenidos a LinkUp Pro! Esta es mi primera publicación.", "/seed/posts/paisaje.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-10));
            var p2 = NuevoPost(ana, "Big Buck Bunny, lo mejor del cine libre. Aquí los comentarios están desactivados.", null, "https://www.youtube.com/watch?v=aqz-KE-bpKQ", Privacidad.SoloAmigos, false, ahora.AddDays(-8));
            var p3 = NuevoPost(ana, "Esta publicación solo la puedo ver yo.", "/seed/posts/ciudad.svg", null, Privacidad.SoloYo, true, ahora.AddDays(-6));
            var p4 = NuevoPost(ana, "Publicación editada: corregí un error de ortografía.", null, "https://www.youtube.com/watch?v=eRsGyueVLvQ", Privacidad.SoloAmigos, true, ahora.AddDays(-4));
            p4.FechaModificacion = ahora.AddDays(-3);
            var p5 = NuevoPost(ana, "Esta publicación fue eliminada.", "/seed/posts/atardecer.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-5));
            p5.IsDeleted = true;

            var p6 = NuevoPost(carlos, "Probando el sistema de comentarios, ¿qué opinan?", "/seed/posts/ciudad.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-9));
            var p7 = NuevoPost(carlos, "Mi video favorito de esta semana.", null, "https://www.youtube.com/watch?v=aqz-KE-bpKQ", Privacidad.SoloAmigos, true, ahora.AddDays(-2));
            var p8 = NuevoPost(maria, "Fin de semana de descanso.", "/seed/posts/atardecer.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-7));
            var p9 = NuevoPost(maria, "Solo para mí.", null, "https://www.youtube.com/watch?v=eRsGyueVLvQ", Privacidad.SoloYo, true, ahora.AddDays(-3));
            var p10 = NuevoPost(luis, "Partida de Battleship esta noche, ¿quién se anima?", "/seed/posts/paisaje.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-1));
            var p11 = NuevoPost(sofia, "Hola a todos desde LinkUp Pro.", "/seed/posts/ciudad.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-2));
            var p12 = NuevoPost(pedro, "Soy nuevo aquí y todavía no tengo amigos.", "/seed/posts/paisaje.svg", null, Privacidad.SoloAmigos, true, ahora.AddDays(-1));

            var posts = new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12 };
            _context.Posts.AddRange(posts);

            // ── Comentarios y respuestas anidadas
            var c1 = NuevoComentario(p1, carlos, null, "¡Qué buena foto, Ana!", ahora.AddDays(-9).AddHours(1));
            var c2 = NuevoComentario(p1, maria, c1, "Totalmente de acuerdo con Carlos.", ahora.AddDays(-9).AddHours(2));
            var c3 = NuevoComentario(p1, ana, c2, "¡Gracias a los dos!", ahora.AddDays(-9).AddHours(3));
            var c4 = NuevoComentario(p1, luis, null, "Bienvenida a la plataforma.", ahora.AddDays(-8).AddHours(5));
            c4.FechaModificacion = ahora.AddDays(-8).AddHours(6); // comentario editado
            var c5 = NuevoComentario(p1, maria, null, "Este comentario se eliminó después.", ahora.AddDays(-8).AddHours(8));
            c5.IsDeleted = true; // eliminado pero con respuestas: se verá "Este comentario fue eliminado."
            var c6 = NuevoComentario(p1, ana, c5, "Respondo a un comentario que ya no existe.", ahora.AddDays(-8).AddHours(9));
            var c7 = NuevoComentario(p1, carlos, null, "Comentario eliminado sin respuestas.", ahora.AddDays(-7));
            c7.IsDeleted = true; // eliminado sin respuestas: desaparece

            var c8 = NuevoComentario(p2, carlos, null, "Gran película.", ahora.AddDays(-8).AddHours(4)); // antes de desactivar los comentarios
            var c9 = NuevoComentario(p6, ana, null, "Me parece muy bien el sistema.", ahora.AddDays(-9).AddHours(2));
            var c10 = NuevoComentario(p6, sofia, null, "Yo también lo estoy probando.", ahora.AddDays(-9).AddHours(3)); // sofia no es amiga de ana
            var c11 = NuevoComentario(p6, carlos, c10, "Qué bueno, Sofía.", ahora.AddDays(-9).AddHours(4));
            var c12 = NuevoComentario(p10, ana, null, "¡Cuenta conmigo!", ahora.AddDays(-1).AddHours(2));
            var c13 = NuevoComentario(p10, luis, c12, "Perfecto, te espero.", ahora.AddDays(-1).AddHours(3));
            var c14 = NuevoComentario(p5, maria, null, "Linda imagen.", ahora.AddDays(-5).AddHours(2)); // la publicación se eliminó después

            var comentarios = new[] { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14 };
            _context.Comments.AddRange(comentarios);

            // ── Reacciones (una por usuario y publicación)
            var reacciones = new[]
            {
                Reaccion(p1, carlos, TipoReaccion.MeGusta, ahora.AddDays(-9)),
                Reaccion(p1, maria, TipoReaccion.MeGusta, ahora.AddDays(-9)),
                Reaccion(p1, luis, TipoReaccion.NoMeGusta, ahora.AddDays(-8)),
                Reaccion(p2, carlos, TipoReaccion.MeGusta, ahora.AddDays(-8)),
                Reaccion(p2, maria, TipoReaccion.MeGusta, ahora.AddDays(-7)),
                Reaccion(p4, carlos, TipoReaccion.MeGusta, ahora.AddDays(-3)),
                Reaccion(p5, carlos, TipoReaccion.MeGusta, ahora.AddDays(-5)),
                Reaccion(p6, ana, TipoReaccion.MeGusta, ahora.AddDays(-9)),
                Reaccion(p6, maria, TipoReaccion.MeGusta, ahora.AddDays(-9)),
                Reaccion(p6, sofia, TipoReaccion.MeGusta, ahora.AddDays(-9)),
                Reaccion(p7, ana, TipoReaccion.NoMeGusta, ahora.AddDays(-1)),
                Reaccion(p7, maria, TipoReaccion.MeGusta, ahora.AddDays(-1)),
                Reaccion(p8, ana, TipoReaccion.MeGusta, ahora.AddDays(-6)),
                Reaccion(p8, carlos, TipoReaccion.MeGusta, ahora.AddDays(-6)),
                Reaccion(p8, luis, TipoReaccion.MeGusta, ahora.AddDays(-6)),
                Reaccion(p10, ana, TipoReaccion.MeGusta, ahora.AddDays(-1)),
                Reaccion(p10, carlos, TipoReaccion.MeGusta, ahora.AddHours(-20)),
                Reaccion(p11, carlos, TipoReaccion.MeGusta, ahora.AddDays(-2))
            };
            _context.Reactions.AddRange(reacciones);

            // Se guarda aquí para que publicaciones y comentarios ya tengan Id
            await _context.SaveChangesAsync();

            // ── Notificaciones: las mismas que habría generado la aplicación (sin autoaviso).
            // Las de más de 5 días quedan como leídas y el resto como no leídas.
            var notificaciones = new List<Notification>();
            foreach (var comentario in comentarios)
            {
                var padre = comentario.ParentComment;
                var destinatarioId = padre?.AutorId ?? comentario.Post!.AutorId;
                if (destinatarioId == comentario.AutorId)
                    continue;

                var actor = UsuarioPorId(comentario.AutorId, ana, carlos, maria, luis, sofia, pedro);
                notificaciones.Add(new Notification
                {
                    DestinatarioId = destinatarioId,
                    ActorId = actor.Id,
                    Tipo = padre is null ? TipoNotificacion.Comentario : TipoNotificacion.Respuesta,
                    Descripcion = padre is null
                        ? $"{actor.UserName} comentó tu publicación."
                        : $"{actor.UserName} respondió tu comentario.",
                    PostId = comentario.PostId,
                    CommentId = comentario.Id,
                    Leida = comentario.FechaCreacion < ahora.AddDays(-5),
                    Fecha = comentario.FechaCreacion
                });
            }

            foreach (var reaccion in reacciones)
            {
                var post = reaccion.Post!;
                if (post.AutorId == reaccion.UsuarioId)
                    continue;

                var actor = UsuarioPorId(reaccion.UsuarioId, ana, carlos, maria, luis, sofia, pedro);
                var texto = reaccion.Tipo == TipoReaccion.MeGusta ? "Me gusta" : "No me gusta";
                notificaciones.Add(new Notification
                {
                    DestinatarioId = post.AutorId,
                    ActorId = actor.Id,
                    Tipo = TipoNotificacion.Reaccion,
                    Descripcion = $"{actor.UserName} reaccionó con {texto} a tu publicación.",
                    PostId = post.Id,
                    Leida = reaccion.Fecha < ahora.AddDays(-5),
                    Fecha = reaccion.Fecha
                });
            }

            _context.Notifications.AddRange(notificaciones);

            // ── Partidas de Battleship
            // 1) ana vs carlos: finalizada, ganó ana hundiendo todos los barcos
            var g1 = PartidaGanadaPorHundimiento(ana, carlos, ahora.AddDays(-6));
            // 2) maria vs ana: finalizada por rendición de ana
            var g2 = PartidaConRendicion(maria, ana, ahora.AddDays(-4));
            // 3) ana vs luis: en curso, le toca atacar a ana
            var g3 = PartidaEnCurso(ana, luis, ahora.AddDays(-1));
            // 4) carlos vs sofia: sofia todavía no posicionó ningún barco
            var g4 = PartidaPosicionando(carlos, sofia, ahora.AddHours(-10), creadorPosicionado: true);
            // 5) maria vs ana: ana todavía no posicionó ningún barco (la anterior entre ellas ya terminó)
            var g5 = PartidaPosicionando(maria, ana, ahora.AddHours(-5), creadorPosicionado: true);

            _context.Games.AddRange(g1, g2, g3, g4, g5);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            _logger.LogInformation("Seed: datos de prueba cargados. Contraseña de todas las cuentas: {Password}", PasswordDePrueba);
            return true;
        }

        // ───────────────────────────── Usuarios, amistades, publicaciones ─────────────────────────────

        private async Task<ApplicationUser> CrearUsuarioAsync(
            string userName, string nombre, string apellido, string telefono, bool activo, DateTime fechaRegistro)
        {
            var usuario = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName}@linkup.test",
                EmailConfirmed = activo,
                PhoneNumber = telefono,
                Nombre = nombre,
                Apellido = apellido,
                FotoPerfil = $"/seed/avatars/{userName}.svg",
                FechaRegistro = fechaRegistro
            };

            var resultado = await _userManager.CreateAsync(usuario, PasswordDePrueba);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    $"No se pudo crear el usuario de prueba '{userName}': {string.Join("; ", resultado.Errors.Select(e => e.Description))}");

            return usuario;
        }

        private static ApplicationUser UsuarioPorId(string id, params ApplicationUser[] usuarios) =>
            usuarios.First(u => u.Id == id);

        private static Friendship Amistad(ApplicationUser a, ApplicationUser b, DateTime fecha)
        {
            // Siempre en el mismo orden, como exige el índice único de la tabla
            var (usuarioA, usuarioB) = UserPair.Order(a.Id, b.Id);
            return new Friendship
            {
                UsuarioAId = usuarioA,
                UsuarioBId = usuarioB,
                Estado = EstadoAmistad.Activa,
                FechaCreacion = fecha
            };
        }

        private static FriendRequest Solicitud(ApplicationUser emisor, ApplicationUser receptor, EstadoSolicitud estado, DateTime fechaEnvio) =>
            new()
            {
                EmisorId = emisor.Id,
                ReceptorId = receptor.Id,
                Estado = estado,
                FechaEnvio = fechaEnvio,
                FechaRespuesta = estado is EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada ? fechaEnvio.AddHours(3) : null,
                FechaCancelacion = estado == EstadoSolicitud.Cancelada ? fechaEnvio.AddHours(1) : null
            };

        private static Post NuevoPost(
            ApplicationUser autor, string contenido, string? rutaImagen, string? urlYouTube,
            Privacidad privacidad, bool permitirComentarios, DateTime fecha) =>
            new()
            {
                AutorId = autor.Id,
                Contenido = contenido,
                TipoContenido = rutaImagen is not null ? TipoContenido.Imagen : TipoContenido.YouTube,
                RutaImagen = rutaImagen,
                UrlYouTube = urlYouTube,
                Privacidad = privacidad,
                PermitirComentarios = permitirComentarios,
                FechaCreacion = fecha
            };

        private static Comment NuevoComentario(Post post, ApplicationUser autor, Comment? padre, string contenido, DateTime fecha) =>
            new()
            {
                Post = post,
                AutorId = autor.Id,
                ParentComment = padre,
                Contenido = contenido,
                FechaCreacion = fecha
            };

        private static Reaction Reaccion(Post post, ApplicationUser usuario, TipoReaccion tipo, DateTime fecha) =>
            new()
            {
                Post = post,
                UsuarioId = usuario.Id,
                Tipo = tipo,
                Fecha = fecha
            };

        // ───────────────────────────── Partidas ─────────────────────────────

        private static Game NuevaPartida(ApplicationUser creador, ApplicationUser oponente, DateTime inicio) =>
            new()
            {
                CreadorId = creador.Id,
                OponenteId = oponente.Id,
                FechaInicio = inicio,
                Estado = EstadoPartida.Posicionando
            };

        private static void AgregarBarcosPosicionados(
            Game partida, string jugadorId, (int Tamano, int Fila, int Columna, DireccionBarco Direccion)[] tablero)
        {
            foreach (var barco in tablero)
            {
                partida.Ships.Add(new Ship
                {
                    JugadorId = jugadorId,
                    Tamano = barco.Tamano,
                    FilaInicio = barco.Fila,
                    ColumnaInicio = barco.Columna,
                    Direccion = barco.Direccion,
                    Posicionado = true
                });
            }
        }

        private static void AgregarBarcosPendientes(Game partida, string jugadorId)
        {
            foreach (var tamano in BattleshipRules.ShipSizes)
                partida.Ships.Add(new Ship { JugadorId = jugadorId, Tamano = tamano, Posicionado = false });
        }

        private static List<(int Fila, int Columna)> CeldasDe((int Tamano, int Fila, int Columna, DireccionBarco Direccion)[] tablero) =>
            tablero.SelectMany(b => BattleshipRules.GetShipCells(b.Fila, b.Columna, b.Tamano, b.Direccion)).ToList();

        private static void AgregarAtaque(Game partida, string atacanteId, (int Fila, int Columna) celda, bool acierto, DateTime fecha) =>
            partida.Moves.Add(new Move
            {
                AtacanteId = atacanteId,
                Fila = celda.Fila,
                Columna = celda.Columna,
                Acierto = acierto,
                Fecha = fecha
            });

        // ana (tablero A) vs carlos (tablero B): ana ataca primero y hunde los 17 casilleros; carlos acertó solo 8 veces
        private static Game PartidaGanadaPorHundimiento(ApplicationUser ganador, ApplicationUser perdedor, DateTime inicio)
        {
            var partida = NuevaPartida(ganador, perdedor, inicio);
            AgregarBarcosPosicionados(partida, ganador.Id, TableroA);
            AgregarBarcosPosicionados(partida, perdedor.Id, TableroB);

            // Ataques del ganador: los 17 aciertos intercalados con 6 fallos (ninguno cae sobre un barco de tablero B)
            var aciertosGanador = CeldasDe(TableroB);
            var fallosGanador = new List<(int, int)> { (11, 5), (9, 3), (3, 3), (6, 10), (2, 5), (11, 8) };
            var ataquesGanador = new List<((int Fila, int Columna) Celda, bool Acierto)>();
            var fallo = 0;
            for (var i = 0; i < aciertosGanador.Count; i++)
            {
                ataquesGanador.Add((aciertosGanador[i], true));
                if ((i + 1) % 3 == 0 && fallo < fallosGanador.Count)
                    ataquesGanador.Add((fallosGanador[fallo++], false));
            }

            // Ataques del perdedor: 8 aciertos sobre el tablero A y 14 fallos
            var ataquesPerdedor = new List<((int Fila, int Columna) Celda, bool Acierto)>();
            foreach (var celda in new[] { (0, 0), (0, 1), (0, 2), (2, 0), (2, 1), (4, 0), (6, 2), (8, 0) })
                ataquesPerdedor.Add((celda, true));
            foreach (var celda in new[]
                     { (5, 5), (5, 6), (5, 8), (5, 9), (7, 3), (7, 4), (7, 5), (9, 9), (9, 10), (9, 11), (10, 5), (10, 6), (11, 2), (11, 3) })
                ataquesPerdedor.Add((celda, false));

            // Se alternan: primero el creador (ganador), luego el oponente, cada 15 minutos
            var momento = inicio.AddMinutes(30);
            var total = Math.Max(ataquesGanador.Count, ataquesPerdedor.Count);
            for (var i = 0; i < total; i++)
            {
                if (i < ataquesGanador.Count)
                {
                    AgregarAtaque(partida, ganador.Id, ataquesGanador[i].Celda, ataquesGanador[i].Acierto, momento);
                    momento = momento.AddMinutes(15);
                }
                if (i < ataquesPerdedor.Count && i < ataquesGanador.Count - 1)
                {
                    AgregarAtaque(partida, perdedor.Id, ataquesPerdedor[i].Celda, ataquesPerdedor[i].Acierto, momento);
                    momento = momento.AddMinutes(15);
                }
            }

            partida.Estado = EstadoPartida.Finalizada;
            partida.GanadorId = ganador.Id;
            partida.MotivoFin = MotivoFinPartida.Hundidos;
            partida.TurnoUsuarioId = null;
            partida.FechaFin = momento;
            return partida;
        }

        // El oponente se rinde después de unas pocas jugadas: gana el creador
        private static Game PartidaConRendicion(ApplicationUser creador, ApplicationUser oponente, DateTime inicio)
        {
            var partida = NuevaPartida(creador, oponente, inicio);
            AgregarBarcosPosicionados(partida, creador.Id, TableroA);
            AgregarBarcosPosicionados(partida, oponente.Id, TableroB);

            var momento = inicio.AddMinutes(20);
            AgregarAtaque(partida, creador.Id, (0, 11), true, momento);
            AgregarAtaque(partida, oponente.Id, (3, 3), false, momento.AddMinutes(30));
            AgregarAtaque(partida, creador.Id, (1, 11), true, momento.AddMinutes(60));
            AgregarAtaque(partida, oponente.Id, (7, 7), false, momento.AddMinutes(90));
            AgregarAtaque(partida, creador.Id, (6, 6), false, momento.AddMinutes(120));

            partida.Estado = EstadoPartida.Finalizada;
            partida.GanadorId = creador.Id;
            partida.MotivoFin = MotivoFinPartida.Rendicion;
            partida.TurnoUsuarioId = null;
            partida.FechaFin = momento.AddMinutes(150);
            return partida;
        }

        // Cuatro ataques por jugador: vuelve a tocarle al creador, con el turno asignado hace pocas horas
        private static Game PartidaEnCurso(ApplicationUser creador, ApplicationUser oponente, DateTime inicio)
        {
            var partida = NuevaPartida(creador, oponente, inicio);
            AgregarBarcosPosicionados(partida, creador.Id, TableroA);
            AgregarBarcosPosicionados(partida, oponente.Id, TableroB);

            var momento = DateTime.UtcNow.AddHours(-9);
            var ataquesCreador = new[] { ((0, 11), true), ((1, 11), true), ((5, 5), false), ((9, 9), false) };
            var ataquesOponente = new[] { ((0, 0), true), ((11, 11), false), ((10, 10), false), ((7, 7), false) };

            for (var i = 0; i < 4; i++)
            {
                AgregarAtaque(partida, creador.Id, ataquesCreador[i].Item1, ataquesCreador[i].Item2, momento);
                momento = momento.AddHours(1);
                AgregarAtaque(partida, oponente.Id, ataquesOponente[i].Item1, ataquesOponente[i].Item2, momento);
                momento = momento.AddHours(1);
            }

            partida.Estado = EstadoPartida.EnCurso;
            partida.TurnoUsuarioId = creador.Id;
            partida.FechaAsignacionTurno = momento;
            return partida;
        }

        // El creador ya posicionó sus 5 barcos; el oponente todavía no ha posicionado ninguno
        private static Game PartidaPosicionando(ApplicationUser creador, ApplicationUser oponente, DateTime inicio, bool creadorPosicionado)
        {
            var partida = NuevaPartida(creador, oponente, inicio);

            if (creadorPosicionado)
                AgregarBarcosPosicionados(partida, creador.Id, TableroA);
            else
                AgregarBarcosPendientes(partida, creador.Id);

            AgregarBarcosPendientes(partida, oponente.Id);
            return partida;
        }
    }
}
