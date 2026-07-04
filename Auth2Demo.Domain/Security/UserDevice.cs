using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class UserDevice : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nome amigável do dispositivo (ex.: "Meu Notebook", "iPhone 16").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único do dispositivo gerado pela aplicação.
    /// </summary>
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Navegador utilizado (Chrome, Edge, Firefox...).
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// Sistema operacional (Windows 11, Android, iOS...).
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Endereço IP do último acesso.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent completo enviado pelo navegador.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Indica se o dispositivo foi marcado como confiável.
    /// </summary>
    public bool IsTrusted { get; set; }

    /// <summary>
    /// Data do primeiro acesso por este dispositivo.
    /// </summary>
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Data do último acesso por este dispositivo.
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indica se o dispositivo está ativo.
    /// </summary>
    public bool IsActive { get; set; } = true;
}