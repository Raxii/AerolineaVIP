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
    public class AsientoesController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Asientoes
        public ActionResult Index()
        {
            var asiento = db.Asiento.Include(a => a.ModeloAeronave);
            return View(asiento.ToList());
        }

        // GET: Asientoes/Details/5
        public ActionResult Details(int? modeloAeronaveId, int? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue || numeroAsiento.Value < 1 || numeroAsiento.Value > byte.MaxValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asiento.Find(modeloAeronaveId.Value, (byte)numeroAsiento.Value);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            asiento.ModeloAeronave = db.ModeloAeronave.Find(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asientoes/Create
        public ActionResult Create()
        {
            ViewBag.ModeloAeronaveId = CargarModelos(null);
            return View();
        }

        // POST: Asientoes/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento asiento)
        {
            if (ModelState.IsValid)
            {
                db.Asiento.Add(asiento);
                db.SaveChanges();
                TempData["Ok"] = "Asiento registrado correctamente.";
                return RedirectToAction("Index");
            }

            ViewBag.ModeloAeronaveId = CargarModelos(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asientoes/Edit/5
        public ActionResult Edit(int? modeloAeronaveId, int? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue || numeroAsiento.Value < 1 || numeroAsiento.Value > byte.MaxValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asiento.Find(modeloAeronaveId.Value, (byte)numeroAsiento.Value);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            asiento.ModeloAeronave = db.ModeloAeronave.Find(asiento.ModeloAeronaveId);
            ViewBag.ModeloAeronaveId = CargarModelos(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // POST: Asientoes/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ModeloAeronaveId,NumeroAsiento,Codigo,Fila,Lado,Ubicacion")] Asiento asiento)
        {
            if (ModelState.IsValid)
            {
                db.Entry(asiento).State = EntityState.Modified;
                db.SaveChanges();
                TempData["Ok"] = "Cambios del asiento guardados correctamente.";
                return RedirectToAction("Index");
            }
            ViewBag.ModeloAeronaveId = CargarModelos(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // GET: Asientoes/Delete/5
        public ActionResult Delete(int? modeloAeronaveId, int? numeroAsiento)
        {
            if (!modeloAeronaveId.HasValue || !numeroAsiento.HasValue || numeroAsiento.Value < 1 || numeroAsiento.Value > byte.MaxValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asiento.Find(modeloAeronaveId.Value, (byte)numeroAsiento.Value);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            asiento.ModeloAeronave = db.ModeloAeronave.Find(asiento.ModeloAeronaveId);
            return View(asiento);
        }

        // POST: Asientoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int modeloAeronaveId, int numeroAsiento)
        {
            if (numeroAsiento < 1 || numeroAsiento > byte.MaxValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiento asiento = db.Asiento.Find(modeloAeronaveId, (byte)numeroAsiento);
            if (asiento == null)
            {
                return HttpNotFound();
            }
            db.Asiento.Remove(asiento);
            db.SaveChanges();
            TempData["Ok"] = "El asiento se elimino correctamente.";
            return RedirectToAction("Index");
        }

        private List<SelectListItem> CargarModelos(int? seleccionado)
        {
            var opciones = new List<SelectListItem>();
            foreach (var modelo in db.ModeloAeronave)
            {
                string texto = (modelo.Fabricante ?? "") + " " + (modelo.Nombre ?? "") +
                               " - " + modelo.CapacidadAsientos + " asientos";
                opciones.Add(new SelectListItem
                {
                    Value = modelo.ModeloAeronaveId.ToString(),
                    Text = texto,
                    Selected = seleccionado.HasValue && seleccionado.Value == modelo.ModeloAeronaveId
                });
            }
            return opciones;
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
