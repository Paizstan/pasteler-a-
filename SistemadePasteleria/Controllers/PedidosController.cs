using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;
using SistemadePasteleria.ViewModels;

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
            ViewBag.Clientes = _context.Clientes
                .Select(c => new { c.Id, c.Nombre })
                .ToList();

            ViewBag.Usuarios = _context.Usuarios
                .Select(u => new { u.Id, u.Nombre })
                .ToList();

            // Necesitamos Productos para el detalle
            ViewBag.Productos = _context.Productos
                .Select(p => new { p.Id, p.Nombre, p.Precio })
                .ToList();

            ViewBag.Estados = new List<string> { "Pendiente", "En proceso", "Entregado" };

            return View(new PedidoCreateVM());
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidoCreateVM vm)
        {
            // Repoblar selects si algo falla
            void FillBags()
            {
                ViewBag.Clientes = _context.Clientes.Select(c => new { c.Id, c.Nombre }).ToList();
                ViewBag.Usuarios = _context.Usuarios.Select(u => new { u.Id, u.Nombre }).ToList();
                ViewBag.Productos = _context.Productos.Select(p => new { p.Id, p.Nombre, p.Precio }).ToList();
                ViewBag.Estados = new List<string> { "Pendiente", "En proceso", "Entregado" };
            }

            if (!ModelState.IsValid)
            {
                FillBags();
                return View(vm);
            }

            if (vm.Detalles == null || !vm.Detalles.Any(d => d.Cantidad > 0))
            {
                ModelState.AddModelError(string.Empty, "Agrega al menos un producto.");
                FillBags();
                return View(vm);
            }

            // Tomamos los ids de productos involucrados
            var ids = vm.Detalles.Where(d => d.Cantidad > 0).Select(d => d.ProductoId).Distinct().ToList();

            // Traemos precios de BD (nunca confiamos en el precio que venga del cliente)
            var precios = await _context.Productos
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Precio })
                .ToListAsync();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = new Pedido
                {
                    Fecha = DateOnly.FromDateTime(DateTime.Now),
                    ClienteId = vm.ClienteId,
                    UsuarioId = vm.UsuarioId,
                    Abono = vm.Abono,
                    FechaEstimada = vm.FechaEstimada,
                    Estado = string.IsNullOrWhiteSpace(vm.Estado) ? "Pendiente" : vm.Estado,
                    Total = 0m
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                decimal total = 0m;
                var detallesEntity = new List<DetallePedido>();

                foreach (var d in vm.Detalles.Where(x => x.Cantidad > 0))
                {
                    var pr = precios.First(x => x.Id == d.ProductoId).Precio;
                    var sub = pr * d.Cantidad;

                    detallesEntity.Add(new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = d.ProductoId,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = pr,
                        Subtotal = sub
                    });

                    total += sub;
                }

                _context.DetallePedidos.AddRange(detallesEntity);
                pedido.Total = total;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el pedido.");
                FillBags();
                return View(vm);
            }
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
