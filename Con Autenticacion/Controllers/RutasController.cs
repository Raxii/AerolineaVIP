
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "SuperUsuario,Administrador")]
    public class RutasController : Controller
    {
        private AerolineaVIP_Context db = new AerolineaVIP_Context();

        // ==========================================
        // INDEX
        // ==========================================
        public ActionResult Index(string buscar, string estado)
        {
            var listaRutas = new List<Ruta>();
            var todasRutas = new List<Ruta>();
            var aeropuertos = new List<Aeropuerto>();

            foreach (var aeropuerto in db.Aeropuerto)
            {
                aeropuertos.Add(aeropuerto);
            }

            foreach (var ruta in db.Ruta)
            {
                todasRutas.Add(ruta);
            }

            int total = 0;
            int activas = 0;
            int inactivas = 0;

            foreach (var ruta in todasRutas)
            {
                total++;

                foreach (var aeropuerto in aeropuertos)
                {
                    if (aeropuerto.AeropuertoId == ruta.AeropuertoOrigenId)
                    {
                        ruta.Aeropuerto = aeropuerto;
                    }

                    if (aeropuerto.AeropuertoId == ruta.AeropuertoDestinoId)
                    {
                        ruta.Aeropuerto1 = aeropuerto;
                    }
                }

                if (ruta.Activa)
                {
                    activas++;
                }
                else
                {
                    inactivas++;
                }

                bool incluir = true;

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string origen = ruta.Aeropuerto != null
                        ? ruta.Aeropuerto.CodigoIATA : "";

                    string destino = ruta.Aeropuerto1 != null
                        ? ruta.Aeropuerto1.CodigoIATA : "";

                    string texto = ruta.RutaId + " " + origen + " " + destino;

                    if (!Contiene(texto, buscar))
                    {
                        incluir = false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    if (estado == "Activa" && !ruta.Activa)
                    {
                        incluir = false;
                    }

                    if (estado == "Inactiva" && ruta.Activa)
                    {
                        incluir = false;
                    }
                }

                if (incluir)
                {
                    listaRutas.Add(ruta);
                }
            }

            ViewBag.ListaRutas = listaRutas;
            ViewBag.TotalRutas = total;
            ViewBag.RutasActivas = activas;
            ViewBag.RutasInactivas = inactivas;
            ViewBag.Buscar = buscar ?? "";
            ViewBag.EstadoSeleccionado = estado ?? "";

            return View();
        }

        // ==========================================
        // DETAILS
        // ==========================================
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

            ruta.Aeropuerto = db.Aeropuerto.Find(ruta.AeropuertoOrigenId);
            ruta.Aeropuerto1 = db.Aeropuerto.Find(ruta.AeropuertoDestinoId);

            ViewBag.FrecuenciasAsociadas = ContarFrecuencias(ruta.RutaId);

            return View(ruta);
        }

        // ==========================================
        // CREATE - GET
        // ==========================================
        public ActionResult Create()
        {
            PrepararAeropuertos(null, null);

            return View(new Ruta
            {
                Activa = true
            });
        }

        // ==========================================
        // CREATE - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            [Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")]
            Ruta ruta)
        {
            if (ruta.AeropuertoOrigenId == ruta.AeropuertoDestinoId)
            {
                ModelState.AddModelError(
                    "AeropuertoDestinoId",
                    "El origen y el destino deben ser diferentes."
                );
            }

            if (db.Aeropuerto.Find(ruta.AeropuertoOrigenId) == null)
            {
                ModelState.AddModelError(
                    "AeropuertoOrigenId",
                    "El aeropuerto de origen no existe."
                );
            }

            if (db.Aeropuerto.Find(ruta.AeropuertoDestinoId) == null)
            {
                ModelState.AddModelError(
                    "AeropuertoDestinoId",
                    "El aeropuerto de destino no existe."
                );
            }

            if (ModelState.IsValid)
            {
                db.Ruta.Add(ruta);
                db.SaveChanges();

                TempData["Ok"] = "Ruta registrada correctamente.";

                return RedirectToAction("Index");
            }

            PrepararAeropuertos(
                ruta.AeropuertoOrigenId,
                ruta.AeropuertoDestinoId
            );

            return View(ruta);
        }


        // GET: Rutas/Edit/5

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Buscar directamente por clave primaria
            Ruta ruta = db.Ruta.Find(id.Value);

            if (ruta == null)
            {
                return HttpNotFound();
            }

            // Guardar los identificadores originales
            int origenOriginal = ruta.AeropuertoOrigenId;
            int destinoOriginal = ruta.AeropuertoDestinoId;

            // Recuperar los aeropuertos
            Aeropuerto aeropuertoOrigen =
                db.Aeropuerto.Find(origenOriginal);

            Aeropuerto aeropuertoDestino =
                db.Aeropuerto.Find(destinoOriginal);

            // Preparar las listas
            PrepararAeropuertos(
                origenOriginal,
                destinoOriginal
            );

            // Diagnostico temporal
            ViewBag.DiagnosticoOrigen = origenOriginal;
            ViewBag.DiagnosticoDestino = destinoOriginal;

            ViewBag.NombreOrigen = aeropuertoOrigen != null
                ? aeropuertoOrigen.CodigoIATA
                : "NO ENCONTRADO";

            ViewBag.NombreDestino = aeropuertoDestino != null
                ? aeropuertoDestino.CodigoIATA
                : "NO ENCONTRADO";

            return View(ruta);
        }



        // POST: Rutas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,Activa")]
    Ruta ruta)
        {
            if (ruta.AeropuertoOrigenId == ruta.AeropuertoDestinoId)
            {
                ModelState.AddModelError(
                    "AeropuertoDestinoId",
                    "El origen y el destino deben ser diferentes."
                );
            }

            Ruta rutaActual = null;

            // Buscar el registro original
            foreach (var item in db.Ruta)
            {
                if (item.RutaId == ruta.RutaId)
                {
                    rutaActual = item;
                    break;
                }
            }

            if (rutaActual == null)
            {
                return HttpNotFound();
            }

            // Comprobar que ambos aeropuertos existan
            if (db.Aeropuerto.Find(ruta.AeropuertoOrigenId) == null)
            {
                ModelState.AddModelError(
                    "AeropuertoOrigenId",
                    "El aeropuerto de origen no existe."
                );
            }

            if (db.Aeropuerto.Find(ruta.AeropuertoDestinoId) == null)
            {
                ModelState.AddModelError(
                    "AeropuertoDestinoId",
                    "El aeropuerto de destino no existe."
                );
            }

            if (ModelState.IsValid)
            {
                rutaActual.AeropuertoOrigenId = ruta.AeropuertoOrigenId;
                rutaActual.AeropuertoDestinoId = ruta.AeropuertoDestinoId;
                rutaActual.Activa = ruta.Activa;

                db.SaveChanges();

                TempData["Ok"] = "Los cambios de la ruta se guardaron correctamente.";

                return RedirectToAction("Index");
            }

            // Si hay errores, recuperar las listas nuevamente
            PrepararListasAeropuertos(
                ruta.AeropuertoOrigenId,
                ruta.AeropuertoDestinoId
            );

            return View(ruta);
        }


        // Preparar listas de aeropuertos
        private void PrepararListasAeropuertos(
            int? origenSeleccionado,
            int? destinoSeleccionado)
        {
            var listaOrigen = new List<SelectListItem>();
            var listaDestino = new List<SelectListItem>();

            foreach (var aeropuerto in db.Aeropuerto)
            {
                string texto =
                    (aeropuerto.CodigoIATA ?? "") + " - " +
                    (aeropuerto.Localidad ?? "") + " | " +
                    (aeropuerto.Nombre ?? "");

                listaOrigen.Add(new SelectListItem
                {
                    Value = aeropuerto.AeropuertoId.ToString(),
                    Text = texto,
                    Selected = origenSeleccionado.HasValue &&
                               aeropuerto.AeropuertoId == origenSeleccionado.Value
                });

                listaDestino.Add(new SelectListItem
                {
                    Value = aeropuerto.AeropuertoId.ToString(),
                    Text = texto,
                    Selected = destinoSeleccionado.HasValue &&
                               aeropuerto.AeropuertoId == destinoSeleccionado.Value
                });
            }

            ViewBag.AeropuertoOrigenId = listaOrigen;
            ViewBag.AeropuertoDestinoId = listaDestino;
        }

        // ==========================================
        // DELETE - GET
        // ==========================================
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

            ruta.Aeropuerto = db.Aeropuerto.Find(
                ruta.AeropuertoOrigenId
            );

            ruta.Aeropuerto1 = db.Aeropuerto.Find(
                ruta.AeropuertoDestinoId
            );

            ViewBag.FrecuenciasAsociadas = ContarFrecuencias(ruta.RutaId);

            return View(ruta);
        }

        // ==========================================
        // DELETE - POST
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Ruta.Find(id);

            if (ruta == null)
            {
                return HttpNotFound();
            }

            if (ContarFrecuencias(id) > 0)
            {
                TempData["Error"] =
                    "No se puede eliminar esta ruta porque tiene frecuencias asociadas. " +
                    "Desactiva la ruta si ya no debe utilizarse.";

                return RedirectToAction("Delete", new { id = id });
            }

            db.Ruta.Remove(ruta);
            db.SaveChanges();

            TempData["Ok"] = "La ruta se elimino correctamente.";

            return RedirectToAction("Index");
        }

        // ==========================================
        // PREPARAR AEROPUERTOS
        // ==========================================
        private void PrepararAeropuertos(
            int? origenSeleccionado,
            int? destinoSeleccionado)
        {
            ViewBag.AeropuertoOrigenId =
                CargarAeropuertos(origenSeleccionado);

            ViewBag.AeropuertoDestinoId =
                CargarAeropuertos(destinoSeleccionado);
        }

        // ==========================================
        // CARGAR AEROPUERTOS
        // ==========================================
        private List<SelectListItem> CargarAeropuertos(int? seleccionado)
        {
            var opciones = new List<SelectListItem>();

            foreach (var aeropuerto in db.Aeropuerto)
            {
                string texto =
                    (aeropuerto.CodigoIATA ?? "") + " - " +
                    (aeropuerto.Localidad ?? "") + " | " +
                    (aeropuerto.Nombre ?? "");

                opciones.Add(new SelectListItem
                {
                    Value = aeropuerto.AeropuertoId.ToString(),
                    Text = texto,
                    Selected = seleccionado.HasValue &&
                        seleccionado.Value == aeropuerto.AeropuertoId
                });
            }

            return opciones;
        }

        // ==========================================
        // CONTAR FRECUENCIAS
        // ==========================================
        private int ContarFrecuencias(int rutaId)
        {
            int total = 0;

            foreach (var frecuencia in db.Frecuencia)
            {
                if (frecuencia.RutaId == rutaId)
                {
                    total++;
                }
            }

            return total;
        }

        // ==========================================
        // BUSCAR TEXTO SIN LINQ
        // ==========================================
        private bool Contiene(string texto, string buscar)
        {
            if (string.IsNullOrEmpty(texto) ||
                string.IsNullOrEmpty(buscar))
            {
                return false;
            }

            return texto.IndexOf(
                buscar,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

        // ==========================================
        // LIBERAR RECURSOS
        // ==========================================
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
