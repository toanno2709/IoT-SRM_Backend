using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Email_SMTP_Settings")]
[Index("IsActive", Name = "UX_SMTP_ActiveSingleton", IsUnique = true)]
public partial class EmailSMTPSettings
{
    [Key]
    [Column("setting_id")]
    public int SettingId { get; set; }

    [Column("host")]
    [StringLength(255)]
    public string Host { get; set; } = null!;

    [Column("port")]
    public int Port { get; set; }

    [Column("username")]
    [StringLength(255)]
    public string? Username { get; set; }

    [Column("password_plain")]
    [StringLength(512)]
    public string? PasswordPlain { get; set; }

    [Column("use_ssl")]
    public bool? UseSsl { get; set; }

    [Column("use_starttls")]
    public bool? UseStarttls { get; set; }

    [Column("from_name")]
    [StringLength(255)]
    public string? FromName { get; set; }

    [Column("from_address")]
    [StringLength(255)]
    public string? FromAddress { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UpdatedBy")]
    [InverseProperty("EmailSMTPSettings")]
    public virtual User? UpdatedByNavigation { get; set; }
}


