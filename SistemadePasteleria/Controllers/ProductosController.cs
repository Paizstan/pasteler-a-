using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemadePasteleria.Models;
using SistemadePasteleria.Utilidades;

namespace SistemadePasteleria.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProductosController : Controller
    {
        private readonly PasteldbContext _context;

        public ProductosController(PasteldbContext context)
        {
            _context = context;
        }

        // GET: Productos
        public async Task<IActionResult> Index(string? buscar, int pagina = 1)
        {
            var query = _context.Productos
        .Include(p => p.Categoria)
        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                // Búsqueda por nombre y categoría (LIKE)
                var term = $"%{buscar.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Nombre, term) ||
                    (p.Categoria != null && EF.Functions.Like(p.Categoria.Nombre, term))
                );
            }

            // Orden para resultados consistentes
            query = query.OrderBy(p => p.Nombre);

            // Paginación
            int totalRegistros = await query.CountAsync();
            var paginacion = new Paginacion(totalRegistros, pagina, 5, "Productos", "Index");

            var resultado = await query
                .Skip(paginacion.Salto)
                .Take(paginacion.RegistrosPagina)
                .ToListAsync();

            // Para que el input conserve el valor y vista reciba la info
            ViewData["buscar"] = buscar;
            ViewBag.Paginacion = paginacion;
            ViewBag.Busqueda = buscar;

            return View(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Buscar(string? term)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var like = $"%{term.Trim()}%";
                query = query.Where(p =>
                    EF.Functions.Like(p.Nombre, like) ||
                    (p.Categoria != null && EF.Functions.Like(p.Categoria.Nombre, like))
                );
            }

            var resultados = await query
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            // Devuelve solo las filas <tr> (partial)
            return PartialView("_ProductosRows", resultados);
        }



        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nombre");
            return View();
        }

        // POST: Productos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile imagenInput)
        {
            if (imagenInput != null && imagenInput.Length > 0)
            {
                // Asegura que la carpeta exista
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);

                // Nombre único para evitar conflictos
                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenInput.FileName);
                var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

                // Guardar el archivo en wwwroot/imagenes
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagenInput.CopyToAsync(stream);
                }

                // Guardar la ruta relativa en la base de datos
                producto.ImagenUrl = "/imagenes/" + nombreArchivo;
            }

            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }


        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            // Muestra nombres en el combo
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto formModel, IFormFile? imagenInput)
        {
            if (id != formModel.Id) return NotFound();

            var productoDb = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (productoDb == null) return NotFound();

            // 1) Actualiza solo los campos permitidos (lista blanca)
            //    Evita sobreposting (no tocaremos ImagenUrl aquí)
            if (!await TryUpdateModelAsync(productoDb, prefix: "",
                p => p.Nombre, p => p.Precio, p => p.Stock, p => p.CategoriaId))
            {
                ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nombre", productoDb.CategoriaId);
                return View(productoDb);
            }

            // 2) Imagen: conserva si no suben nueva; si suben, guarda y borra la anterior
            if (imagenInput != null && imagenInput.Length > 0)
            {
                var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");
                if (!Directory.Exists(rutaCarpeta))
                    Directory.CreateDirectory(rutaCarpeta);

                var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagenInput.FileName);
                var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    await imagenInput.CopyToAsync(stream);

                // Guarda nueva ruta
                var imagenAnterior = productoDb.ImagenUrl;
                productoDb.ImagenUrl = "/imagenes/" + nombreArchivo;

                await _context.SaveChangesAsync(); // primero DB

                // luego borra archivo anterior si existía
                EliminarArchivoFisico(imagenAnterior);
                return RedirectToAction(nameof(Index));
            }

            // No hay nueva imagen -> conserva la actual tal cual
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto != null)
            {
                var rutaImagen = producto.ImagenUrl;
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync(); // primero DB

                // luego intenta borrar archivo físico (si hay)
                EliminarArchivoFisico(rutaImagen);
            }

            return RedirectToAction(nameof(Index));
        }

        // -------- Helper privado --------
        private void EliminarArchivoFisico(string? imagenUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagenUrl)) return;

                // imagenUrl viene como "/imagenes/xxx.ext"
                var relative = imagenUrl.TrimStart('/');
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // Opcional: loguear. No interrumpir UX si falla el borrado físico.
            }
        }


        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}
