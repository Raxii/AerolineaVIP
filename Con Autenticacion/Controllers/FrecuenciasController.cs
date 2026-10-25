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
    public class FrecuenciasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Frecuencias
        public ActionResult Index()
        {
            var frecuencia = db.Frecuencia.Include(f => f.Ruta);
            return View(frecuencia.ToList());
        }

        // GET: Frecuencias/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // GET: Frecuencias/Create
        public ActionResult Create()
        {
            ViewBag.RutaId = new SelectList(db.Ruta, "RutaId", "RutaId");
            return View();
        }

        // POST: Frecuencias/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (ModelState.IsValid)
            {
                db.Frecuencia.Add(frecuencia);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RutaId = new SelectList(db.Ruta, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencias/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            ViewBag.RutaId = new SelectList(db.Ruta, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // POST: Frecuencias/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FrecuenciaId,RutaId,DiaSemana,HoraSalida,HoraLlegada,Activa")] Frecuencia frecuencia)
        {
            if (ModelState.IsValid)
            {
                db.Entry(frecuencia).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RutaId = new SelectList(db.Ruta, "RutaId", "RutaId", frecuencia.RutaId);
            return View(frecuencia);
        }

        // GET: Frecuencias/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            if (frecuencia == null)
            {
                return HttpNotFound();
            }
            return View(frecuencia);
        }

        // POST: Frecuencias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Frecuencia frecuencia = db.Frecuencia.Find(id);
            db.Frecuencia.Remove(frecuencia);
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
