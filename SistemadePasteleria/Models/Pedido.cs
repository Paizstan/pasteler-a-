using System;
using System.Collections.Generic;

namespace SistemadePasteleria.Models;

public partial class Pedido
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }


    public decimal Total { get; set; }

    public int ClienteId { get; set; }

    public int UsuarioId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual Usuario Usuario { get; set; } = null!;
}