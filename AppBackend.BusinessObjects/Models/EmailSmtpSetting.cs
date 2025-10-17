using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Models;

public partial class EmailSmtpSetting
{
    public int SettingId { get; set; }

    public string Host { get; set; } = null!;

    public int Port { get; set; }

    public string? Username { get; set; }

    public string? PasswordPlain { get; set; }

    public bool? UseSsl { get; set; }

    public bool? UseStarttls { get; set; }

    public string? FromName { get; set; }

    public string? FromAddress { get; set; }

    public bool IsActive { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
