using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.Persistence;

public static class DatabaseSeeder
{
    private static readonly string[] IdentityRoles =
    [
        "SuperAdmin",
        "Owner",
        "Admin",
        "Recepcionista",
        "Profesional",
        "Cliente"
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ReservaFlowDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        await SeedIdentityRolesAsync(roleManager);
        await SeedCatalogsAsync(dbContext, cancellationToken);
        await SeedSuperAdminAsync(userManager, configuration);
    }

    private static async Task SeedIdentityRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in IdentityRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task SeedCatalogsAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        await SeedRubrosAsync(dbContext, cancellationToken);
        await SeedRolesNegocioAsync(dbContext, cancellationToken);
        await SeedTiposPrestadorAsync(dbContext, cancellationToken);
        await SeedTiposCampoAsync(dbContext, cancellationToken);
        await SeedEstadosCitaAsync(dbContext, cancellationToken);
        await SeedCanalesNotificacionAsync(dbContext, cancellationToken);
        await SeedEstadosNotificacionAsync(dbContext, cancellationToken);
        await SeedEstadosPagoAsync(dbContext, cancellationToken);
        await SeedMetodosPagoAsync(dbContext, cancellationToken);
        await SeedTiposNotificacionAsync(dbContext, cancellationToken);
        await SeedUbicacionesAsync(dbContext, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRubrosAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Barbería", "Reservas para barberías y peluquerías"),
            ("Veterinaria", "Reservas para consultas y controles veterinarios"),
            ("Taller mecánico", "Reservas para servicios y diagnósticos de vehículos"),
            ("Centro de estética", "Reservas para servicios estéticos"),
            ("Profesional independiente", "Reservas para psicólogos, abogados, consultores u otros profesionales"),
            ("Cancha deportiva", "Reservas de canchas y espacios deportivos"),
            ("Restaurante", "Reservas de mesas o turnos de atención"),
            ("Salud", "Reservas para consultas médicas, dentales o kinesiológicas")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.Rubros.FirstOrDefaultAsync(rubro => nameAliases.Contains(rubro.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.Rubros.Add(new Rubro { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedRolesNegocioAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Owner", "Dueño del negocio"),
            ("Admin", "Administrador del negocio"),
            ("Recepcionista", "Usuario que gestiona citas y clientes"),
            ("Profesional", "Prestador que atiende citas")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.RolesNegocio.FirstOrDefaultAsync(role => nameAliases.Contains(role.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.RolesNegocio.Add(new RolNegocio { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedTiposPrestadorAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Profesional", "Persona que presta un servicio"),
            ("Recurso", "Recurso reservable como cancha, box, cabina, mesa o sala")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.TiposPrestador.FirstOrDefaultAsync(tipo => nameAliases.Contains(tipo.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.TiposPrestador.Add(new TipoPrestador { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedTiposCampoAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Texto", "Campo de texto corto o largo"),
            ("Número", "Campo numérico"),
            ("Fecha", "Campo de fecha"),
            ("Booleano", "Campo verdadero/falso"),
            ("Select", "Campo de selección con opciones")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.TiposCampo.FirstOrDefaultAsync(tipo => nameAliases.Contains(tipo.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.TiposCampo.Add(new TipoCampo { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedEstadosCitaAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Pendiente", "La cita fue solicitada y está pendiente de confirmación", false),
            ("Confirmada", "La cita fue confirmada por el negocio", false),
            ("Reagendada", "La cita fue cambiada de fecha u hora", false),
            ("Cancelada", "La cita fue cancelada", true),
            ("Atendida", "El cliente fue atendido", true),
            ("No asistió", "El cliente no asistió a la cita", true),
            ("Pendiente de pago", "La cita requiere pago o confirmación de pago", false)
        };

        foreach (var (nombre, descripcion, esEstadoFinal) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.EstadosCita.FirstOrDefaultAsync(estado => nameAliases.Contains(estado.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.EstadosCita.Add(new EstadoCita
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    EsEstadoFinal = esEstadoFinal,
                    Activo = true
                });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.EsEstadoFinal = esEstadoFinal;
            item.Activo = true;
        }
    }

    private static async Task SeedCanalesNotificacionAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Email", "Notificación por correo electrónico"),
            ("WhatsApp", "Notificación por WhatsApp"),
            ("SMS", "Notificación por mensaje de texto"),
            ("Interna", "Notificación interna dentro del sistema")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.CanalesNotificacion.FirstOrDefaultAsync(canal => nameAliases.Contains(canal.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.CanalesNotificacion.Add(new CanalNotificacion { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedEstadosNotificacionAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Pendiente", "La notificación está pendiente de envío"),
            ("Enviada", "La notificación fue enviada correctamente"),
            ("Error", "Ocurrió un error al enviar la notificación"),
            ("Cancelada", "La notificación fue cancelada")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.EstadosNotificacion.FirstOrDefaultAsync(estado => nameAliases.Contains(estado.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.EstadosNotificacion.Add(new EstadoNotificacion { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedTiposNotificacionAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("ConfirmacionReserva", "Confirmación de reserva creada"),
            ("Recordatorio24Horas", "Recordatorio 24 horas antes de la cita"),
            ("Recordatorio2Horas", "Recordatorio 2 horas antes de la cita"),
            ("CancelacionReserva", "Aviso de cancelación de reserva"),
            ("ReagendamientoReserva", "Aviso de cambio de fecha u hora"),
            ("PostAtencion", "Mensaje posterior a la atención"),
            ("InvitacionNegocio", "Invitación para unirse a un negocio"),
            ("NuevaResenaNegocio", "Aviso interno cuando un negocio recibe una nueva reseña"),
            ("AlertaResenaNegocio", "Alerta operativa interna por una reseña con baja puntuación")
        };

        foreach (var (nombre, descripcion) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.TiposNotificacion.FirstOrDefaultAsync(tipo => nameAliases.Contains(tipo.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.TiposNotificacion.Add(new TipoNotificacion { Nombre = nombre, Descripcion = descripcion, Activo = true });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.Activo = true;
        }
    }

    private static async Task SeedEstadosPagoAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Pendiente", "El pago fue creado y está pendiente de confirmación", false),
            ("Pagado", "El pago fue confirmado correctamente", true),
            ("Parcialmente devuelto", "El pago fue confirmado y tiene una devolución parcial registrada", false),
            ("Devuelto", "El pago fue devuelto completamente", true),
            ("Rechazado", "El pago fue rechazado por el proveedor", true),
            ("Anulado", "El pago fue anulado por el proveedor", true),
            ("Error", "El pago no pudo procesarse correctamente", true)
        };

        foreach (var (nombre, descripcion, esEstadoFinal) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.EstadosPago.FirstOrDefaultAsync(estado => nameAliases.Contains(estado.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.EstadosPago.Add(new EstadoPago
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    EsEstadoFinal = esEstadoFinal,
                    Activo = true
                });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.EsEstadoFinal = esEstadoFinal;
            item.Activo = true;
        }
    }

    private static async Task SeedMetodosPagoAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var items = new[]
        {
            ("Flow", "Pago online procesado por Flow", false, true),
            ("Efectivo", "Pago manual recibido en efectivo en el negocio", true, false),
            ("Transferencia bancaria", "Pago manual recibido por transferencia bancaria", true, false),
            ("Tarjeta débito", "Pago manual recibido con tarjeta de débito", true, false),
            ("Tarjeta crédito", "Pago manual recibido con tarjeta de crédito", true, false),
            ("POS", "Pago manual recibido mediante terminal POS o red de pago", true, false),
            ("Cheque", "Pago manual recibido con cheque", true, false),
            ("Otro", "Otro método de pago manual", true, false)
        };

        foreach (var (nombre, descripcion, esManual, esOnline) in items)
        {
            var nameAliases = BuildNameAliases(nombre);
            var item = await dbContext.MetodosPago.FirstOrDefaultAsync(metodo => nameAliases.Contains(metodo.Nombre), cancellationToken);
            if (item is null)
            {
                dbContext.MetodosPago.Add(new MetodoPago
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    EsManual = esManual,
                    EsOnline = esOnline,
                    Activo = true
                });
                continue;
            }

            item.Nombre = nombre;
            item.Descripcion = descripcion;
            item.EsManual = esManual;
            item.EsOnline = esOnline;
            item.Activo = true;
        }
    }

    private static async Task SeedUbicacionesAsync(ReservaFlowDbContext dbContext, CancellationToken cancellationToken)
    {
        var chile = await dbContext.Paises.FirstOrDefaultAsync(
            pais => pais.CodigoIso2 == "CL",
            cancellationToken);

        if (chile is null)
        {
            chile = new Pais
            {
                Nombre = "Chile",
                CodigoIso2 = "CL",
                Activo = true
            };

            dbContext.Paises.Add(chile);
        }
        else
        {
            chile.Nombre = "Chile";
            chile.Activo = true;
        }

        var ciudades = new Dictionary<string, string[]>
        {
            ["Arica y Parinacota"] =
            [
                "Arica",
                "Camarones",
                "General Lagos",
                "Putre"
            ],
            ["Tarapacá"] =
            [
                "Alto Hospicio",
                "Camiña",
                "Colchane",
                "Huara",
                "Iquique",
                "Pica",
                "Pozo Almonte"
            ],
            ["Antofagasta"] =
            [
                "Antofagasta",
                "Calama",
                "María Elena",
                "Mejillones",
                "Ollagüe",
                "San Pedro de Atacama",
                "Sierra Gorda",
                "Taltal",
                "Tocopilla"
            ],
            ["Atacama"] =
            [
                "Alto del Carmen",
                "Caldera",
                "Chañaral",
                "Copiapó",
                "Diego de Almagro",
                "Freirina",
                "Huasco",
                "Tierra Amarilla",
                "Vallenar"
            ],
            ["Coquimbo"] =
            [
                "Andacollo",
                "Canela",
                "Combarbalá",
                "Coquimbo",
                "Illapel",
                "La Higuera",
                "La Serena",
                "Los Vilos",
                "Monte Patria",
                "Ovalle",
                "Paiguano",
                "Punitaqui",
                "Río Hurtado",
                "Salamanca",
                "Vicuña"
            ],
            ["Valparaíso"] =
            [
                "Algarrobo",
                "Cabildo",
                "Calera",
                "Calle Larga",
                "Cartagena",
                "Casablanca",
                "Catemu",
                "Concón",
                "El Quisco",
                "El Tabo",
                "Hijuelas",
                "Isla de Pascua",
                "Juan Fernández",
                "La Cruz",
                "La Ligua",
                "Limache",
                "Llaillay",
                "Los Andes",
                "Nogales",
                "Olmué",
                "Panquehue",
                "Papudo",
                "Petorca",
                "Puchuncaví",
                "Putaendo",
                "Quillota",
                "Quilpué",
                "Quintero",
                "Rinconada",
                "San Antonio",
                "San Esteban",
                "San Felipe",
                "Santa María",
                "Santo Domingo",
                "Valparaíso",
                "Villa Alemana",
                "Viña del Mar",
                "Zapallar"
            ],
            ["Metropolitana de Santiago"] =
            [
                "Alhué",
                "Buin",
                "Calera de Tango",
                "Cerrillos",
                "Cerro Navia",
                "Colina",
                "Conchalí",
                "Curacaví",
                "El Bosque",
                "El Monte",
                "Estación Central",
                "Huechuraba",
                "Independencia",
                "Isla de Maipo",
                "La Cisterna",
                "La Florida",
                "La Granja",
                "La Pintana",
                "La Reina",
                "Lampa",
                "Las Condes",
                "Lo Barnechea",
                "Lo Espejo",
                "Lo Prado",
                "Macul",
                "Maipú",
                "María Pinto",
                "Melipilla",
                "Ñuñoa",
                "Padre Hurtado",
                "Paine",
                "Pedro Aguirre Cerda",
                "Peñaflor",
                "Peñalolén",
                "Pirque",
                "Providencia",
                "Pudahuel",
                "Puente Alto",
                "Quilicura",
                "Quinta Normal",
                "Recoleta",
                "Renca",
                "San Bernardo",
                "San Joaquín",
                "San José de Maipo",
                "San Miguel",
                "San Pedro",
                "San Ramón",
                "Santiago",
                "Talagante",
                "Tiltil",
                "Vitacura"
            ],
            ["Libertador General Bernardo O'Higgins"] =
            [
                "Chépica",
                "Chimbarongo",
                "Codegua",
                "Coinco",
                "Coltauco",
                "Doñihue",
                "Graneros",
                "La Estrella",
                "Las Cabras",
                "Litueche",
                "Lolol",
                "Machalí",
                "Malloa",
                "Marchihue",
                "Mostazal",
                "Nancagua",
                "Navidad",
                "Olivar",
                "Palmilla",
                "Paredones",
                "Peralillo",
                "Peumo",
                "Pichidegua",
                "Pichilemu",
                "Placilla",
                "Pumanque",
                "Quinta de Tilcoco",
                "Rancagua",
                "Rengo",
                "Requínoa",
                "San Fernando",
                "San Vicente",
                "Santa Cruz"
            ],
            ["Maule"] =
            [
                "Cauquenes",
                "Chanco",
                "Colbún",
                "Constitución",
                "Curepto",
                "Curicó",
                "Empedrado",
                "Hualañé",
                "Licantén",
                "Linares",
                "Longaví",
                "Maule",
                "Molina",
                "Parral",
                "Pelarco",
                "Pelluhue",
                "Pencahue",
                "Rauco",
                "Retiro",
                "Río Claro",
                "Romeral",
                "Sagrada Familia",
                "San Clemente",
                "San Javier",
                "San Rafael",
                "Talca",
                "Teno",
                "Vichuquén",
                "Villa Alegre",
                "Yerbas Buenas"
            ],
            ["Ñuble"] =
            [
                "Bulnes",
                "Chillán",
                "Chillán Viejo",
                "Cobquecura",
                "Coelemu",
                "Coihueco",
                "El Carmen",
                "Ninhue",
                "Ñiquén",
                "Pemuco",
                "Pinto",
                "Portezuelo",
                "Quillón",
                "Quirihue",
                "Ranquil",
                "San Carlos",
                "San Fabián",
                "San Ignacio",
                "San Nicolás",
                "Treguaco",
                "Yungay"
            ],
            ["Biobío"] =
            [
                "Alto Biobío",
                "Antuco",
                "Arauco",
                "Cabrero",
                "Cañete",
                "Chiguayante",
                "Concepción",
                "Contulmo",
                "Coronel",
                "Curanilahue",
                "Florida",
                "Hualpén",
                "Hualqui",
                "Laja",
                "Lebu",
                "Los Alamos",
                "Los Ángeles",
                "Lota",
                "Mulchén",
                "Nacimiento",
                "Negrete",
                "Penco",
                "Quilaco",
                "Quilleco",
                "San Pedro de la Paz",
                "San Rosendo",
                "Santa Bárbara",
                "Santa Juana",
                "Talcahuano",
                "Tirúa",
                "Tomé",
                "Tucapel",
                "Yumbel"
            ],
            ["La Araucanía"] =
            [
                "Angol",
                "Carahue",
                "Cholchol",
                "Collipulli",
                "Cunco",
                "Curacautín",
                "Curarrehue",
                "Ercilla",
                "Freire",
                "Galvarino",
                "Gorbea",
                "Lautaro",
                "Loncoche",
                "Lonquimay",
                "Los Sauces",
                "Lumaco",
                "Melipeuco",
                "Nueva Imperial",
                "Padre Las Casas",
                "Perquenco",
                "Pitrufquen",
                "Pucón",
                "Purén",
                "Renaico",
                "Saavedra",
                "Temuco",
                "Teodoro Schmidt",
                "Toltén",
                "Traiguén",
                "Victoria",
                "Vilcún",
                "Villarrica"
            ],
            ["Los Ríos"] =
            [
                "Corral",
                "Futrono",
                "La Unión",
                "Lago Ranco",
                "Lanco",
                "Los Lagos",
                "Máfil",
                "Mariquina",
                "Paillaco",
                "Panguipulli",
                "Río Bueno",
                "Valdivia"
            ],
            ["Los Lagos"] =
            [
                "Ancud",
                "Calbuco",
                "Castro",
                "Chaitén",
                "Chonchi",
                "Cochamó",
                "Curaco de Vélez",
                "Dalcahue",
                "Fresia",
                "Frutillar",
                "Futaleufú",
                "Hualaihué",
                "Llanquihue",
                "Los Muermos",
                "Maullín",
                "Osorno",
                "Palena",
                "Puerto Montt",
                "Puerto Octay",
                "Puerto Varas",
                "Puqueldón",
                "Purranque",
                "Puyehue",
                "Queilén",
                "Quellón",
                "Quemchi",
                "Quinchao",
                "Río Negro",
                "San Juan de la Costa",
                "San Pablo"
            ],
            ["Aysén del General Carlos Ibáñez del Campo"] =
            [
                "Aysén",
                "Chile Chico",
                "Cisnes",
                "Cochrane",
                "Coyhaique",
                "Guaitecas",
                "Lago Verde",
                "O'Higgins",
                "Río Ibáñez",
                "Tortel"
            ],
            ["Magallanes y de la Antártica Chilena"] =
            [
                "Antártica",
                "Cabo de Hornos",
                "Laguna Blanca",
                "Natales",
                "Porvenir",
                "Primavera",
                "Punta Arenas",
                "Río Verde",
                "San Gregorio",
                "Timaukel",
                "Torres del Paine"
            ]
        };

        var ciudadesOficiales = ciudades.Keys
            .SelectMany(BuildNameAliases)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ciudadesExistentes = await dbContext.Ciudades
            .Include(ciudad => ciudad.Pais)
            .Include(ciudad => ciudad.Comunas)
            .Where(ciudad => ciudad.Pais.CodigoIso2 == "CL")
            .ToArrayAsync(cancellationToken);

        foreach (var ciudadExistente in ciudadesExistentes.Where(ciudad => !ciudadesOficiales.Contains(ciudad.Nombre)))
        {
            ciudadExistente.Activo = false;

            foreach (var comunaExistente in ciudadExistente.Comunas)
            {
                comunaExistente.Activo = false;
            }
        }

        foreach (var (nombreCiudad, comunas) in ciudades)
        {
            var ciudadAliases = BuildNameAliases(nombreCiudad);
            var ciudad = await dbContext.Ciudades.FirstOrDefaultAsync(
                item => ciudadAliases.Contains(item.Nombre) && item.Pais.CodigoIso2 == "CL",
                cancellationToken);

            if (ciudad is null)
            {
                ciudad = new Ciudad
                {
                    Pais = chile,
                    Nombre = nombreCiudad,
                    Activo = true
                };

                dbContext.Ciudades.Add(ciudad);
            }
            else
            {
                ciudad.Nombre = nombreCiudad;
                ciudad.Activo = true;
            }

            foreach (var nombreComuna in comunas)
            {
                var comunaAliases = BuildNameAliases(nombreComuna);
                var comuna = await dbContext.Comunas.FirstOrDefaultAsync(
                    item =>
                        comunaAliases.Contains(item.Nombre) &&
                        ciudadAliases.Contains(item.Ciudad.Nombre) &&
                        item.Ciudad.Pais.CodigoIso2 == "CL",
                    cancellationToken);

                if (comuna is null)
                {
                    dbContext.Comunas.Add(new Comuna
                    {
                        Ciudad = ciudad,
                        Nombre = nombreComuna,
                        Activo = true
                    });
                    continue;
                }

                comuna.Nombre = nombreComuna;
                comuna.Activo = true;
            }
        }
    }

    private static async Task SeedSuperAdminAsync(UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        var email = configuration["Seed:SuperAdmin:Email"];
        var password = configuration["Seed:SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"No se pudo crear el usuario SuperAdmin inicial: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(user, "SuperAdmin");
        }
    }

    private static string[] BuildNameAliases(string name)
    {
        return new[]
            {
                name,
                RemoveDiacritics(name),
                Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(name))
            }
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
