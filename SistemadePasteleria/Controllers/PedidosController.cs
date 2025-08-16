using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;

namespace SistemadePasteleria.Controllers
{
    public class PedidosController : Controller
    {
        private readonly PasteldbContext _context;

        public PedidosController(PasteldbContext context)
        {
            _context = context;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index()
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.DetallePedidos)
                .ThenInclude(dp => dp.Producto);

            return View(await pedidos.ToListAsync());
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create()
        {
            ViewData["Clientes"] = _context.Clientes.ToList();
            ViewData["Usuarios"] = _context.Usuarios.ToList();

            // Lista de estados
            ViewData["Estados"] = new List<string> { "Pendiente", "En proceso", "Entregado" };

            return View();
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                // Si usas DateOnly lo convierto desde DateTime.Now
                pedido.Fecha = DateOnly.FromDateTime(DateTime.Now);

                _context.Add(pedido);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Clientes"] = _context.Clientes.ToList();
            ViewData["Usuarios"] = _context.Usuarios.ToList();
            ViewData["Estados"] = new List<string> { "Pendiente", "En proceso", "Entregado" };

            return View(pedido);
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            ViewData["Clientes"] = _context.Clientes.ToList();
            ViewData["Usuarios"] = _context.Usuarios.ToList();
            ViewData["Estados"] = new List<string> { "Pendiente", "En proceso", "Entregado" };

            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Pedido pedido)
        {
            if (id != pedido.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Clientes"] = _context.Clientes.ToList();
            ViewData["Usuarios"] = _context.Usuarios.ToList();
            ViewData["Estados"] = new List<string> { "Pendiente", "En proceso", "Entregado" };

            return View(pedido);
        }

        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }
    }
}
