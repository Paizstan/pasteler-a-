using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;

namespace SistemadePasteleria.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportesController : Controller
    {
        private readonly PasteldbContext _context;

        public ReportesController(PasteldbContext context)
        {
            _context = context;
        }

        // GET: Reportes/VolumenVentas
        public async Task<IActionResult> VolumenVentas(string filtro = "dia")
        {
            var query = _context.Pedidos.AsQueryable();

            var datos = await query
                .GroupBy(p => filtro == "mes"
                    ? new DateTime(p.Fecha.Year, p.Fecha.Month, 1)
                    : p.Fecha.Date)
                .Select(g => new VolumenVentasModel
                {
                    Fecha = g.Key,
                    TotalPedidos = g.Count(),
                    TotalVentas = g.Sum(p => p.Total)
                })
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            ViewBag.Filtro = filtro;
            return View(datos);
        }
    }
}
