using System.Globalization;
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
                .Include(p => p.DetallePedidos).ThenInclude(d => d.Producto)
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (fecha.HasValue)
                query = query.Where(p => p.Fecha == DateOnly.FromDateTime(fecha.Value));
            else if (mes.HasValue)
                query = query.Where(p => p.Fecha.Month == mes.Value && p.Fecha.Year == DateTime.Today.Year);

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                query = query.Where(p => p.Estado == estado);

            var pedidos = await query.ToListAsync();

            var pdfBytes = GeneratePDF(pedidos, fecha, mes, estado);
            return File(pdfBytes, "application/pdf", "VolumenVentas_VidaMia.pdf");
        }

        // ========= DISEÑO COMPATIBLE (sin BorderRadius ni APIs nuevas) =========
        private byte[] GeneratePDF(List<Pedido> pedidos, DateTime? fecha, int? mes, string estado)
        {
            var culture = new CultureInfo("es-SV");

            // Paleta
            var brandBg = "#FFF1F4";
            var brandCard = "#FFE6EB";
            var brandChip = "#FFD6DE";
            var head = "#7B3F00";
            var accent = "#8C4A33";
            var text = "#5B3A29";
            var zebra = "#FFF8FA";
            var white = "#FFFFFF";
            var grayText = "#7A7A7A";

            // KPIs
            var totalPedidos = pedidos.Count;
            var totalVentas = pedidos.Sum(p => p.Total);
            var ticketProm = totalPedidos > 0 ? totalVentas / totalPedidos : 0m;

            // Filtros
            var filtrosList = new List<string>();
            if (fecha.HasValue) filtrosList.Add($"Día: {fecha.Value:dd/MM/yyyy}");
            if (mes.HasValue) filtrosList.Add($"Mes: {mes.Value:00}/{DateTime.Today.Year}");
            if (!string.IsNullOrWhiteSpace(estado)) filtrosList.Add($"Estado: {estado}");
            var filtros = string.Join("   ", filtrosList);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.Background().Background(brandBg);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(text));

                    // Header
                    page.Header().PaddingBottom(6).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(t =>
                            {
                                t.Span("🍰  Reporte de Volumen de Ventas – ").FontSize(20).FontColor(accent);
                                t.Span("Vida Mia").FontSize(20).Bold().FontColor(head);
                            });

                            if (!string.IsNullOrEmpty(filtros))
                            {
                                col.Item().PaddingTop(4).Row(r =>
                                {
                                    r.AutoItem()
                                        .PaddingVertical(3).PaddingHorizontal(8)
                                        .Background(brandChip)
                                        .Border(1).BorderColor(accent)
                                        .Text(filtros).FontSize(9).FontColor(accent);
                                });
                            }
                        });

                        row.AutoItem().AlignRight().Text("🧁").FontSize(24);
                    });

                    // Contenido
                    page.Content().Column(col =>
                    {
                        // KPIs (tarjetas simples sin esquinas redondeadas)
                        col.Item().Row(row =>
                        {
                            void Kpi(string titulo, string valor)
                            {
                                row.RelativeItem().PaddingRight(6).Element(card =>
                                {
                                    card.Background(brandCard)
                                        .Border(1).BorderColor(accent)
                                        .Padding(10)
                                        .Column(cc =>
                                        {
                                            cc.Item().Text(titulo).FontSize(10).FontColor(accent);
                                            cc.Item().Text(valor).FontSize(16).Bold().FontColor(head);
                                        });
                                });
                            }

                            Kpi("Total de pedidos", totalPedidos.ToString("N0", culture));
                            Kpi("Total vendido", string.Format(culture, "{0:C}", totalVentas));
                            Kpi("Ticket promedio", string.Format(culture, "{0:C}", ticketProm));
                        });

                        col.Item().PaddingTop(8);

                        if (pedidos.Count == 0)
                        {
                            col.Item()
                                .Background(white)
                                .Border(1).BorderColor(brandChip)
                                .Padding(12)
                                .AlignCenter()
                                .Text("No se encontraron pedidos con esos filtros.")
                                .FontSize(12).FontColor(accent);
                        }
                        else
                        {
                            // Tabla
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1); // #
                                    columns.RelativeColumn(2); // Cliente
                                    columns.RelativeColumn(2); // Usuario
                                    columns.RelativeColumn(1); // Fecha
                                    columns.RelativeColumn(1); // Estado
                                    columns.RelativeColumn(1); // Total
                                });

                                // Encabezado
                                table.Header(header =>
                                {
                                    header.Cell().Background(head).Padding(8).Text("Pedido #").Bold().FontColor(white);
                                    header.Cell().Background(head).Padding(8).Text("Cliente").Bold().FontColor(white);
                                    header.Cell().Background(head).Padding(8).Text("Usuario").Bold().FontColor(white);
                                    header.Cell().Background(head).Padding(8).Text("Fecha").Bold().FontColor(white);
                                    header.Cell().Background(head).Padding(8).Text("Estado").Bold().FontColor(white);
                                    header.Cell().Background(head).Padding(8).AlignRight().Text("Total").Bold().FontColor(white);
                                });

                                bool alt = false;
                                foreach (var p in pedidos)
                                {
                                    var bg = alt ? white : zebra;
                                    alt = !alt;

                                    table.Cell().Background(bg).Padding(6).Text("#" + p.Id.ToString());
                                    table.Cell().Background(bg).Padding(6).Text(p.Cliente != null ? p.Cliente.Nombre : "N/A");
                                    table.Cell().Background(bg).Padding(6).Text(p.Usuario != null ? p.Usuario.Nombre : "N/A");
                                    table.Cell().Background(bg).Padding(6).Text(p.Fecha.ToString("dd/MM/yyyy", culture));
                                    table.Cell().Background(bg).Padding(6).Text(p.Estado ?? "");
                                    table.Cell().Background(bg).Padding(6).AlignRight()
                                         .Text(string.Format(culture, "{0:C}", p.Total));
                                }

                                // Totales
                                table.Cell().ColumnSpan(5).PaddingTop(6).PaddingRight(8).AlignRight()
                                     .Text("TOTAL").Bold().FontColor(accent);
                                table.Cell().PaddingTop(6).AlignRight()
                                     .Text(string.Format(culture, "{0:C}", totalVentas))
                                     .Bold().FontColor(accent);
                            });
                        }
                    });

                    // Footer
                    page.Footer().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text(t =>
                        {
                            t.Span("Generado el ");
                            t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        });

                        row.AutoItem().AlignRight().Text(t =>
                        {
                            t.Span("Página ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}