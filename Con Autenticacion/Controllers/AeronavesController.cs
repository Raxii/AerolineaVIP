using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Net;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class AeronavesController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // GET: Aeronaves
        public ActionResult Index(string buscar, int? modeloId, string estadoCodigo)
        {
            var listaAeronaves = new List<Aeronave>();
            var modelos = new List<ModeloAeronave>();
            var estados = new List<EstadoAeronave>();
            var vuelosPorAeronave = new Dictionary<int, int>();

            int totalAeronaves = 0;
            int aeronavesActivas = 0;
            int aeronavesEnMantenimiento = 0;
            int capacidadTotal = 0;

            var todosVuelos = new List<Vuelo>();
            var todasAeronaves = new List<Aeronave>();

            foreach (var modelo in db.ModeloAeronave) { modelos.Add(modelo); }
            foreach (var estado in db.EstadoAeronave) { estados.Add(estado); }
            foreach (var vuelo in db.Vuelo) { todosVuelos.Add(vuelo); }
            foreach (var aeronave in db.Aeronave) { todasAeronaves.Add(aeronave); }

            foreach (var vuelo in todosVuelos)
            {
                if (vuelo.AeronaveId.HasValue)
                {
                    int idAeronave = vuelo.AeronaveId.Value;
                    if (!vuelosPorAeronave.ContainsKey(idAeronave))
                    {
                        vuelosPorAeronave[idAeronave] = 0;
                    }
                    vuelosPorAeronave[idAeronave]++;
                }
            }

            foreach (var aeronave in todasAeronaves)
            {
                totalAeronaves++;
                foreach (var modelo in modelos)
                {
                    if (modelo.ModeloAeronaveId == aeronave.ModeloAeronaveId)
                    {
                        aeronave.ModeloAeronave = modelo;
                        break;
                    }
                }
                foreach (var estado in estados)
                {
                    if (estado.Codigo == aeronave.EstadoAeronaveCodigo)
                    {
                        aeronave.EstadoAeronave = estado;
                        break;
                    }
                }

                string codigoEstado = aeronave.EstadoAeronaveCodigo ?? "";
                string descripcionEstado = "";
                if (aeronave.EstadoAeronave != null)
                {
                    descripcionEstado = aeronave.EstadoAeronave.Descripcion ?? "";
                }

                string textoEstado = codigoEstado + " " + descripcionEstado;
                if (Contiene(textoEstado, "MANTENIMIENTO"))
                {
                    aeronavesEnMantenimiento++;
                }
                else if (Contiene(textoEstado, "ACTIV") ||
                         Contiene(textoEstado, "HABILIT") ||
                         Contiene(textoEstado, "OPERATIV"))
                {
                    aeronavesActivas++;
                }

                if (aeronave.ModeloAeronave != null)
                {
                    capacidadTotal += aeronave.ModeloAeronave.CapacidadAsientos;
                }

                bool incluir = true;

                if (modeloId.HasValue && aeronave.ModeloAeronaveId != modeloId.Value)
                {
                    incluir = false;
                }

                if (!string.IsNullOrWhiteSpace(estadoCodigo) &&
                    !string.Equals(aeronave.EstadoAeronaveCodigo, estadoCodigo, StringComparison.OrdinalIgnoreCase))
                {
                    incluir = false;
                }

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string textoBuscar = aeronave.Matricula ?? "";
                    textoBuscar += " " + aeronave.AeronaveId;
                    if (aeronave.ModeloAeronave != null)
                    {
                        textoBuscar += " " + (aeronave.ModeloAeronave.Nombre ?? "");
                        textoBuscar += " " + (aeronave.ModeloAeronave.Fabricante ?? "");
                    }

                    if (!Contiene(textoBuscar, buscar))
                    {
                        incluir = false;
                    }
                }

                if (incluir)
                {
                    listaAeronaves.Add(aeronave);
                }
            }

            int capacidadPromedio = totalAeronaves > 0 ? capacidadTotal / totalAeronaves : 0;

            ViewBag.TotalAeronaves = totalAeronaves;
            ViewBag.AeronavesActivas = aeronavesActivas;
            ViewBag.AeronavesEnMantenimiento = aeronavesEnMantenimiento;
            ViewBag.CapacidadPromedio = capacidadPromedio;
            ViewBag.ListaAeronaves = listaAeronaves;
            ViewBag.Modelos = modelos;
            ViewBag.Estados = estados;
            ViewBag.VuelosPorAeronave = vuelosPorAeronave;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.ModeloSeleccionado = modeloId;
            ViewBag.EstadoSeleccionado = estadoCodigo ?? "";

            return View();
        }

        // GET: Aeronaves/Asignaciones/5
        public ActionResult Asignaciones(int? id)
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

            aeronave.ModeloAeronave = db.ModeloAeronave.Find(aeronave.ModeloAeronaveId);
            aeronave.EstadoAeronave = db.EstadoAeronave.Find(aeronave.EstadoAeronaveCodigo);

            var vuelos = new List<Vuelo>();
            var todosVuelos = new List<Vuelo>();
            foreach (var vueloItem in db.Vuelo)
            {
                todosVuelos.Add(vueloItem);
            }

            foreach (var vuelo in todosVuelos)
            {
                if (vuelo.AeronaveId.HasValue && vuelo.AeronaveId.Value == id.Value)
                {
                    vuelo.EstadoVuelo = db.EstadoVuelo.Find(vuelo.EstadoVueloCodigo);
                    vuelo.Frecuencia = db.Frecuencia.Find(vuelo.FrecuenciaId);

                    if (vuelo.Frecuencia != null)
                    {
                        vuelo.Frecuencia.Ruta = db.Ruta.Find(vuelo.Frecuencia.RutaId);
                        if (vuelo.Frecuencia.Ruta != null)
                        {
                            vuelo.Frecuencia.Ruta.Aeropuerto = db.Aeropuerto.Find(vuelo.Frecuencia.Ruta.AeropuertoOrigenId);
                            vuelo.Frecuencia.Ruta.Aeropuerto1 = db.Aeropuerto.Find(vuelo.Frecuencia.Ruta.AeropuertoDestinoId);
                        }
                    }

                    vuelos.Add(vuelo);
                }
            }

            ViewBag.Aeronave = aeronave;
            ViewBag.TotalVuelos = vuelos.Count;
            return View(vuelos);
        }

        // POST: Aeronaves/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstado(int aeronaveId, string estadoCodigo)
        {
            if (aeronaveId <= 0 || string.IsNullOrWhiteSpace(estadoCodigo))
            {
                TempData["Error"] = "Seleccione una aeronave y un estado valido.";
                return RedirectToAction("Index");
            }

            Aeronave aeronave = db.Aeronave.Find(aeronaveId);
            EstadoAeronave estado = db.EstadoAeronave.Find(estadoCodigo);

            if (aeronave == null || estado == null)
            {
                TempData["Error"] = "No se encontro la aeronave o el estado seleccionado.";
                return RedirectToAction("Index");
            }

            aeronave.EstadoAeronaveCodigo = estado.Codigo;
            db.SaveChanges();

            TempData["Ok"] = "El estado de la aeronave se actualizo correctamente.";
            return RedirectToAction("Index");
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
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre");
            return View();
        }

        // POST: Aeronaves/Create
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
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
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
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // POST: Aeronaves/Edit/5
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
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
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
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            db.Aeronave.Remove(aeronave);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private bool Contiene(string texto, string buscar)
        {
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(buscar))
            {
                return false;
            }
            return texto.IndexOf(buscar, StringComparison.OrdinalIgnoreCase) >= 0;
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
