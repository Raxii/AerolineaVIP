
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        private bool PuedeVerTodasLasReservas()
        {
            return User.IsInRole("SuperUsuario") ||
                   User.IsInRole("Administrador") ||
                   User.IsInRole("Counter");
        }

        private bool PuedeAdministrarReservas()
        {
            return PuedeVerTodasLasReservas();
        }

        // GET: Reservas
        // El cliente solamente consulta sus propias reservas.
        public ActionResult Index()
        {
            var consulta = db.Reserva
                .Include(r => r.EstadoReserva)
                .Include(r => r.ReservaPasajeroVuelo.Select(a => a.Pasajero));

            if (!PuedeVerTodasLasReservas())
            {
                string usuarioId = User.Identity.GetUserId();

                consulta = consulta.Where(r => r.UsuarioId == usuarioId);
            }

            return View(consulta
                .OrderByDescending(r => r.FechaCreacion)
                .ToList());
        }

        // GET: Reservas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var reserva = db.Reserva
                .Include(r => r.EstadoReserva)
                .Include(r => r.ReservaPasajeroVuelo.Select(a => a.Pasajero))
                .FirstOrDefault(r => r.ReservaId == id.Value);

            if (reserva == null)
            {
                return HttpNotFound();
            }

            // Evitar que un cliente consulte reservas ajenas.
            if (!PuedeVerTodasLasReservas())
            {
                string usuarioId = User.Identity.GetUserId();

                if (reserva.UsuarioId != usuarioId)
                {
                    return HttpNotFound();
                }
            }

            return View(reserva);
        }

        // GET: Reservas/Create
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult Create()
        {
            ViewBag.EstadoReservaCodigo = new SelectList(
                db.EstadoReserva,
                "Codigo",
                "Descripcion"
            );

            return View();
        }

        // POST: Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult Create(
            [Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")]
            Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Reserva.Add(reserva);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.EstadoReservaCodigo = new SelectList(
                db.EstadoReserva,
                "Codigo",
                "Descripcion",
                reserva.EstadoReservaCodigo
            );

            return View(reserva);
        }

        // GET: Reservas/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Reserva reserva = db.Reserva.Find(id);

            if (reserva == null)
            {
                return HttpNotFound();
            }

            ViewBag.EstadoReservaCodigo = new SelectList(
                db.EstadoReserva,
                "Codigo",
                "Descripcion",
                reserva.EstadoReservaCodigo
            );

            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult Edit(
            [Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")]
            Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Entry(reserva).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.EstadoReservaCodigo = new SelectList(
                db.EstadoReserva,
                "Codigo",
                "Descripcion",
                reserva.EstadoReservaCodigo
            );

            return View(reserva);
        }

        // GET: Reservas/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Reserva reserva = db.Reserva.Find(id);

            if (reserva == null)
            {
                return HttpNotFound();
            }

            return View(reserva);
        }

        // POST: Reservas/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
        public ActionResult DeleteConfirmed(int id)
        {
            Reserva reserva = db.Reserva.Find(id);

            if (reserva == null)
            {
                return HttpNotFound();
            }

            db.Reserva.Remove(reserva);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
