using System;
using System.Collections.Generic;

namespace SistemadePasteleria.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public string? PasswordHash { get; set; }

    public int RolId { get; set; }

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual Role Rol { get; set; } = null!;
}
