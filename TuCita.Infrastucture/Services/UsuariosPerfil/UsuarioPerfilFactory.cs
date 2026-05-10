using TuCita.Infrastucture.Entities;

namespace TuCita.Infrastucture.UsuariosPerfil;

internal static class UsuarioPerfilFactory
{
    public static UsuarioPerfil Create(
        string userId,
        string? nombre,
        string? apellido,
        string? rut,
        DateTime? fechaNacimiento,
        string? avatarUrl,
        string? telefonoAlternativo,
        string? direccion,
        int? idComuna,
        bool aceptaTerminos,
        bool aceptaMarketing)
    {
        var now = DateTime.UtcNow;
        var profile = new UsuarioPerfil
        {
            UserId = userId,
            FechaCreacion = now,
            Activo = true,
            Contacto = new UsuarioContacto { FechaCreacion = now },
            Direccion = new UsuarioDireccion { FechaCreacion = now },
            Consentimiento = new UsuarioConsentimiento { FechaCreacion = now },
            Seguridad = new UsuarioSeguridad { FechaCreacion = now }
        };

        ApplyEditable(
            profile,
            nombre,
            apellido,
            rut,
            fechaNacimiento,
            avatarUrl,
            telefonoAlternativo,
            direccion,
            idComuna,
            updateAudit: false);
        ApplyConsent(profile, aceptaTerminos, aceptaMarketing, updateAudit: false);

        return profile;
    }

    public static void ApplyEditable(
        UsuarioPerfil profile,
        string? nombre,
        string? apellido,
        string? rut,
        DateTime? fechaNacimiento,
        string? avatarUrl,
        string? telefonoAlternativo,
        string? direccion,
        int? idComuna,
        bool updateAudit = true)
    {
        profile.Nombre = TrimToNull(nombre);
        profile.Apellido = TrimToNull(apellido);
        profile.NombreCompleto = BuildNombreCompleto(profile.Nombre, profile.Apellido);
        profile.Rut = TrimToNull(rut);
        profile.FechaNacimiento = fechaNacimiento;
        profile.AvatarUrl = TrimToNull(avatarUrl);

        profile.Contacto ??= new UsuarioContacto { FechaCreacion = DateTime.UtcNow };
        profile.Contacto.TelefonoAlternativo = TrimToNull(telefonoAlternativo);

        profile.Direccion ??= new UsuarioDireccion { FechaCreacion = DateTime.UtcNow };
        profile.Direccion.Direccion = TrimToNull(direccion);
        profile.Direccion.IdComuna = idComuna;

        if (updateAudit)
        {
            var now = DateTime.UtcNow;
            profile.FechaActualizacion = now;
            profile.Contacto.FechaActualizacion = now;
            profile.Direccion.FechaActualizacion = now;
        }
    }

    public static void ApplyConsent(
        UsuarioPerfil profile,
        bool aceptaTerminos,
        bool aceptaMarketing,
        bool updateAudit = true)
    {
        profile.Consentimiento ??= new UsuarioConsentimiento { FechaCreacion = DateTime.UtcNow };

        if (!profile.Consentimiento.AceptaTerminos && aceptaTerminos)
        {
            profile.Consentimiento.FechaAceptacionTerminos = DateTime.UtcNow;
        }

        if (!aceptaTerminos)
        {
            profile.Consentimiento.FechaAceptacionTerminos = null;
        }

        profile.Consentimiento.AceptaTerminos = aceptaTerminos;
        profile.Consentimiento.AceptaMarketing = aceptaMarketing;

        if (updateAudit)
        {
            profile.Consentimiento.FechaActualizacion = DateTime.UtcNow;
        }
    }

    public static UsuarioSeguridad EnsureSecurity(UsuarioPerfil profile)
    {
        profile.Seguridad ??= new UsuarioSeguridad { FechaCreacion = DateTime.UtcNow };
        return profile.Seguridad;
    }

    private static string? BuildNombreCompleto(string? nombre, string? apellido)
    {
        var value = string.Join(
            ' ',
            new[] { nombre, apellido }.Where(item => !string.IsNullOrWhiteSpace(item)));

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
