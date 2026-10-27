using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    // Solo el SuperUsuario puede ver usuarios y administrar sus roles.
    [Authorize(Roles = "SuperUsuario")]
    public class UsuariosController : Controller
    {
        // GET: /Usuarios/Index
        // PLANTILLA #20
        public ActionResult Index()
        {
            return View();
        }

        // POST: /Usuarios/CambiarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(string id, string rol)
        {
            string[] rolesPermitidos = { "Cliente", "Administrador", "Counter" };

            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(rol) ||
                !rolesPermitidos.Contains(rol))
            {
                return new HttpStatusCodeResult(400, "Rol o usuario no valido.");
            }

            if (id == User.Identity.GetUserId())
            {
                TempData["Error"] = "No puedes cambiar tu propio rol.";
                return RedirectToAction("Index");
            }

            using (var db = new ApplicationDbContext())
            {
                var store = new UserStore<ApplicationUser>(db)
                {
                    DisposeContext = false
                };

                using (var manager = new UserManager<ApplicationUser>(store))
                {
                    var usuario = manager.FindById(id);
                    if (usuario == null)
                    {
                        return HttpNotFound();
                    }

                    var rolesActuales = manager.GetRoles(id).ToList();

                    // La pantalla no permite crear ni modificar SuperUsuarios.
                    if (rolesActuales.Contains("SuperUsuario") ||
                        rolesActuales.Any(r => !rolesPermitidos.Contains(r)))
                    {
                        TempData["Error"] = "Esta cuenta tiene un rol protegido.";
                        return RedirectToAction("Index");
                    }

                    if (rolesActuales.Count == 1 && rolesActuales[0] == rol)
                    {
                        TempData["Ok"] = "El usuario ya tiene ese rol.";
                        return RedirectToAction("Index");
                    }

                    // Mantener consistente el cambio si hay que retirar un rol anterior.
                    using (var transaccion = db.Database.BeginTransaction())
                    {
                        if (!rolesActuales.Contains(rol))
                        {
                            var agregado = manager.AddToRole(id, rol);
                            if (!agregado.Succeeded)
                            {
                                TempData["Error"] = string.Join("; ", agregado.Errors);
                                return RedirectToAction("Index");
                            }
                        }

                        foreach (var anterior in rolesActuales.Where(r => r != rol))
                        {
                            var quitado = manager.RemoveFromRole(id, anterior);
                            if (!quitado.Succeeded)
                            {
                                TempData["Error"] = string.Join("; ", quitado.Errors);
                                return RedirectToAction("Index");
                            }
                        }

                        var sello = manager.UpdateSecurityStamp(id);
                        if (!sello.Succeeded)
                        {
                            TempData["Error"] = string.Join("; ", sello.Errors);
                            return RedirectToAction("Index");
                        }

                        transaccion.Commit();
                    }
                }
            }

            TempData["Ok"] = "Rol actualizado. El usuario debe iniciar sesion de nuevo.";
            return RedirectToAction("Index");
        }
    }

    // Modelo sencillo para mostrar la lista de usuarios.
    public class UsuarioRolViewModel
    {
        public string Id { get; set; }
        public string Usuario { get; set; }
        public string Correo { get; set; }
        public string Roles { get; set; }
        public string RolSeleccionado { get; set; }
        public bool PuedeCambiarRol { get; set; }
    }
}
