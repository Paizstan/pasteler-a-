using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemadePasteleria.Models;

namespace SistemadePasteleria.Controllers
{
    public class ReportesController : Controller
    {
        private readonly PasteldbContext _context;

        public ReportesController(PasteldbContext context)
        {
            _context = context;
        }

        // GET: Reportes/VolumenVentas
        public IActionResult VolumenVentas()
        {
            ViewBag.Estados = new List<string> { "", "Pendiente", "En proceso", "Finalizado", "Anulado" };
            return View();
        }

        // POST: Reportes/VolumenVentasPDF
        [HttpPost]
        public async Task<IActionResult> VolumenVentasPDF(DateTime? fecha, int? mes, string estado)
        {
            var query = _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (fecha.HasValue)
            {
                query = query.Where(p => p.Fecha == DateOnly.FromDateTime(fecha.Value));
            }
            else if (mes.HasValue)
            {
                query = query.Where(p => p.Fecha.Month == mes.Value && p.Fecha.Year == DateTime.Today.Year);
            }

            if(!string.IsNullOrEmpty(estado) && estado != "Todos")
{
                query = query.Where(p => p.Estado == estado);
            }

            var pedidos = await query.ToListAsync();

            var pdfBytes = GeneratePDF(pedidos, fecha, mes, estado);

            return File(pdfBytes, "application/pdf", "VolumenVentas_VidaMia.pdf");
        }

        private byte[] GeneratePDF(List<Pedido> pedidos, DateTime? fecha, int? mes, string estado)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header()
                        .Text("Reporte de Volumen de Ventas - Vida Mia")
                        .Bold().FontSize(18).AlignCenter();

                    page.Content()
                        .Column(col =>
                        {
                            col.Item().Text($"Filtros aplicados: " +
                                $"{(fecha.HasValue ? $"Día: {fecha.Value:dd/MM/yyyy}" : "")}" +
                                $"{(mes.HasValue ? $"Mes: {mes.Value}" : "")}" +
                                $"{(!string.IsNullOrEmpty(estado) ? $"Estado: {estado}" : "")}")
                                .FontSize(12).Italic();

                            col.Spacing(10);

                            if (!pedidos.Any())
                            {
                                col.Item().Text("No se encontraron pedidos con esos filtros.");
                            }
                            else
                            {
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1); // ID
                                        columns.RelativeColumn(2); // Cliente
                                        columns.RelativeColumn(2); // Usuario
                                        columns.RelativeColumn(1); // Fecha
                                        columns.RelativeColumn(1); // Estado
                                        columns.RelativeColumn(1); // Total
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Pedido #").Bold();
                                        header.Cell().Text("Cliente").Bold();
                                        header.Cell().Text("Usuario").Bold();
                                        header.Cell().Text("Fecha").Bold();
                                        header.Cell().Text("Estado").Bold();
                                        header.Cell().Text("Total").Bold();
                                    });

                                    foreach (var pedido in pedidos)
                                    {
                                        table.Cell().Text($"#{pedido.Id}");
                                        table.Cell().Text(pedido.Cliente?.Nombre ?? "N/A");
                                        table.Cell().Text(pedido.Usuario?.Nombre ?? "N/A");
                                        table.Cell().Text(pedido.Fecha.ToString("dd/MM/yyyy"));
                                        table.Cell().Text(pedido.Estado);
                                        table.Cell().Text($"${pedido.Total:F2}");
                                    }
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Vida Mia - {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf();
        }
    }
}
