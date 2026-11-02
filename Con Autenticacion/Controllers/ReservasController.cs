using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador,Counter")]
    public class ReservasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Reservas
        public ActionResult Index()
        {
            var reservas = new List<Reserva>();
            var todasReservas = new List<Reserva>();
            var todasAsignaciones = new List<ReservaPasajeroVuelo>();
            foreach (var item in db.Reserva) { todasReservas.Add(item); }
            foreach (var item in db.ReservaPasajeroVuelo) { todasAsignaciones.Add(item); }

            foreach (var reserva in todasReservas)
            {
                reserva.EstadoReserva = db.EstadoReserva.Find(reserva.EstadoReservaCodigo);

                var asignaciones = new List<ReservaPasajeroVuelo>();
                foreach (var asignacion in todasAsignaciones)
                {
                    if (asignacion.ReservaId == reserva.ReservaId)
                    {
                        asignacion.Pasajero = db.Pasajero.Find(asignacion.PasajeroId);
                        asignaciones.Add(asignacion);
                    }
                }
                reserva.ReservaPasajeroVuelo = asignaciones;
                reservas.Add(reserva);
            }

            return View(reservas);
        }

        // GET: Reservas/Details/5
        public ActionResult Details(int? id)
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
            reserva.EstadoReserva = db.EstadoReserva.Find(reserva.EstadoReservaCodigo);
            var asignaciones = new List<ReservaPasajeroVuelo>();
            var todasAsignaciones = new List<ReservaPasajeroVuelo>();
            foreach (var item in db.ReservaPasajeroVuelo) { todasAsignaciones.Add(item); }
            foreach (var asignacion in todasAsignaciones)
            {
                if (asignacion.ReservaId == reserva.ReservaId)
                {
                    asignacion.Pasajero = db.Pasajero.Find(asignacion.PasajeroId);
                    asignaciones.Add(asignacion);
                }
            }
            reserva.ReservaPasajeroVuelo = asignaciones;
            return View(reserva);
        }

        // GET: Reservas/Create
        public ActionResult Create()
        {
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion");
            return View();
        }

        // POST: Reservas/Create
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")] Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Reserva.Add(reserva);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // GET: Reservas/Edit/5
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
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        // Para protegerse de ataques de publicacion excesiva, habilite las propiedades especificas a las que quiere enlazarse. Para obtener 
        // mas detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")] Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Entry(reserva).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // GET: Reservas/Delete/5
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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Reserva reserva = db.Reserva.Find(id);
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
