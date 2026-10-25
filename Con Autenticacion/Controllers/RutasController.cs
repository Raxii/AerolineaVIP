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
    public class RutasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Rutas
        public ActionResult Index()
        {
            var ruta = db.Ruta.Include(r => r.Aeropuerto).Include(r => r.Aeropuerto1);
            return View(ruta.ToList());
        }

        // GET: Rutas/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // GET: Rutas/Create
        public ActionResult Create()
        {
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA");
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA");
            return View();
        }

        // POST: Rutas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Ruta.Add(ruta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // POST: Rutas/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ruta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "CodigoIATA", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Ruta.Find(id);
            db.Ruta.Remove(ruta);
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
