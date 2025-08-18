// ViewModels/PedidoCreateVM.cs
using SistemadePasteleria.Models;
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

    public class DetalleEditVM
    {
        public int Id { get; set; }   // Necesario para identificar el detalle ya existente
        [Required] public int ProductoId { get; set; }
        [Range(1, int.MaxValue)] public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }


        // <-- Agregar esta propiedad para mostrar en la vista
        public string ProductoNombre { get; set; } = string.Empty;
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
    public class PedidoEditVM
    {
        public int Id { get; set; }

        [Required] public int ClienteId { get; set; }
        [Required] public int UsuarioId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Abono { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Fecha { get; set; }

        [MaxLength(50)]
        public string? Estado { get; set; }

        public decimal Total { get; set; }

        public List<DetalleEditVM> Detalles { get; set; } = new();

        //  Propiedades para dropdowns
        public List<Cliente> Clientes { get; set; } = new();
        public List<Usuario> Usuarios { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<string> Estados { get; set; } = new();
    }
}
