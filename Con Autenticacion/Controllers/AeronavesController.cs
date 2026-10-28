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
    public class AeronavesController : Controller
    {
        
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Aeronaves
        // PLANTILLA #19
        public ActionResult Index()
        {
            var aeronaves = new List<Aeronave>();
            var totalAeronaves = 0;
            var aeronavesActivas = 0;
            var aeronavesEnMantenimiento = 0;
            var capacidadTotal = 0;
            var modelos = new List<ModeloAeronave>();
            var estados = new List<EstadoAeronave>();
            var vuelosPorAeronave = new Dictionary<int, int>();

            try
            {
                // Cargar todas las aeronaves
                foreach (var aeronave in db.Aeronave)
                {
                    aeronaves.Add(aeronave);
                    totalAeronaves++;

                    // Contar vuelos por aeronave
                    var vuelosCount = 0;
                    try
                    {
                        foreach (var vuelo in db.Vuelo)
                        {
                            if (vuelo.AeronaveId == aeronave.AeronaveId)
                            {
                                vuelosCount++;
                            }
                        }
                    }
                    catch { }
                    vuelosPorAeronave[aeronave.AeronaveId] = vuelosCount;

                    // Contar capacidad
                    try
                    {
                        if (aeronave.ModeloAeronave != null)
                        {
                            capacidadTotal += aeronave.ModeloAeronave.CapacidadAsientos;
                        }
                    }
                    catch { }

                    // Contar por estado
                    try
                    {
                        if (aeronave.EstadoAeronave != null)
                        {
                            if (aeronave.EstadoAeronave.Descripcion == "Activa")
                            {
                                aeronavesActivas++;
                            }
                            else if (aeronave.EstadoAeronave.Descripcion.Contains("mantenimiento"))
                            {
                                aeronavesEnMantenimiento++;
                            }
                        }
                    }
                    catch { }
                }

                // Cargar modelos para dropdown
                foreach (var modelo in db.ModeloAeronave)
                {
                    modelos.Add(modelo);
                }

                // Cargar estados para dropdown
                foreach (var estado in db.EstadoAeronave)
                {
                    estados.Add(estado);
                }
            }
            catch { }

            // Calcular capacidad promedio
            var capacidadPromedio = totalAeronaves > 0 ? capacidadTotal / totalAeronaves : 0;

            // Pasar datos a la vista
            ViewBag.TotalAeronaves = totalAeronaves;
            ViewBag.AeronavesActivas = aeronavesActivas;
            ViewBag.AeronavesEnMantenimiento = aeronavesEnMantenimiento;
            ViewBag.CapacidadPromedio = capacidadPromedio;
            ViewBag.ListaAeronaves = aeronaves;
            ViewBag.Modelos = modelos;
            ViewBag.Estados = estados;
            ViewBag.VuelosPorAeronave = vuelosPorAeronave;

            return View();
        }

        // GET: Aeronaves/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // GET: Aeronaves/Create
        public ActionResult Create()
        {
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion");
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante");
            return View();
        }

        // POST: Aeronaves/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                db.Aeronave.Add(aeronave);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // POST: Aeronaves/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                db.Entry(aeronave).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Fabricante", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // POST: Aeronaves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeronave aeronave = db.Aeronave.Find(id);
            db.Aeronave.Remove(aeronave);
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
