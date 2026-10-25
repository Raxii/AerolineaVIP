using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    public class CheckInsController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: CheckIns
        public ActionResult Index()
        {
            var checkIn = db.CheckIn.Include(c => c.ReservaPasajeroVuelo).Include(c => c.EstadoCheckIn);
            return View(checkIn.ToList());
        }

        // GET: CheckIns/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIn checkIn = db.CheckIn.Find(id);
            if (checkIn == null)
            {
                return HttpNotFound();
            }
            return View(checkIn);
        }

        // GET: CheckIns/Create
        public ActionResult Create()
        {
            ViewBag.AsignacionId = new SelectList(db.ReservaPasajeroVuelo, "AsignacionId", "EstadoAsignacionCodigo");
            ViewBag.EstadoCheckInCodigo = new SelectList(db.EstadoCheckIn, "Codigo", "Descripcion");
            return View();
        }

        // POST: CheckIns/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CheckInId,AsignacionId,FechaRegistro,EstadoCheckInCodigo")] CheckIn checkIn)
        {
            if (ModelState.IsValid)
            {
                db.CheckIn.Add(checkIn);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AsignacionId = new SelectList(db.ReservaPasajeroVuelo, "AsignacionId", "EstadoAsignacionCodigo", checkIn.AsignacionId);
            ViewBag.EstadoCheckInCodigo = new SelectList(db.EstadoCheckIn, "Codigo", "Descripcion", checkIn.EstadoCheckInCodigo);
            return View(checkIn);
        }

        // GET: CheckIns/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIn checkIn = db.CheckIn.Find(id);
            if (checkIn == null)
            {
                return HttpNotFound();
            }
            ViewBag.AsignacionId = new SelectList(db.ReservaPasajeroVuelo, "AsignacionId", "EstadoAsignacionCodigo", checkIn.AsignacionId);
            ViewBag.EstadoCheckInCodigo = new SelectList(db.EstadoCheckIn, "Codigo", "Descripcion", checkIn.EstadoCheckInCodigo);
            return View(checkIn);
        }

        // POST: CheckIns/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CheckInId,AsignacionId,FechaRegistro,EstadoCheckInCodigo")] CheckIn checkIn)
        {
            if (ModelState.IsValid)
            {
                db.Entry(checkIn).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AsignacionId = new SelectList(db.ReservaPasajeroVuelo, "AsignacionId", "EstadoAsignacionCodigo", checkIn.AsignacionId);
            ViewBag.EstadoCheckInCodigo = new SelectList(db.EstadoCheckIn, "Codigo", "Descripcion", checkIn.EstadoCheckInCodigo);
            return View(checkIn);
        }

        // GET: CheckIns/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CheckIn checkIn = db.CheckIn.Find(id);
            if (checkIn == null)
            {
                return HttpNotFound();
            }
            return View(checkIn);
        }

        // POST: CheckIns/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CheckIn checkIn = db.CheckIn.Find(id);
            db.CheckIn.Remove(checkIn);
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
