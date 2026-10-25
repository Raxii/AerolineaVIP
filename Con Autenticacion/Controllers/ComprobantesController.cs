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
    public class ComprobantesController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Comprobantes
        public ActionResult Index()
        {
            var comprobante = db.Comprobante.Include(c => c.Compra).Include(c => c.EstadoComprobante).Include(c => c.TipoComprobante);
            return View(comprobante.ToList());
        }

        // GET: Comprobantes/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Comprobante comprobante = db.Comprobante.Find(id);
            if (comprobante == null)
            {
                return HttpNotFound();
            }
            return View(comprobante);
        }

        // GET: Comprobantes/Create
        public ActionResult Create()
        {
            ViewBag.CompraId = new SelectList(db.Compra, "CompraId", "MonedaCodigo");
            ViewBag.EstadoComprobanteCodigo = new SelectList(db.EstadoComprobante, "Codigo", "Descripcion");
            ViewBag.TipoComprobanteCodigo = new SelectList(db.TipoComprobante, "Codigo", "Descripcion");
            return View();
        }

        // POST: Comprobantes/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ComprobanteId,CompraId,TipoComprobanteCodigo,NumeroComprobante,FechaEmision,EstadoComprobanteCodigo")] Comprobante comprobante)
        {
            if (ModelState.IsValid)
            {
                db.Comprobante.Add(comprobante);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CompraId = new SelectList(db.Compra, "CompraId", "MonedaCodigo", comprobante.CompraId);
            ViewBag.EstadoComprobanteCodigo = new SelectList(db.EstadoComprobante, "Codigo", "Descripcion", comprobante.EstadoComprobanteCodigo);
            ViewBag.TipoComprobanteCodigo = new SelectList(db.TipoComprobante, "Codigo", "Descripcion", comprobante.TipoComprobanteCodigo);
            return View(comprobante);
        }

        // GET: Comprobantes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Comprobante comprobante = db.Comprobante.Find(id);
            if (comprobante == null)
            {
                return HttpNotFound();
            }
            ViewBag.CompraId = new SelectList(db.Compra, "CompraId", "MonedaCodigo", comprobante.CompraId);
            ViewBag.EstadoComprobanteCodigo = new SelectList(db.EstadoComprobante, "Codigo", "Descripcion", comprobante.EstadoComprobanteCodigo);
            ViewBag.TipoComprobanteCodigo = new SelectList(db.TipoComprobante, "Codigo", "Descripcion", comprobante.TipoComprobanteCodigo);
            return View(comprobante);
        }

        // POST: Comprobantes/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ComprobanteId,CompraId,TipoComprobanteCodigo,NumeroComprobante,FechaEmision,EstadoComprobanteCodigo")] Comprobante comprobante)
        {
            if (ModelState.IsValid)
            {
                db.Entry(comprobante).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CompraId = new SelectList(db.Compra, "CompraId", "MonedaCodigo", comprobante.CompraId);
            ViewBag.EstadoComprobanteCodigo = new SelectList(db.EstadoComprobante, "Codigo", "Descripcion", comprobante.EstadoComprobanteCodigo);
            ViewBag.TipoComprobanteCodigo = new SelectList(db.TipoComprobante, "Codigo", "Descripcion", comprobante.TipoComprobanteCodigo);
            return View(comprobante);
        }

        // GET: Comprobantes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Comprobante comprobante = db.Comprobante.Find(id);
            if (comprobante == null)
            {
                return HttpNotFound();
            }
            return View(comprobante);
        }

        // POST: Comprobantes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Comprobante comprobante = db.Comprobante.Find(id);
            db.Comprobante.Remove(comprobante);
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
