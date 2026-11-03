
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Con_Autenticacion.Models;

namespace Con_Autenticacion.Controllers
{
    [Authorize]
    public class PasajeroesController : Controller
    {
        private readonly AerolineaVIP_Context db =
            new AerolineaVIP_Context();

        // =====================================================
        // PERMISOS
        // =====================================================

        private bool EsPersonalAdministrativo()
        {
            return User.IsInRole("SuperUsuario")
                || User.IsInRole("Administrador")
                || User.IsInRole("Counter");
        }

        // =====================================================
        // PREPARAR FORMULARIO
        // =====================================================

        private void PrepararFormulario(
            int? vueloId,
            int? cantidadPasajeros,
            string codigosAsientos,
            string tipoDocumento = null)
        {
            ViewBag.TipoDocumentoCodigo = new SelectList(
                db.TipoDocumentoIdentidad.ToList(),
                "Codigo",
                "Descripcion",
                tipoDocumento
            );

            ViewBag.VueloId = vueloId;
            ViewBag.CantidadPasajeros =
                cantidadPasajeros.HasValue &&
                cantidadPasajeros.Value > 0
                ? cantidadPasajeros.Value : 1;

            ViewBag.CodigosAsientos =
                codigosAsientos ?? "";
        }

        // =====================================================
        // VALIDAR CEDULA ECUATORIANA
        // =====================================================

        private bool ValidarCedula(string numero)
        {
            if (String.IsNullOrWhiteSpace(numero))
                return false;

            numero = numero.Trim();

            if (numero.Length != 10)
                return false;

            for (int i = 0; i < numero.Length; i++)
            {
                if (!Char.IsDigit(numero[i]))
                    return false;
            }

            int provincia =
                Int32.Parse(numero.Substring(0, 2));

            int tercerDigito =
                Int32.Parse(numero.Substring(2, 1));

            if (provincia < 1 || provincia > 24 ||
                tercerDigito > 5)
                return false;

            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int digito =
                    Int32.Parse(numero.Substring(i, 1));

                if (i % 2 == 0)
                {
                    digito *= 2;

                    if (digito > 9)
                        digito -= 9;
                }

                suma += digito;
            }

            int verificador =
                (10 - (suma % 10)) % 10;

            return verificador ==
                Int32.Parse(numero.Substring(9, 1));
        }

        // =====================================================
        // INDEX
        // =====================================================

        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult Index()
        {
            var pasajeros = db.Pasajero
                .Include(p => p.TipoDocumentoIdentidad)
                .ToList();

            return View(pasajeros);
        }

        // =====================================================
        // DETAILS
        // =====================================================

        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Pasajero pasajero = db.Pasajero
                .Include(p => p.TipoDocumentoIdentidad)
                .FirstOrDefault(p => p.PasajeroId == id.Value);

            if (pasajero == null)
                return HttpNotFound();

