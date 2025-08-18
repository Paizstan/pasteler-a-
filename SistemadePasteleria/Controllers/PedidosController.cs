using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;
using SistemadePasteleria.Utilidades;
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
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            var query = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .AsQueryable();

            // Ordenar para consistencia (puede ser por fecha descendente o Id)
            query = query.OrderByDescending(p => p.Fecha);

            // 📄 Paginación
            int totalRegistros = await query.CountAsync();
            var paginacion = new Paginacion(totalRegistros, pagina, 5, "Pedidos", "Index");

            var resultado = await query
                .Skip(paginacion.Salto)
                .Take(paginacion.RegistrosPagina)
                .ToListAsync();

            // Para que la vista pueda usar búsqueda y paginación
            ViewData["buscar"] = buscar;
            ViewBag.Paginacion = paginacion;
            ViewBag.Busqueda = buscar;

            return View(resultado);
        }

        public async Task<IActionResult> Buscar(string term)
        {
            var query = _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.DetallePedidos)
                .ThenInclude(dp => dp.Producto)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var likeTerm = $"%{term.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Cliente.Nombre, likeTerm) ||
                    EF.Functions.Like(p.Usuario.Nombre, likeTerm) ||
                    EF.Functions.Like(p.Estado, likeTerm)
                );
            }

            var pedidos = await query
                .OrderBy(p => p.Fecha)
                .ToListAsync();

            return PartialView("_PedidosRows", pedidos);
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

            ViewBag.Estados = new List<string> { "Pendiente", "En proceso", "Finalizado", "Anulado" };

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
                ViewBag.Estados = new List<string> { "Pendiente", "En proceso", "Finalizado", "Anulado" };
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

            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            // Mapear Pedido -> PedidoEditVM
            var vm = new PedidoEditVM
            {
                Id = pedido.Id,
                ClienteId = pedido.ClienteId,
                UsuarioId = pedido.UsuarioId,
                Fecha = pedido.Fecha.ToDateTime(new TimeOnly(0, 0)),
                Estado = pedido.Estado,
                Total = pedido.Total,
                Abono = pedido.Abono,
                Detalles = pedido.DetallePedidos.Select(d => new DetalleEditVM
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal,
                    ProductoNombre = d.Producto.Nombre
                }).ToList(),

                // Listas para dropdowns
                Clientes = await _context.Clientes.ToListAsync(),
                Usuarios = await _context.Usuarios.ToListAsync(),
                Productos = await _context.Productos.ToListAsync(),
                Estados = new List<string> { "Pendiente", "En proceso", "Finalizado", "Anulado" }
            };

            return View(vm);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PedidoEditVM vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.Clientes = await _context.Clientes.ToListAsync();
                vm.Usuarios = await _context.Usuarios.ToListAsync();
                vm.Estados = new List<string> { "Pendiente", "En proceso", "Finalizado", "Anulado" };
                return View(vm);
            }

            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            // Actualizar campos del pedido
            pedido.ClienteId = vm.ClienteId;
            pedido.UsuarioId = vm.UsuarioId;
            pedido.Estado = vm.Estado ?? pedido.Estado;
            pedido.Fecha = vm.Fecha.HasValue ? DateOnly.FromDateTime(vm.Fecha.Value) : pedido.Fecha;
            pedido.Abono = vm.Abono ?? pedido.Abono;

            // Eliminar detalles que ya no están en el VM
            var detallesIdsVm = vm.Detalles.Select(d => d.Id).ToList();
            var detallesEliminar = pedido.DetallePedidos.Where(d => !detallesIdsVm.Contains(d.Id)).ToList();
            if (detallesEliminar.Any())
            {
                _context.DetallePedidos.RemoveRange(detallesEliminar);
            }

            // Actualizar o agregar detalles
            foreach (var detalleVm in vm.Detalles)
            {
                var detalle = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleVm.Id);
                if (detalle != null)
                {
                    detalle.ProductoId = detalleVm.ProductoId;
                    detalle.Cantidad = detalleVm.Cantidad;
                    detalle.PrecioUnitario = detalleVm.PrecioUnitario;
                    detalle.Subtotal = detalleVm.Cantidad * detalleVm.PrecioUnitario;
                }
                else
                {
                    // Nuevo detalle (agregado desde la vista)
                    var nuevoDetalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = detalleVm.ProductoId,
                        Cantidad = detalleVm.Cantidad,
                        PrecioUnitario = detalleVm.PrecioUnitario,
                        Subtotal = detalleVm.Cantidad * detalleVm.PrecioUnitario
                    };
                    _context.DetallePedidos.Add(nuevoDetalle);
                }
            }

            // Recalcular Total
            pedido.Total = pedido.DetallePedidos.Sum(d => d.Subtotal);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(pedido.Id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar
        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
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
            var pedido = await _context.Pedidos
                .Include(p => p.DetallePedidos) // Traemos los detalles
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido != null)
            {
                // 1. Eliminar los detalles asociados
                if (pedido.DetallePedidos != null && pedido.DetallePedidos.Any())
                {
                    _context.DetallePedidos.RemoveRange(pedido.DetallePedidos);
                }

                // 2. Eliminar el pedido
                _context.Pedidos.Remove(pedido);

                // 3. Guardar cambios en la BD
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
