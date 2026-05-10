# TuCita

TuCita es una API web para una plataforma SaaS multi-negocio de reservas y citas. Permite que distintos negocios configuren sus servicios, prestadores, horarios, reglas de reserva, clientes, pagos, reseñas, reportes y notificaciones desde una misma base de datos, manteniendo la información separada por negocio.

El sistema está pensado para rubros como barberías, veterinarias, talleres mecánicos, centros de estética, profesionales independientes, restaurantes, canchas deportivas y centros de salud.

## Estado Del Proyecto

Proyecto backend desarrollado con ASP.NET Core, Entity Framework Core, SQL Server e Identity. Incluye autenticación JWT, refresh tokens, autorización por roles, módulos operativos de agenda, pagos, notificaciones, auditoría, reseñas y reportes.

La solución usa una arquitectura por capas:

```text
TuCita.Api
  Controllers, autenticación HTTP, Swagger, CORS, background worker y almacenamiento local.

TuCita.Application
  DTOs, requests, queries, contratos de servicios y resultados de aplicación.

TuCita.Infrastucture
  EF Core, Identity, servicios de negocio, integraciones, seed, email, Flow y persistencia.

TuCita.Domain
  Capa reservada para reglas de dominio compartidas.
```

## Tecnologías

- ASP.NET Core `net10.0`
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication
- Swagger / OpenAPI
- BackgroundService para tareas en segundo plano
- SMTP para correos
- Flow sandbox para pagos online
- Generación de reportes Excel
- Generación de comprobantes PDF

## Módulos Principales

- Autenticación, registro, login y refresh token.
- Registro diferenciado para clientes y dueños de negocio.
- Perfil de usuario con datos normalizados de contacto y ubicación.
- Administración global para SuperAdmin.
- Mantención de negocios, rubros y catálogos.
- Usuarios por negocio con roles internos.
- Invitaciones seguras por email con token hasheado, expiración y uso único.
- Servicios, categorías, prestadores y servicios por prestador.
- Horarios de negocio, horarios de prestador y bloqueos.
- Reglas de reserva por negocio.
- Disponibilidad de agenda con validación de horarios, bloqueos y citas existentes.
- Reserva pública por slug de negocio.
- Agenda del usuario logueado.
- Gestión de citas con historial.
- Formularios dinámicos por tipo de servicio.
- Clientes del negocio.
- Notificaciones por plantillas y envío SMTP.
- Pagos online con Flow y pagos manuales.
- Anulaciones, devoluciones e historial de pagos.
- Comprobantes de pago en PDF.
- Dashboard operativo.
- Reportes profesionales exportables.
- Auditoría general.
- Reseñas post-atención, configuración de reputación y reseñas públicas.
- Centro de acciones operativas.

## Roles

TuCita combina roles globales de Identity con roles internos por negocio.

Roles principales:

- `SuperAdmin`: administra la plataforma completa.
- `Owner`: dueño de un negocio.
- `Admin`: administrador operativo del negocio.
- `Recepcionista`: gestiona agenda, clientes y citas.
- `Profesional`: atiende citas y consulta su agenda.
- `Cliente`: reserva y gestiona sus propias citas.

## Flujo General

```text
1. Un usuario registra un negocio.
2. El sistema lo asocia automáticamente como Owner.
3. El negocio configura servicios, prestadores, horarios y reglas.
4. El cliente explora negocios públicos y reserva por slug.
5. La API valida disponibilidad y crea la cita.
6. Se generan historial, notificaciones y pagos si corresponde.
7. El negocio gestiona la agenda, pagos, reportes y reseñas.
```

## Requisitos

- .NET SDK compatible con `net10.0`
- SQL Server o SQL Server Express
- Visual Studio 2026 o superior recomendado
- Cuenta SMTP si se desea enviar correos reales
- Credenciales Flow sandbox si se desea probar pagos online

## Configuración

Los archivos `appsettings.json` y `appsettings.Development.json` contienen valores locales o placeholders. No se deben subir credenciales reales al repositorio.

