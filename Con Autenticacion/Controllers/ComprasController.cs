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
    public class ComprasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Compras
        public ActionResult Index()
        {
            var compra = db.Compra.Include(c => c.EstadoCompra).Include(c => c.Moneda).Include(c => c.Reserva);
            return View(compra.ToList());
        }

        // GET: Compras/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Compra compra = db.Compra.Find(id);
            if (compra == null)
            {
                return HttpNotFound();
            }
            return View(compra);
        }

        // GET: Compras/Create
        public ActionResult Create()
        {
            ViewBag.EstadoCompraCodigo = new SelectList(db.EstadoCompra, "Codigo", "Descripcion");
            ViewBag.MonedaCodigo = new SelectList(db.Moneda, "Codigo", "Descripcion");
            ViewBag.ReservaId = new SelectList(db.Reserva, "ReservaId", "CodigoReserva");
            return View();
        }

        // POST: Compras/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CompraId,ReservaId,FechaCompra,Importe,MonedaCodigo,EstadoCompraCodigo,ClaveIdempotencia")] Compra compra)
        {
            if (ModelState.IsValid)
            {
                db.Compra.Add(compra);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoCompraCodigo = new SelectList(db.EstadoCompra, "Codigo", "Descripcion", compra.EstadoCompraCodigo);
            ViewBag.MonedaCodigo = new SelectList(db.Moneda, "Codigo", "Descripcion", compra.MonedaCodigo);
            ViewBag.ReservaId = new SelectList(db.Reserva, "ReservaId", "CodigoReserva", compra.ReservaId);
            return View(compra);
        }

        // GET: Compras/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Compra compra = db.Compra.Find(id);
            if (compra == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoCompraCodigo = new SelectList(db.EstadoCompra, "Codigo", "Descripcion", compra.EstadoCompraCodigo);
            ViewBag.MonedaCodigo = new SelectList(db.Moneda, "Codigo", "Descripcion", compra.MonedaCodigo);
            ViewBag.ReservaId = new SelectList(db.Reserva, "ReservaId", "CodigoReserva", compra.ReservaId);
            return View(compra);
        }

        // POST: Compras/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CompraId,ReservaId,FechaCompra,Importe,MonedaCodigo,EstadoCompraCodigo,ClaveIdempotencia")] Compra compra)
        {
            if (ModelState.IsValid)
            {
                db.Entry(compra).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoCompraCodigo = new SelectList(db.EstadoCompra, "Codigo", "Descripcion", compra.EstadoCompraCodigo);
            ViewBag.MonedaCodigo = new SelectList(db.Moneda, "Codigo", "Descripcion", compra.MonedaCodigo);
            ViewBag.ReservaId = new SelectList(db.Reserva, "ReservaId", "CodigoReserva", compra.ReservaId);
            return View(compra);
        }

        // GET: Compras/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Compra compra = db.Compra.Find(id);
            if (compra == null)
            {
                return HttpNotFound();
            }
            return View(compra);
        }

        // POST: Compras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Compra compra = db.Compra.Find(id);
            db.Compra.Remove(compra);
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
