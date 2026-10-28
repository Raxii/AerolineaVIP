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
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class ItinerariosController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Itinerarios
        public ActionResult Index()
        {
            var itinerario = db.Itinerario.Include(i => i.EstadoItinerario);
            return View(itinerario.ToList());
        }

        // GET: Itinerarios/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // GET: Itinerarios/Create
        public ActionResult Create()
        {
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion");
            return View();
        }

        // POST: Itinerarios/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Itinerario.Add(itinerario);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // POST: Itinerarios/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(itinerario).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // POST: Itinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Itinerario itinerario = db.Itinerario.Find(id);
            db.Itinerario.Remove(itinerario);
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
