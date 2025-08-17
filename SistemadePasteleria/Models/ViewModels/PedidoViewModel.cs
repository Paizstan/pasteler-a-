// ViewModels/PedidoCreateVM.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemadePasteleria.ViewModels
{
    public class DetalleCreateVM
    {
        [Required] public int ProductoId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Mínimo 1")] public int Cantidad { get; set; } = 1;

        // Estos dos se rellenan en el front solo para vista; en el backend se recalculan.
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PedidoCreateVM
    {
        [Required] public int ClienteId { get; set; }
        [Required] public int UsuarioId { get; set; }

        [Range(0, double.MaxValue)] public decimal? Abono { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaEstimada { get; set; } = DateTime.Today.AddDays(1);

        [MaxLength(50)]
        public string? Estado { get; set; } = "Pendiente";

        // Se calcula en backend
        public decimal Total { get; set; }

        public List<DetalleCreateVM> Detalles { get; set; } = new() { new DetalleCreateVM() };
    }
}