Configuraciones importantes:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=TuCita;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "clave-segura",
    "Issuer": "TuCitaAPI",
    "Audience": "TuCitaAPI.Client"
  },
  "Email": {
    "Enabled": false,
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true
  },
  "Flow": {
    "Enabled": false,
    "ApiBaseUrl": "https://sandbox.flow.cl/api"
  }
}
```

## Base De Datos

El proyecto usa SQL Server y EF Core. El `DbContext` principal es:

```text
TuCita.Infrastucture.Persistence.ReservaFlowDbContext
```

### Crear Migración

Desde la Consola del Administrador de paquetes de Visual Studio:

```powershell
Add-Migration NombreMigracion -Project TuCita.Infrastucture -StartupProject TuCita.Api -Context ReservaFlowDbContext
```

### Aplicar Migraciones

Desde la Consola del Administrador de paquetes de Visual Studio:

```powershell
Update-Database -Project TuCita.Infrastucture -StartupProject TuCita.Api -Context ReservaFlowDbContext
```

## Ejecutar El Proyecto

Restaurar y compilar:

```powershell
dotnet restore
dotnet build TuCita.slnx
```

Ejecutar API:

```powershell
dotnet run --project TuCita.Api --launch-profile https
```

Swagger queda disponible en:

```text
https://localhost:7219/swagger
```

## Endpoints Por Área

La API está organizada en controladores semánticos:

- `AuthController`: login, registro y refresh token.
- `OnboardingController`: registro inicial de dueños de negocio.
- `NegociosController`: mantenedor interno de negocios.
- `NegociosPublicosController`: exploración pública de negocios.
- `ReservasPublicasController`: reserva pública por slug.
- `MiAgendaController`: agenda del usuario logueado.
- `MisCitasController`: citas del usuario.
- `CitasController`: gestión interna de citas.
- `ClientesController`: clientes del negocio.
- `ServiciosController`: servicios del negocio.
- `PrestadoresController`: profesionales y recursos.
- `HorariosNegocioController`: horarios generales.
- `HorariosPrestadorController`: horarios por prestador.
- `BloqueosHorarioController`: bloqueos de agenda.
- `ReglasReservaController`: reglas del negocio.
- `CamposReservaController`: formularios dinámicos.
- `PagosController`, `PagosFlowController`, `PagosManualesController`: pagos y comprobantes.
- `NotificacionesController`: procesamiento de notificaciones.
- `InvitacionesController`, `MisInvitacionesController`, `InvitacionesNegocioController`: invitaciones.
- `ResenasController`, `ResenasNegocioController`, `ResenasPublicasController`: reseñas y reputación.
- `DashboardNegocioController`: métricas del negocio.
- `ReportesNegocioController`: reportes descargables.
- `AuditoriaController`, `AuditoriaGlobalController`: auditoría.
- `CentroOperativoController`: acciones que requieren atención.
- Catálogos: rubros, roles de negocio, tipos de campo, estados, canales y tipos de notificación.

## Pagos

El sistema soporta:

- Pagos online con Flow.
- Pagos manuales, como efectivo, transferencia, débito, crédito u otros métodos configurados.
- Estados de pago normalizados.
- Historial de cambios de pago.
- Anulaciones y devoluciones con trazabilidad.
- Comprobantes PDF profesionales.

Para Flow, se recomienda usar el ambiente sandbox durante desarrollo:

```json
{
  "Flow": {
    "Enabled": true,
    "ApiBaseUrl": "https://sandbox.flow.cl/api",
    "ApiKey": "",
    "SecretKey": ""
  }
}
```

## Notificaciones

Las notificaciones se almacenan en base de datos y pueden procesarse manualmente o mediante el worker en segundo plano.

Casos cubiertos:

- Confirmación de reserva.
- Recordatorios.
- Cancelación.
- Reagendamiento.
- Post-atención.
- Invitaciones de negocio.
- Alertas por reseñas bajas.

Las plantillas de correo se renderizan en HTML y usan la configuración SMTP definida para el entorno.

## Background Jobs

El worker procesa tareas operativas:

- Enviar notificaciones pendientes.
- Sincronizar pagos Flow pendientes.
- Expirar pagos no completados.
- Expirar invitaciones.
- Expirar solicitudes de reseña.

Configuración:

```json
{
  "BackgroundJobs": {
    "Enabled": true,
    "RunOnStartup": true,
    "IntervalSeconds": 60
  }
}
```

## Seguridad

Buenas prácticas aplicadas:

- Identity para usuarios, roles, password hash y seguridad base.
- JWT con expiración.
- Refresh tokens almacenados como hash.
- Invitaciones con token real solo por correo y hash en base de datos.
- Validación de acceso por negocio.
- Roles globales y roles por negocio.
- Auditoría de cambios relevantes.
- Archivos subidos guardados en `wwwroot/uploads`, ignorados por Git.
- Credenciales reales fuera del repositorio.

## Reportes Y Dashboard

El backend incluye endpoints para construir paneles y reportes:

- Citas por estado.
- Ingresos por período.
- Servicios más reservados.
- Clientes frecuentes.
- Tasa de inasistencia.
- Rendimiento por prestador.
- Métricas de pagos.
- Métricas de reputación.
- Reseñas recientes y distribución por estrellas.
- Acciones operativas pendientes.

## Reseñas Y Reputación

Las reseñas se asocian a citas atendidas. El negocio puede configurar:

- Si las reseñas están activas.
- Si se autoaprueban.
- Días máximos para calificar.
- Si permite respuesta del negocio.
- Si se muestran públicamente.
- Umbral de alerta operativa por baja puntuación.

La página pública puede consultar promedio, total de reseñas y distribución de estrellas para mostrar reputación al cliente.

## Estructura Resumida

```text
TuCita
├── TuCita.Api
│   ├── Controllers
│   ├── Authorization
│   ├── BackgroundJobs
│   ├── Requests
│   └── Storage
├── TuCita.Application
│   ├── DTOs
│   ├── Interfaces
│   ├── Queries
│   └── Requests
├── TuCita.Infrastucture
│   ├── Entities
│   ├── Persistence
│   ├── Services
│   ├── Email
│   ├── Authentication
│   └── Migrations
└── TuCita.Domain
```
