using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize]
    public class ComprasController : Controller
    {
        private readonly AerolineaVIP_Context db = new AerolineaVIP_Context();

        private bool PuedeConsultarTodas()
        {
            return User.IsInRole("SuperUsuario")
                || User.IsInRole("Administrador")
                || User.IsInRole("Counter");
        }

        private IQueryable<Compra> ConsultaCompras()
        {
            // IMPORTANTE: IQueryable permite reasignar el resultado de Where().
            IQueryable<Compra> consulta = db.Compra
                .Include(c => c.EstadoCompra)
                .Include(c => c.Moneda)
                .Include(c => c.Reserva)
                .Include(c => c.Reserva.EstadoReserva)
                .Include(c => c.Reserva.ReservaPasajeroVuelo.Select(a => a.Pasajero));

            if (!PuedeConsultarTodas())
            {
                string usuarioId = User.Identity.GetUserId();

                var reservasUsuario = db.Reserva
                    .Where(r => r.UsuarioId == usuarioId)
                    .Select(r => r.ReservaId);

                consulta = consulta.Where(c =>
                    reservasUsuario.Contains(c.ReservaId));
            }

            return consulta;
        }

        private Reserva ObtenerReservaVisible(int reservaId)
        {
            IQueryable<Reserva> consulta = db.Reserva;

            if (!PuedeConsultarTodas())
            {
                string usuarioId = User.Identity.GetUserId();
                consulta = consulta.Where(r => r.UsuarioId == usuarioId);
            }

            return consulta.FirstOrDefault(r => r.ReservaId == reservaId);
        }

        // GET: Compras
        public ActionResult Index()
        {
            var compras = ConsultaCompras()
                .OrderByDescending(c => c.FechaCompra)
                .ToList();

            foreach (var compra in compras)
            {
                if (compra.Reserva != null)
                    ProcesarExpiracion(compra.Reserva);
            }

            compras = ConsultaCompras()
                .OrderByDescending(c => c.FechaCompra)
                .ToList();

            return View(compras);
        }

        // GET: Compras/MisCompras
        public ActionResult MisCompras()
        {
            return RedirectToAction("Index");
        }

        // GET: Compras/Details/5
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var compra = ConsultaCompras()
                .FirstOrDefault(c => c.CompraId == id.Value);

            if (compra == null)
                return HttpNotFound();

            if (compra.Reserva != null)
                ProcesarExpiracion(compra.Reserva);

            compra = ConsultaCompras()
                .FirstOrDefault(c => c.CompraId == id.Value);

            if (compra == null)
                return HttpNotFound();

            return View(compra);
        }

        // GET: Compras/Create?reservaId=1
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        public ActionResult Create(int? reservaId)
        {
            if (reservaId.HasValue)
            {
                var reserva = ObtenerReservaVisible(reservaId.Value);

                if (reserva == null)
                    return HttpNotFound();

                ProcesarExpiracion(reserva);
                reserva = db.Reserva.Find(reservaId.Value);

                if (reserva == null ||
                    reserva.EstadoReservaCodigo != "PENDIENTE")
                {
                    TempData["Error"] = "La reserva no está disponible para comprar.";
                    return RedirectToAction("Index", "Reservas");
                }

                var compraExistente = db.Compra.FirstOrDefault(c =>
                    c.ReservaId == reserva.ReservaId &&
                    (c.EstadoCompraCodigo == "PENDIENTE" ||
                     c.EstadoCompraCodigo == "PAGADA"));

                if (compraExistente != null)
                    return RedirectToAction("Details", new { id = compraExistente.CompraId });

                decimal importe = CalcularImporteReserva(reserva.ReservaId);

                var modelo = new Compra
                {
                    ReservaId = reserva.ReservaId,
                    FechaCompra = DateTime.Now,
                    Importe = importe,
                    MonedaCodigo = "USD",
                    EstadoCompraCodigo = "PENDIENTE",
                    ClaveIdempotencia = Guid.NewGuid()
                };

                PrepararListas(reserva.ReservaId);
                return View(modelo);
            }

            PrepararListas(null);

            return View(new Compra
            {
                FechaCompra = DateTime.Now,
                MonedaCodigo = "USD",
                EstadoCompraCodigo = "PENDIENTE",
                ClaveIdempotencia = Guid.NewGuid()
            });
        }

        // POST: Compras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "ReservaId")] Compra compra)
        {
            if (compra == null || compra.ReservaId <= 0)
            {
                ModelState.AddModelError("", "Debe seleccionar una reserva válida.");
                PrepararListas(null);
                return View(compra);
            }

            int reservaId = compra.ReservaId;
            var reserva = ObtenerReservaVisible(reservaId);

            if (reserva == null)
                return HttpNotFound();

            ProcesarExpiracion(reserva);
            reserva = db.Reserva.Find(reservaId);

            if (reserva == null ||
                reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                TempData["Error"] = "La reserva ya no está pendiente y no puede comprarse.";
                return RedirectToAction("Details", "Reservas", new { id = reservaId });
            }

            var compraExistente = db.Compra.FirstOrDefault(c =>
                c.ReservaId == reservaId &&
                (c.EstadoCompraCodigo == "PENDIENTE" ||
                 c.EstadoCompraCodigo == "PAGADA"));

            if (compraExistente != null)
                return RedirectToAction("Details", new { id = compraExistente.CompraId });

            // El importe siempre se calcula en el servidor.
            decimal importe = CalcularImporteReserva(reservaId);

            if (importe <= 0m)
            {
                ModelState.AddModelError("", "La reserva no tiene asignaciones pendientes con importes válidos.");
                PrepararListas(reservaId);
                return View(compra);
            }

            var nuevaCompra = new Compra
            {
                ReservaId = reservaId,
                FechaCompra = DateTime.Now,
                Importe = importe,
                MonedaCodigo = "USD",
                EstadoCompraCodigo = "PENDIENTE",
                ClaveIdempotencia = Guid.NewGuid()
            };

            try
            {
                using (var transaccion = db.Database.BeginTransaction())
                {
                    db.Compra.Add(nuevaCompra);
                    db.SaveChanges();
                    transaccion.Commit();
                }

                TempData["Mensaje"] = "La compra pendiente fue registrada correctamente.";
                return RedirectToAction("Details", new { id = nuevaCompra.CompraId });
            }
            catch (DbUpdateException)
            {
                db.Entry(nuevaCompra).State = EntityState.Detached;

                var existente = db.Compra.FirstOrDefault(c =>
                    c.ReservaId == reservaId &&
                    (c.EstadoCompraCodigo == "PENDIENTE" ||
                     c.EstadoCompraCodigo == "PAGADA"));

                if (existente != null)
                    return RedirectToAction("Details", new { id = existente.CompraId });

                ModelState.AddModelError("", "No fue posible registrar la compra. Verifique la reserva e inténtelo nuevamente.");
                PrepararListas(reservaId);
                return View(compra);
            }
        }

        private decimal CalcularImporteReserva(int reservaId)
        {
            // ImporteTramo existe en el modelo Database First del proyecto.
            decimal? resultado = db.Database.SqlQuery<decimal?>(
                @"SELECT SUM(CAST(ImporteTramo AS DECIMAL(12,2)))
                  FROM dbo.ReservaPasajeroVuelo
                  WHERE ReservaId = @p0
                    AND EstadoAsignacionCodigo = 'PENDIENTE'",
                reservaId
            ).SingleOrDefault();

            return resultado ?? 0m;
        }

        private void PrepararListas(int? reservaSeleccionada)
        {
            var reservas = ObtenerReservasDisponibles();

            if (reservaSeleccionada.HasValue &&
                !reservas.Any(r => r.ReservaId == reservaSeleccionada.Value))
            {
                var reserva = ObtenerReservaVisible(reservaSeleccionada.Value);

                if (reserva != null &&
                    reserva.EstadoReservaCodigo == "PENDIENTE")
                {
                    reservas.Add(reserva);
                }
            }

            ViewBag.ReservaId = new SelectList(
                reservas, "ReservaId", "CodigoReserva", reservaSeleccionada);

            ViewBag.EstadoCompraCodigo = new SelectList(
                db.EstadoCompra.ToList(), "Codigo", "Descripcion", "PENDIENTE");

            ViewBag.MonedaCodigo = new SelectList(
                db.Moneda.ToList(), "Codigo", "Descripcion", "USD");
        }

        private List<Reserva> ObtenerReservasDisponibles()
        {
            IQueryable<Reserva> consulta = db.Reserva
                .Where(r => r.EstadoReservaCodigo == "PENDIENTE");

            if (!PuedeConsultarTodas())
            {
                string usuarioId = User.Identity.GetUserId();
                consulta = consulta.Where(r => r.UsuarioId == usuarioId);
            }

            var comprasActivas = db.Compra
                .Where(c =>
                    c.EstadoCompraCodigo == "PENDIENTE" ||
                    c.EstadoCompraCodigo == "PAGADA")
                .Select(c => c.ReservaId);

            return consulta
                .Where(r => !comprasActivas.Contains(r.ReservaId))
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();
        }

        private void ProcesarExpiracion(Reserva reserva)
        {
            if (reserva == null ||
                reserva.EstadoReservaCodigo != "PENDIENTE" ||
                !reserva.FechaExpiracion.HasValue ||
                reserva.FechaExpiracion.Value > DateTime.Now)
            {
                return;
            }

            using (var transaccion = db.Database.BeginTransaction())
            {
                try
                {
                    bool yaPagada = db.Compra.Any(c =>
                        c.ReservaId == reserva.ReservaId &&
                        c.EstadoCompraCodigo == "PAGADA");

                    var asignaciones = db.ReservaPasajeroVuelo
                        .Where(a =>
                            a.ReservaId == reserva.ReservaId &&
                            a.EstadoAsignacionCodigo == "PENDIENTE")
                        .ToList();

                    if (yaPagada)
                    {
                        reserva.EstadoReservaCodigo = "CONFIRMADA";

                        foreach (var asignacion in asignaciones)
                            asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
                    }
                    else
                    {
                        reserva.EstadoReservaCodigo = "EXPIRADA";

                        foreach (var asignacion in asignaciones)
                            asignacion.EstadoAsignacionCodigo = "LIBERADA";

                        var comprasPendientes = db.Compra
                            .Where(c =>
                                c.ReservaId == reserva.ReservaId &&
                                c.EstadoCompraCodigo == "PENDIENTE")
                            .ToList();

                        foreach (var item in comprasPendientes)
                            item.EstadoCompraCodigo = "ANULADA";
                    }

                    db.SaveChanges();
                    transaccion.Commit();
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        // GET: Compras/Delete/5
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var compra = ConsultaCompras()
                .FirstOrDefault(c => c.CompraId == id.Value);

            if (compra == null)
                return HttpNotFound();

            if (compra.EstadoCompraCodigo != "PENDIENTE")
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "Solamente se pueden anular compras pendientes.");

            return View(compra);
        }

        // POST: Compras/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente,SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            var compra = ConsultaCompras()
                .FirstOrDefault(c => c.CompraId == id);

            if (compra == null)
                return HttpNotFound();

            if (compra.EstadoCompraCodigo != "PENDIENTE")
            {
                TempData["Error"] = "Esta compra no puede anularse desde esta opción.";
                return RedirectToAction("Details", new { id = id });
            }

            bool pagoAprobado = db.Pago.Any(p =>
                p.CompraId == id &&
                p.EstadoPagoCodigo == "APROBADO");

            if (pagoAprobado)
            {
                TempData["Error"] = "La compra tiene un pago aprobado y no puede anularse desde esta opción.";
                return RedirectToAction("Details", new { id = id });
            }

            try
            {
                compra.EstadoCompraCodigo = "ANULADA";
                db.SaveChanges();
                TempData["Mensaje"] = "La compra pendiente fue anulada correctamente.";
            }
            catch
            {
                TempData["Error"] = "No fue posible anular la compra.";
            }

            return RedirectToAction("Details", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
