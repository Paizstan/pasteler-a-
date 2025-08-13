using Microsoft.AspNetCore.Mvc;
using SistemadePasteleria.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AccesoController : Controller
{
    private readonly PasteldbContext _context;

    public AccesoController(PasteldbContext context)
    {
        _context = context;
    }

    // Método para encriptar contraseñas con SHA256
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hashBytes = sha256.ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();
            foreach (var b in hashBytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    // GET: Login
    public IActionResult Index()
    {
        return View();
    }

    // POST: Login
    [HttpPost]
    public async Task<IActionResult> Login(string Nombre, string PasswordHash)
    {
        string passwordHasheada = HashPassword(PasswordHash);

        var usuario = _context.Usuarios.Include(u => u.Rol)
            .FirstOrDefault(u => u.Nombre == Nombre && u.PasswordHash == passwordHasheada);

        if (usuario != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Usuario o contraseña inválidos.";
        return View("Index");
    }

    // GET: Registrar
    public IActionResult Registrar()
    {
        return View();
    }

    // POST: Registrar
    [HttpPost]
    public IActionResult Registrar(string Nombre, string PasswordHash)
    {
        if (_context.Usuarios.Any(u => u.Nombre == Nombre))
        {
            ViewBag.Error = "Este nombre de usuario ya existe.";
            return View();
        }

        var rolEmpleado = _context.Roles.FirstOrDefault(r => r.Nombre == "Administrador");
        if (rolEmpleado == null)
        {
            ViewBag.Error = "No se encontró el rol 'Empleado'.";
            return View();
        }

        var nuevo = new Usuario
        {
            Nombre = Nombre,
            PasswordHash = HashPassword(PasswordHash), // 🔹 Guardado encriptado
            RolId = rolEmpleado.Id
        };

        _context.Usuarios.Add(nuevo);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    // GET: Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Acceso");
    }
}
