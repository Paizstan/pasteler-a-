using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemadePasteleria.Models.ViewModels
{
    public class PedidoViewModel
    {
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        // Aquí puedes agregar más validaciones si deseas
        public List<ProductoSeleccionado> Productos { get; set; } = new();

        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Total => Productos.Sum(p => p.Cantidad * p.PrecioUnitario);
    }

    public class ProductoSeleccionado
    {
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una cantidad válida")]
        public int Cantidad { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
