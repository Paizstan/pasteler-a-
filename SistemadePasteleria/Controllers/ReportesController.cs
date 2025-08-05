using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // 🔹 Acción para mostrar la vista con los datos (HTML)
        public async Task<IActionResult> VolumenVentas(string filtro = "dia")
        {
            if (string.IsNullOrEmpty(filtro))
                filtro = "dia";

            ViewBag.Filtro = filtro;

            var pedidos = await _context.Pedidos
                .Select(p => new
                {
                    p.Fecha,
                    p.Total
                })
                .ToListAsync();

            var datos = pedidos
                .GroupBy(p => filtro == "mes"
                    ? new DateTime(p.Fecha.Year, p.Fecha.Month, 1)
                    : p.Fecha.Date)
                .Select(g => new VolumenVentasModel
                {
                    Fecha = g.Key,
                    TotalPedidos = g.Count(),
                    TotalVentas = g.Sum(p => p.Total)
                })
                .OrderByDescending(x => x.Fecha)
                .ToList();

            return View(datos); // Esto carga VolumenVentas.cshtml
        }

        // 🔹 Acción para generar y descargar el PDF
        public async Task<IActionResult> GenerarVolumenVentasPdf(string filtro = "dia")
        {
            if (string.IsNullOrEmpty(filtro))
                filtro = "dia";

            var pedidos = await _context.Pedidos
                .Select(p => new
                {
                    p.Fecha,
                    p.Total
                })
                .ToListAsync();

            var datos = pedidos
                .GroupBy(p => filtro == "mes"
                    ? new DateTime(p.Fecha.Year, p.Fecha.Month, 1)
                    : p.Fecha.Date)
                .Select(g => new VolumenVentasModel
                {
                    Fecha = g.Key,
                    TotalPedidos = g.Count(),
                    TotalVentas = g.Sum(p => p.Total)
                })
                .OrderByDescending(x => x.Fecha)
                .ToList();

            var document = new VolumenVentasDocument(datos, filtro); // clase personalizada para PDF
            var pdfStream = document.GeneratePdf();

            return File(pdfStream, "application/pdf", $"volumen-ventas-{filtro}.pdf");
        }
    }
}
