using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Class_Messages")]
[Index("ClassId", "SentAt", Name = "IX_Class_Messages_Class_SentAt")]
public partial class ClassMessage
{
    [Key]
    [Column("message_id")]
    public int MessageId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("sender_id")]
    public int SenderId { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("sent_at")]
    [Precision(0)]
    public DateTime SentAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("ClassMessages")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("ClassMessages")]
    public virtual User Sender { get; set; } = null!;
}
