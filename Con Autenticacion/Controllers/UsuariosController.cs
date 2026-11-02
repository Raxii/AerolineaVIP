using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    // Solo el SuperUsuario puede administrar cuentas y consultar su historial.
    [Authorize(Roles = "SuperUsuario")]
    public class UsuariosController : Controller
    {
        // GET: Usuarios
        public ActionResult Index(string buscar, string rol)
        {
            var listaUsuarios = new List<UsuarioRolViewModel>();
            var reservas = new List<Reserva>();

            int totalUsuarios = 0;
            int reservasActivas = 0;
            int reservasHoy = 0;
            int reservasCanceladas = 0;

            using (var dbReservas = new AerolineaVIP_Context())
            {
                foreach (var reserva in dbReservas.Reserva)
                {
                    reservas.Add(reserva);

                    string estado = reserva.EstadoReservaCodigo ?? "";
                    if (Contiene(estado, "CANCEL"))
                    {
                        reservasCanceladas++;
                    }
                    else if (!Contiene(estado, "EXPIR"))
                    {
                        reservasActivas++;
                    }

                    if (reserva.FechaCreacion.Date == DateTime.Today)
                    {
                        reservasHoy++;
                    }
                }
            }

            using (var db = new ApplicationDbContext())
            {
                var store = new UserStore<ApplicationUser>(db)
                {
                    DisposeContext = false
                };

                using (var manager = new UserManager<ApplicationUser>(store))
                {
                    string usuarioActual = User.Identity.GetUserId();
                    var usuariosRegistrados = new List<ApplicationUser>();
                    foreach (var usuarioItem in manager.Users) { usuariosRegistrados.Add(usuarioItem); }

                    foreach (var usuario in usuariosRegistrados)
                    {
                        totalUsuarios++;

                        var rolesActuales = manager.GetRoles(usuario.Id);
                        string rolesTexto = "";
                        string rolPrincipal = "Sin rol";
                        bool tieneSuperUsuario = false;
                        bool coincideRol = string.IsNullOrWhiteSpace(rol);

                        foreach (string rolUsuario in rolesActuales)
                        {
                            if (rolesTexto.Length > 0)
                            {
                                rolesTexto += ", ";
                            }
                            rolesTexto += rolUsuario;

                            if (rolPrincipal == "Sin rol")
                            {
                                rolPrincipal = rolUsuario;
                            }

                            if (rolUsuario == "SuperUsuario")
                            {
                                tieneSuperUsuario = true;
                            }

                            if (string.Equals(rolUsuario, rol, StringComparison.OrdinalIgnoreCase))
                            {
                                coincideRol = true;
                            }
                        }

                        bool coincideBusqueda = true;
                        if (!string.IsNullOrWhiteSpace(buscar))
                        {
                            string texto = (usuario.UserName ?? "") + " " + (usuario.Email ?? "");
                            coincideBusqueda = Contiene(texto, buscar);
                        }

                        if (!coincideBusqueda || !coincideRol)
                        {
                            continue;
                        }

                        Reserva ultimaReserva = null;
                        int cantidadReservas = 0;

                        foreach (var reserva in reservas)
                        {
                            if (reserva.UsuarioId == usuario.Id)
                            {
                                cantidadReservas++;
                                if (ultimaReserva == null || reserva.FechaCreacion > ultimaReserva.FechaCreacion)
                                {
                                    ultimaReserva = reserva;
                                }
                            }
                        }

                        var fila = new UsuarioRolViewModel();
                        fila.Id = usuario.Id;
                        fila.Usuario = usuario.UserName;
                        fila.Correo = usuario.Email;
                        fila.Roles = rolesTexto;
                        fila.RolSeleccionado = rolPrincipal == "Sin rol" ? "Cliente" : rolPrincipal;
                        fila.PuedeCambiarRol = !tieneSuperUsuario && usuario.Id != usuarioActual;
                        fila.CantidadReservas = cantidadReservas;

                        if (ultimaReserva != null)
                        {
                            fila.ReservaCodigo = ultimaReserva.CodigoReserva;
                            fila.FechaReserva = ultimaReserva.FechaCreacion;
                            fila.EstadoReserva = ultimaReserva.EstadoReservaCodigo;
                        }
                        else
                        {
                            fila.ReservaCodigo = "Sin reservas";
                            fila.FechaReserva = null;
                            fila.EstadoReserva = "Sin actividad";
                        }

                        listaUsuarios.Add(fila);
                    }
                }
            }

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.ReservasActivas = reservasActivas;
            ViewBag.ReservasHoy = reservasHoy;
            ViewBag.ReservasCanceladas = reservasCanceladas;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.RolSeleccionado = rol ?? "";

            return View(listaUsuarios);
        }

        // GET: Usuarios/Historial/id
        public ActionResult Historial(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HttpStatusCodeResult(400, "Usuario no valido.");
            }

            ApplicationUser usuario;
            using (var dbIdentity = new ApplicationDbContext())
            {
                var store = new UserStore<ApplicationUser>(dbIdentity)
                {
                    DisposeContext = false
                };
                using (var manager = new UserManager<ApplicationUser>(store))
                {
                    usuario = manager.FindById(id);
                }
            }

            if (usuario == null)
            {
                return HttpNotFound();
            }

            var historial = new List<Reserva>();
            using (var db = new AerolineaVIP_Context())
            {
                var todasReservas = new List<Reserva>();
                var estadosReserva = new List<EstadoReserva>();
                foreach (var item in db.Reserva) { todasReservas.Add(item); }
                foreach (var item in db.EstadoReserva) { estadosReserva.Add(item); }
                foreach (var reserva in todasReservas)
                {
                    if (reserva.UsuarioId == id)
                    {
                        foreach (var estadoReserva in estadosReserva)
                        {
                            if (estadoReserva.Codigo == reserva.EstadoReservaCodigo)
                            {
                                reserva.EstadoReserva = estadoReserva;
                                break;
                            }
                        }
                        historial.Add(reserva);
                    }
                }
            }

            ViewBag.Usuario = usuario.UserName;
            ViewBag.Correo = usuario.Email;
            ViewBag.TotalReservas = historial.Count;
            return View(historial);
        }

        // POST: Usuarios/CambiarRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(string id, string rol)
        {
            if (string.IsNullOrWhiteSpace(id) || !RolPermitido(rol))
            {
                TempData["Error"] = "Seleccione un usuario y un rol valido.";
                return RedirectToAction("Index");
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

                    var rolesActuales = manager.GetRoles(id);
                    bool tieneSuperUsuario = false;
                    bool yaTieneRol = false;

                    foreach (string rolActual in rolesActuales)
                    {
                        if (rolActual == "SuperUsuario")
                        {
                            tieneSuperUsuario = true;
                        }
                        if (rolActual == rol)
                        {
                            yaTieneRol = true;
                        }
                    }

                    if (tieneSuperUsuario)
                    {
                        TempData["Error"] = "La cuenta SuperUsuario esta protegida.";
                        return RedirectToAction("Index");
                    }

                    if (yaTieneRol && rolesActuales.Count == 1)
                    {
                        TempData["Ok"] = "El usuario ya tiene ese rol.";
                        return RedirectToAction("Index");
                    }

                    // Se actualiza con Identity sin abrir una transaccion manual.
                    if (!yaTieneRol)
                    {
                        var agregado = manager.AddToRole(id, rol);
                        if (!agregado.Succeeded)
                        {
                            TempData["Error"] = ObtenerErrores(agregado.Errors);
                            return RedirectToAction("Index");
                        }
                    }

                    foreach (string rolAnterior in rolesActuales)
                    {
                        if (rolAnterior != rol && rolAnterior != "SuperUsuario")
                        {
                            var quitado = manager.RemoveFromRole(id, rolAnterior);
                            if (!quitado.Succeeded)
                            {
                                TempData["Error"] = ObtenerErrores(quitado.Errors);
                                return RedirectToAction("Index");
                            }
                        }
                    }

                    var sello = manager.UpdateSecurityStamp(id);
                    if (!sello.Succeeded)
                    {
                        TempData["Error"] = ObtenerErrores(sello.Errors);
                        return RedirectToAction("Index");
                    }
                }
            }

            TempData["Ok"] = "Rol actualizado. El usuario debe iniciar sesion nuevamente.";
            return RedirectToAction("Index");
        }

        private bool RolPermitido(string rol)
        {
            return rol == "Cliente" || rol == "Administrador" || rol == "Counter";
        }

        private bool Contiene(string texto, string buscar)
        {
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(buscar))
            {
                return false;
            }
            return texto.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string ObtenerErrores(IEnumerable<string> errores)
        {
            string mensaje = "No se pudo completar la operacion.";
            string detalle = "";
            foreach (string error in errores)
            {
                if (detalle.Length > 0)
                {
                    detalle += " ";
                }
                detalle += error;
            }
            if (detalle.Length > 0)
            {
                mensaje = detalle;
            }
            return mensaje;
        }
    }

    // Datos para mostrar usuarios y su ultima reserva.
    public class UsuarioRolViewModel
    {
        public string Id { get; set; }
        public string Usuario { get; set; }
        public string Correo { get; set; }
        public string Roles { get; set; }
        public string RolSeleccionado { get; set; }
        public bool PuedeCambiarRol { get; set; }
        public string ReservaCodigo { get; set; }
        public DateTime? FechaReserva { get; set; }
        public string EstadoReserva { get; set; }
        public int CantidadReservas { get; set; }
    }
}
