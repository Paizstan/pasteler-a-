using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;
using SistemadePasteleria.Models.ViewModels;

namespace SistemadePasteleria.Controllers
{
    public class PedidoesController : Controller
    {
        private readonly PasteldbContext _context;

        public PedidoesController(PasteldbContext context)
        {
            _context = context;
        }

        // GET: Pedidoes
        public async Task<IActionResult> Index()
        {
            var pasteldbContext = _context.Pedidos.Include(p => p.Cliente).Include(p => p.Usuario);
            return View(await pasteldbContext.ToListAsync());
        }

        // GET: Pedidoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // ✅ GET: Pedidoes/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ClienteId"] = new SelectList(_context.Clientes, "Id", "Nombre");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Nombre");
            ViewData["Productos"] = new SelectList(_context.Productos, "Id", "Nombre");

            var viewModel = new PedidoViewModel();
            return View(viewModel);
        }

        // ✅ POST: Pedidoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ClienteId"] = new SelectList(_context.Clientes, "Id", "Nombre", model.ClienteId);
                ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Nombre", model.UsuarioId);
                ViewData["Productos"] = new SelectList(_context.Productos, "Id", "Nombre");
                return View(model);
            }

            try
            {
                var pedido = new Pedido
                {
                    ClienteId = model.ClienteId,
                    UsuarioId = model.UsuarioId,
                    Fecha = DateTime.Now,
                    Total = model.Productos.Sum(p => p.Cantidad * p.PrecioUnitario)
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                foreach (var item in model.Productos)
                {
                    var detalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Subtotal = item.Cantidad * item.PrecioUnitario
                    };

                    _context.DetallePedidos.Add(detalle);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                ViewData["ClienteId"] = new SelectList(_context.Clientes, "Id", "Nombre", model.ClienteId);
                ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Nombre", model.UsuarioId);
                ViewData["Productos"] = new SelectList(_context.Productos, "Id", "Nombre");
                return View(model);
            }
        }

        // GET: Pedidoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            ViewData["ClienteId"] = new SelectList(_context.Clientes, "Id", "Nombre", pedido.ClienteId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Nombre", pedido.UsuarioId);
            return View(pedido);
        }

        // POST: Pedidoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Fecha,Total,ClienteId,UsuarioId")] Pedido pedido)
        {
            if (id != pedido.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pedido);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id)) return NotFound();
                    else throw;
                }
            }

            ViewData["ClienteId"] = new SelectList(_context.Clientes, "Id", "Nombre", pedido.ClienteId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "Id", "Nombre", pedido.UsuarioId);
            return View(pedido);
        }

        // GET: Pedidoes/Delete/5
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

        // POST: Pedidoes/Delete/5
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
