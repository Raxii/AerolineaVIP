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
    public class TramoItinerariosController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: TramoItinerarios
        public ActionResult Index()
        {
            var tramoItinerario = db.TramoItinerario.Include(t => t.Itinerario).Include(t => t.Vuelo);
            return View(tramoItinerario.ToList());
        }

        // GET: TramoItinerarios/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Create
        public ActionResult Create()
        {
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia");
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo");
            return View();
        }

        // POST: TramoItinerarios/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.TramoItinerario.Add(tramoItinerario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tramoItinerario).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(db.Vuelo, "VueloId", "EstadoVueloCodigo", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            db.TramoItinerario.Remove(tramoItinerario);
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