            return View(pasajero);
        }

        // =====================================================
        // GET: CREATE
        // =====================================================

        [Authorize(Roles =
            "Cliente,SuperUsuario,Administrador")]
        public ActionResult Create(
            int? vueloId,
            int? cantidadPasajeros,
            string codigosAsientos)
        {
            if (!vueloId.HasValue &&
                User.IsInRole("Cliente"))
            {
                return RedirectToAction(
                    "Buscar", "Vueloes");
            }

            if (vueloId.HasValue && vueloId.Value <= 0)
                return RedirectToAction(
                    "Buscar", "Vueloes");

            int cantidad = cantidadPasajeros.HasValue
                ? cantidadPasajeros.Value : 1;

            if (cantidad < 1)
                cantidad = 1;

            PrepararFormulario(
                vueloId,
                cantidad,
                codigosAsientos
            );

            if (vueloId.HasValue && cantidad > 1)
            {
                ModelState.AddModelError(
                    "",
                    "El formulario actual registra un pasajero por operación. " +
                    "Para varios pasajeros se necesita un formulario múltiple."
                );
            }

            return View(new Pasajero());
        }

        // =====================================================
        // POST: CREATE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles =
            "Cliente,SuperUsuario,Administrador")]
        public ActionResult Create(
            [Bind(Include =
            "PasajeroId,TipoDocumentoCodigo,NumeroDocumento," +
            "Nombres,Apellidos,FechaNacimiento,Correo,Telefono")]
            Pasajero pasajero,
            int? vueloId,
            int? cantidadPasajeros,
            string codigosAsientos)
        {
            int cantidad = cantidadPasajeros.HasValue
                ? cantidadPasajeros.Value : 1;

            PrepararFormulario(
                vueloId,
                cantidad,
                codigosAsientos,
                pasajero.TipoDocumentoCodigo
            );

            if (pasajero == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            // Normalizar información.
            pasajero.NumeroDocumento =
                (pasajero.NumeroDocumento ?? "").Trim();

            pasajero.Nombres =
                (pasajero.Nombres ?? "").Trim();

            pasajero.Apellidos =
                (pasajero.Apellidos ?? "").Trim();

            pasajero.Correo =
                String.IsNullOrWhiteSpace(pasajero.Correo)
                ? null : pasajero.Correo.Trim();

            pasajero.Telefono =
                String.IsNullOrWhiteSpace(pasajero.Telefono)
                ? null : pasajero.Telefono.Trim();

            pasajero.FechaRegistro = DateTime.Now;

            // Validaciones generales.
            if (!ModelState.IsValid)
                return View(pasajero);

            if (String.IsNullOrWhiteSpace(
                pasajero.TipoDocumentoCodigo))
            {
                ModelState.AddModelError(
                    "TipoDocumentoCodigo",
                    "Seleccione el tipo de documento."
                );

                return View(pasajero);
            }

            if (pasajero.FechaNacimiento.HasValue &&
                pasajero.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "FechaNacimiento",
                    "La fecha de nacimiento no puede ser futura."
                );

                return View(pasajero);
            }

            var tipoDocumento =
                db.TipoDocumentoIdentidad.FirstOrDefault(
                    t => t.Codigo ==
                    pasajero.TipoDocumentoCodigo
                );

            if (tipoDocumento == null)
            {
                ModelState.AddModelError(
                    "TipoDocumentoCodigo",
                    "El tipo de documento seleccionado no existe."
                );

                return View(pasajero);
            }

            bool esCedula =
                (tipoDocumento.Descripcion ?? "")
                .ToUpperInvariant()
                .Contains("CEDULA")
                ||
                (tipoDocumento.Descripcion ?? "")
                .ToUpperInvariant()
                .Contains("CÉDULA")
                ||
                pasajero.TipoDocumentoCodigo
                .ToUpperInvariant() == "CI";

            if (esCedula &&
                !ValidarCedula(pasajero.NumeroDocumento))
            {
                ModelState.AddModelError(
                    "NumeroDocumento",
                    "Ingrese una cédula ecuatoriana válida."
                );

                return View(pasajero);
            }

            // =================================================
            // REGISTRO ADMINISTRATIVO SIN RESERVA
            // =================================================

            if (!vueloId.HasValue)
            {
                if (!EsPersonalAdministrativo())
                {
                    return RedirectToAction(
                        "Buscar", "Vueloes");
                }

                bool documentoExiste =
                    db.Pasajero.Any(p =>
                        p.TipoDocumentoCodigo ==
                            pasajero.TipoDocumentoCodigo &&
                        p.NumeroDocumento ==
                            pasajero.NumeroDocumento
                    );

                if (documentoExiste)
                {
                    ModelState.AddModelError(
                        "NumeroDocumento",
                        "Ya existe un pasajero con este documento."
                    );

                    return View(pasajero);
                }

                try
                {
                    db.Pasajero.Add(pasajero);
                    db.SaveChanges();

                    TempData["Mensaje"] =
                        "Pasajero registrado correctamente.";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(
                        "",
                        "No se pudo guardar el pasajero: " +
                        ex.GetBaseException().Message
                    );

                    return View(pasajero);
                }
            }

            // =================================================
            // VALIDAR CANTIDAD DE PASAJEROS
            // =================================================

            // La vista actual utiliza un único modelo Pasajero.
            // No se permite crear una reserva grupal incompleta.

            if (cantidad != 1)
            {
                ModelState.AddModelError(
                    "",
                    "El formulario actual permite registrar un pasajero. " +
                    "Seleccione un solo asiento."
                );

                return View(pasajero);
            }

            // =================================================
            // VALIDAR ASIENTO
            // =================================================

            if (String.IsNullOrWhiteSpace(codigosAsientos))
            {
                ModelState.AddModelError(
                    "",
                    "Debe seleccionar un asiento."
                );

                return View(pasajero);
            }

            var codigos = new List<string>();

            string[] codigosRecibidos =
                codigosAsientos.Split(
                    new char[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries
                );

            foreach (string codigoCrudo in codigosRecibidos)
            {
                string codigo =
                    codigoCrudo.Trim().ToUpperInvariant();

                if (codigo.Length > 0)
                    codigos.Add(codigo);
            }

            if (codigos.Count != 1)
            {
                ModelState.AddModelError(
                    "",
                    "Debe seleccionar exactamente un asiento."
                );

                return View(pasajero);
            }

            string codigoAsiento = codigos[0];

            // =================================================
            // OBTENER VUELO Y AERONAVE
            // =================================================

            Vuelo vuelo = db.Vuelo
                .Include("Aeronave.ModeloAeronave")
                .Include("Frecuencia.Ruta.Aeropuerto")
                .Include("Frecuencia.Ruta.Aeropuerto1")
                .FirstOrDefault(v =>
                    v.VueloId == vueloId.Value
                );

            if (vuelo == null)
                return HttpNotFound();

            if (vuelo.EstadoVueloCodigo != "PROGRAMADO" &&
                vuelo.EstadoVueloCodigo != "CONFIRMADO")
            {
                ModelState.AddModelError(
                    "",
                    "El vuelo ya no admite reservas."
                );

                return View(pasajero);
            }

            if (vuelo.Aeronave == null ||
                vuelo.Aeronave.ModeloAeronave == null)
            {
                ModelState.AddModelError(
                    "",
                    "El vuelo no tiene una aeronave asignada."
                );

                return View(pasajero);
            }

            int modeloId =
                vuelo.Aeronave.ModeloAeronave.ModeloAeronaveId;

            // =================================================
            // BUSCAR ASIENTO REAL
            // =================================================

            Asiento asiento = db.Asiento.FirstOrDefault(a =>
                a.ModeloAeronaveId == modeloId &&
                a.Codigo == codigoAsiento
            );

            if (asiento == null)
            {
                ModelState.AddModelError(
                    "",
                    "El asiento seleccionado no pertenece a esta aeronave."
                );

                return View(pasajero);
            }

            // =================================================
            // COMPROBAR DISPONIBILIDAD EN SQL
            // =================================================

            int asientoOcupado =
                db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.ReservaPasajeroVuelo
                      WHERE VueloId = @p0
                        AND NumeroAsiento = @p1
                        AND EstadoAsignacionCodigo
                            IN ('PENDIENTE', 'CONFIRMADA')",
                    vueloId.Value,
                    asiento.NumeroAsiento
                ).Single();

            if (asientoOcupado > 0)
            {
                ModelState.AddModelError(
                    "",
                    "El asiento ya fue reservado por otro cliente. " +
                    "Seleccione otro asiento."
                );

                return View(pasajero);
            }

            // =================================================
            // CALCULAR TARIFA REAL
            // =================================================

            if (vuelo.Frecuencia == null ||
                vuelo.Frecuencia.Ruta == null)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo obtener la ruta del vuelo."
                );

                return View(pasajero);
            }

            string origenIata = null;
            string destinoIata = null;

            if (vuelo.Frecuencia.Ruta.Aeropuerto != null)
            {
                origenIata =
                    vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA;
            }

            if (vuelo.Frecuencia.Ruta.Aeropuerto1 != null)
            {
                destinoIata =
                    vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA;
            }

            decimal? tarifa =
                TarifaCalculos.ObtenerTarifaPorTramo(
                    origenIata,
                    destinoIata
                );

            if (!tarifa.HasValue || tarifa.Value <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "No existe una tarifa válida para esta ruta."
                );

                return View(pasajero);
            }

            decimal importeTramo = tarifa.Value;

            // =================================================
            // CALCULAR EXPIRACIÓN DE LA RESERVA
            // =================================================

            DateTime ahora = DateTime.Now;

            TimeSpan horaSalida;

            if (!TimeSpan.TryParse(
                Convert.ToString(vuelo.Frecuencia.HoraSalida),
                out horaSalida))
            {
                ModelState.AddModelError(
                    "",
                    "No se pudo determinar la hora de salida."
                );

                return View(pasajero);
            }

            DateTime salidaProgramada =
                vuelo.FechaOperacion.Date.Add(horaSalida);

            // No permitir reservar a menos de 60 minutos
            // de la salida del vuelo.

            DateTime limiteSalida =
                salidaProgramada.AddMinutes(-60);

            if (limiteSalida <= ahora)
            {
                ModelState.AddModelError(
                    "",
                    "El vuelo ya se encuentra fuera del periodo permitido para reservar."
                );

                return View(pasajero);
            }

            // Política académica propuesta: máximo 24 horas.
            DateTime fechaExpiracion =
                ahora.AddHours(24);

            if (limiteSalida < fechaExpiracion)
            {
                fechaExpiracion = limiteSalida;
            }

            // =================================================
            // IDENTIFICAR PASAJERO EXISTENTE
            // =================================================

            Pasajero pasajeroGuardado =
                db.Pasajero.FirstOrDefault(p =>
                    p.TipoDocumentoCodigo ==
                        pasajero.TipoDocumentoCodigo &&
                    p.NumeroDocumento ==
                        pasajero.NumeroDocumento
                );

            bool pasajeroNuevo =
                pasajeroGuardado == null;

            if (!pasajeroNuevo)
            {
                int pasajeroYaAsignado =
                    db.Database.SqlQuery<int>(
                        @"SELECT COUNT(1)
                          FROM dbo.ReservaPasajeroVuelo
                          WHERE VueloId = @p0
                            AND PasajeroId = @p1
                            AND EstadoAsignacionCodigo
                                IN ('PENDIENTE', 'CONFIRMADA')",
                        vueloId.Value,
                        pasajeroGuardado.PasajeroId
                    ).Single();

                if (pasajeroYaAsignado > 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Este pasajero ya tiene una asignación vigente en el vuelo."
                    );

                    return View(pasajero);
                }
            }
            else
            {
                pasajeroGuardado = pasajero;
            }

            // =================================================
            // CREAR RESERVA Y ASIGNACIÓN
            // =================================================

            string usuarioId =
                User.Identity.GetUserId();

            if (String.IsNullOrWhiteSpace(usuarioId))
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.Unauthorized);
            }

            Reserva nuevaReserva = null;
            int reservaIdCreada = 0;

            try
            {
                using (var transaccion =
                    db.Database.BeginTransaction())
                {
                    // 1. Registrar pasajero si no existe.

                    if (pasajeroNuevo)
                    {
                        db.Pasajero.Add(pasajeroGuardado);
                        db.SaveChanges();
                    }

                    // 2. Crear reserva pendiente.

                    nuevaReserva = new Reserva
                    {
                        UsuarioId = usuarioId,
                        FechaCreacion = ahora,
                        FechaExpiracion = fechaExpiracion,
                        EstadoReservaCodigo = "PENDIENTE"
                    };

                    db.Reserva.Add(nuevaReserva);
                    db.SaveChanges();

                    // 3. Registrar asiento y pasajero.
                    //
                    // Se utiliza SQL directo para escribir ImporteTramo,
                    // que debe existir en la tabla de SQL Server.

                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO dbo.ReservaPasajeroVuelo
                          (
                              ReservaId,
                              PasajeroId,
                              VueloId,
                              NumeroAsiento,
                              ImporteTramo,
                              EstadoAsignacionCodigo,
                              FechaAsignacion
                          )
                          VALUES
                          (
                              @p0,
                              @p1,
                              @p2,
                              @p3,
                              @p4,
                              @p5,
                              SYSDATETIME()
                          )",
                        nuevaReserva.ReservaId,
                        pasajeroGuardado.PasajeroId,
                        vuelo.VueloId,
                        asiento.NumeroAsiento,
                        importeTramo,
                        "PENDIENTE"
                    );

                    // 4. Confirmar toda la operación.

                    reservaIdCreada =
                        nuevaReserva.ReservaId;

                    transaccion.Commit();
                }
            }
            catch (Exception ex)
            {
                // Limpiar entidades nuevas del contexto
                // después de una posible reversión.

                if (pasajeroNuevo &&
                    pasajeroGuardado != null)
                {
                    db.Entry(pasajeroGuardado).State =
                        EntityState.Detached;
                }

                if (nuevaReserva != null)
                {
                    db.Entry(nuevaReserva).State =
                        EntityState.Detached;
                }

                ModelState.AddModelError(
                    "",
                    "No se pudo completar la reserva: " +
                    ex.GetBaseException().Message
                );

                PrepararFormulario(
                    vueloId,
                    cantidad,
                    codigosAsientos,
                    pasajero.TipoDocumentoCodigo
                );

                return View(pasajero);
            }

            // =================================================
            // CONTINUAR A COMPRAS
            // =================================================

            TempData["Mensaje"] =
                "La reserva fue registrada correctamente. " +
                "Ahora puede continuar con la compra.";

            return RedirectToAction(
                "Create",
                "Compras",
                new
                {
                    reservaId = reservaIdCreada
                }
            );
        }

        // =====================================================
        // GET: EDIT
        // =====================================================

        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Pasajero pasajero =
                db.Pasajero.Find(id.Value);

            if (pasajero == null)
                return HttpNotFound();

            ViewBag.TipoDocumentoCodigo = new SelectList(
                db.TipoDocumentoIdentidad,
                "Codigo",
                "Descripcion",
                pasajero.TipoDocumentoCodigo
            );

            return View(pasajero);
        }

        // =====================================================
        // POST: EDIT
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult Edit(
            [Bind(Include =
            "PasajeroId,TipoDocumentoCodigo,NumeroDocumento," +
            "Nombres,Apellidos,FechaNacimiento,Correo,Telefono")]
            Pasajero pasajero)
        {
            if (pasajero == null)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            if (!ModelState.IsValid)
            {
                ViewBag.TipoDocumentoCodigo = new SelectList(
                    db.TipoDocumentoIdentidad,
                    "Codigo",
                    "Descripcion",
                    pasajero.TipoDocumentoCodigo
                );

                return View(pasajero);
            }

            Pasajero actual =
                db.Pasajero.Find(pasajero.PasajeroId);

            if (actual == null)
                return HttpNotFound();

            bool documentoDuplicado =
                db.Pasajero.Any(p =>
                    p.PasajeroId != pasajero.PasajeroId &&
                    p.TipoDocumentoCodigo ==
                        pasajero.TipoDocumentoCodigo &&
                    p.NumeroDocumento ==
                        pasajero.NumeroDocumento
                );

            if (documentoDuplicado)
            {
                ModelState.AddModelError(
                    "NumeroDocumento",
                    "El documento ya pertenece a otro pasajero."
                );

                ViewBag.TipoDocumentoCodigo = new SelectList(
                    db.TipoDocumentoIdentidad,
                    "Codigo",
                    "Descripcion",
                    pasajero.TipoDocumentoCodigo
                );

                return View(pasajero);
            }

            actual.TipoDocumentoCodigo =
                pasajero.TipoDocumentoCodigo;

            actual.NumeroDocumento =
                (pasajero.NumeroDocumento ?? "").Trim();

            actual.Nombres =
                (pasajero.Nombres ?? "").Trim();

            actual.Apellidos =
                (pasajero.Apellidos ?? "").Trim();

            actual.FechaNacimiento =
                pasajero.FechaNacimiento;

            actual.Correo =
                pasajero.Correo;

            actual.Telefono =
                pasajero.Telefono;

            db.SaveChanges();

            TempData["Mensaje"] =
                "Pasajero actualizado correctamente.";

            return RedirectToAction("Index");
        }

        // =====================================================
        // GET: DELETE
        // =====================================================

        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult Delete(int? id)
        {
            if (!id.HasValue)
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);

            Pasajero pasajero = db.Pasajero
                .Include(p => p.TipoDocumentoIdentidad)
                .FirstOrDefault(p =>
                    p.PasajeroId == id.Value);

            if (pasajero == null)
                return HttpNotFound();

            return View(pasajero);
        }

        // =====================================================
        // POST: DELETE
        // =====================================================

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles =
            "SuperUsuario,Administrador,Counter")]
        public ActionResult DeleteConfirmed(int id)
        {
            Pasajero pasajero =
                db.Pasajero.Find(id);

            if (pasajero == null)
                return HttpNotFound();

            int asignaciones =
                db.Database.SqlQuery<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.ReservaPasajeroVuelo
                      WHERE PasajeroId = @p0",
                    id
                ).Single();

            if (asignaciones > 0)
            {
                TempData["Error"] =
                    "No se puede eliminar este pasajero porque " +
                    "tiene historial de reservas.";

                return RedirectToAction("Index");
            }

            db.Pasajero.Remove(pasajero);
            db.SaveChanges();

            TempData["Mensaje"] =
                "Pasajero eliminado correctamente.";

            return RedirectToAction("Index");
        }

        // =====================================================
        // LIBERAR RECURSOS
        // =====================================================

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
