using Microsoft.AspNetCore.Mvc;
using SistemadePasteleria.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

public class AccesoController : Controller
{
    private readonly PasteldbContext _context;

    public AccesoController(PasteldbContext context)
    {
        _context = context;
    }

    // GET: Login
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [HttpPost]
    public async Task<IActionResult> Login(string Nombre, string PasswordHash)
    {
        var usuario = _context.Usuarios.Include(u => u.Rol)
            .FirstOrDefault(u => u.Nombre == Nombre && u.PasswordHash == PasswordHash);

        if (usuario != null)
        {
            // Crear la identidad
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction("Index", "Home"); // ✔️ Redirige al Home
        }

        ViewBag.Error = "Usuario o contraseña inválidos.";
        return View("Index");
    }

    // GET: Registrar
    public IActionResult Registrar()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Registrar(string Nombre, string PasswordHash)
    {
        // Verifica si ya existe
        if (_context.Usuarios.Any(u => u.Nombre == Nombre))
        {
            ViewBag.Error = "Este nombre de usuario ya existe.";
            return View();
        }

        var nuevo = new Usuario
        {
            Nombre = Nombre,
            PasswordHash = PasswordHash,
            RolId = 1 // Suponiendo 1 = Empleado
        };

        _context.Usuarios.Add(nuevo);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Acceso");
    }
}
