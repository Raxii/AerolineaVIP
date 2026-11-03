using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Net;
using System.Web.Mvc;
using Con_Autenticacion.Models;
using Con_Autenticacion.ViewModels;
using Microsoft.AspNet.Identity;

namespace Con_Autenticacion.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class RegistroPasajerosVueloController : Controller
    {
        private readonly AerolineaVIP_Context db = new AerolineaVIP_Context();

        [HttpGet]
        public ActionResult Create(int? vueloId, int? cantidadPasajeros, string codigosAsientos)
        {
            if (!vueloId.HasValue || !cantidadPasajeros.HasValue)
                return RedirectToAction("Buscar", "Vueloes");

            var vm = new RegistroPasajerosVueloVM
            {
                VueloId = vueloId.Value,
                CantidadPasajeros = cantidadPasajeros.Value,
                CodigosAsientos = codigosAsientos
            };

            string error;
            if (!PrepararYValidarVuelo(vm, out error))
            {
                TempData["ErrorSeleccion"] = error;
                return RedirectToAction("Seleccionar", "Vueloes", new
                {
                    vueloId = vm.VueloId,
                    cantidadPasajeros = vm.CantidadPasajeros,
                    codigosAsientos = vm.CodigosAsientos
                });
            }

            var codigos = SepararCodigos(vm.CodigosAsientos);
            foreach (var codigo in codigos)
                vm.Pasajeros.Add(new DatosViajeroVM { CodigoAsiento = codigo });

            CargarTiposDocumento();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RegistroPasajerosVueloVM vm)
        {
            if (vm == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string error;
            bool vueloValido = PrepararYValidarVuelo(vm, out error);
            if (!vueloValido) ModelState.AddModelError("", error);

            var codigos = SepararCodigos(vm.CodigosAsientos);
            if (vm.Pasajeros == null || vm.Pasajeros.Count != vm.CantidadPasajeros ||
                codigos.Count != vm.CantidadPasajeros)
                ModelState.AddModelError("", "La cantidad de viajeros debe coincidir con los asientos seleccionados.");

            if (vm.Pasajeros != null)
            {
                var documentos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < vm.Pasajeros.Count; i++)
                {
                    var p = vm.Pasajeros[i];
                    if (p == null)
                    {
                        ModelState.AddModelError("", "Los datos de todos los pasajeros son obligatorios.");
                        continue;
                    }
                    // Los codigos de asiento de la vista nunca son autoridad.
                    p.CodigoAsiento = i < codigos.Count ? codigos[i] : "";
                    p.TipoDocumentoCodigo = (p.TipoDocumentoCodigo ?? "").Trim();
                    p.NumeroDocumento = (p.NumeroDocumento ?? "").Trim();
                    p.Nombres = (p.Nombres ?? "").Trim();
                    p.Apellidos = (p.Apellidos ?? "").Trim();
                    p.Correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim();
                    p.Telefono = string.IsNullOrWhiteSpace(p.Telefono) ? null : p.Telefono.Trim();

                    TipoDocumentoIdentidad tipoDocumento = null;
                    foreach (var tipo in db.TipoDocumentoIdentidad)
                    {
                        if (string.Equals(tipo.Codigo, p.TipoDocumentoCodigo, StringComparison.OrdinalIgnoreCase))
                        {
                            tipoDocumento = tipo;
                            break;
                        }
                    }

                    if (tipoDocumento == null)
                    {
                        ModelState.AddModelError("", "Selecciona un tipo de documento valido para el pasajero " + (i + 1) + ".");
                    }
                    else
                    {
                        string descripcionTipo = (tipoDocumento.Descripcion ?? "").ToUpperInvariant();
                        bool esCedula = descripcionTipo.Contains("CEDULA") || descripcionTipo.Contains("CÉDULA") ||
                                        p.TipoDocumentoCodigo.ToUpperInvariant() == "CI";
                        bool esPasaporte = descripcionTipo.Contains("PASAPORTE") ||
                                           p.TipoDocumentoCodigo.ToUpperInvariant().Contains("PAS");

                        if (esCedula && !ValidarCedulaEcuador(p.NumeroDocumento))
                        {
                            ModelState.AddModelError("", "La cedula del pasajero " + (i + 1) + " no es valida.");
                        }
                        else if (esPasaporte && !ValidarPasaporte(p.NumeroDocumento))
                        {
                            ModelState.AddModelError("", "El pasaporte del pasajero " + (i + 1) + " debe contener entre 5 y 20 letras o numeros.");
                        }
                    }

                    if (p.FechaNacimiento.HasValue &&
                        (p.FechaNacimiento.Value.Date <= new DateTime(1900, 1, 1) ||
                         p.FechaNacimiento.Value.Date > DateTime.Today))
                        ModelState.AddModelError("", "Revisa la fecha de nacimiento del pasajero " + (i + 1) + ".");

                    if (!string.IsNullOrEmpty(p.Telefono))
                    {
                        foreach (char c in p.Telefono)
                        {
                            if (!char.IsDigit(c) && c != '+' && c != ' ')
                            {
                                ModelState.AddModelError("", "El telefono del pasajero " + (i + 1) + " contiene caracteres no permitidos.");
                                break;
                            }
                        }
                    }
                    string clave = p.TipoDocumentoCodigo + ":" + p.NumeroDocumento;
                    if (!documentos.Add(clave))
                        ModelState.AddModelError("", "No puedes registrar dos pasajeros con el mismo documento.");
                }
            }

            if (!ModelState.IsValid)
            {
                CargarTiposDocumento();
                return View(vm);
            }

            using (var transaccion = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Revalidar los asientos dentro de la transaccion, no confiar en el navegador.
                    if (!PrepararYValidarVuelo(vm, out error))
                    {
                        ModelState.AddModelError("", error);
                        transaccion.Rollback();
                        CargarTiposDocumento();
                        return View(vm);
                    }

                    string usuarioId = User.Identity.GetUserId();
                    if (string.IsNullOrWhiteSpace(usuarioId))
                    {
                        transaccion.Rollback();
                        return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                    }

                    var reserva = new Reserva
                    {
                        UsuarioId = usuarioId,
                        FechaCreacion = DateTime.Now,
                        EstadoReservaCodigo = "PENDIENTE"
                    };
                    db.Reserva.Add(reserva);
                    db.SaveChanges();

                    decimal total = vm.TotalEstimado;
                    decimal basePorPersona = Math.Floor((total * 100m) / vm.CantidadPasajeros) / 100m;
                    decimal acumulado = 0m;

                    for (int i = 0; i < vm.CantidadPasajeros; i++)
                    {
                        var datos = vm.Pasajeros[i];
                        Pasajero pasajero = null;
                        foreach (var registrado in db.Pasajero)
                        {
                            if (registrado.TipoDocumentoCodigo == datos.TipoDocumentoCodigo &&
                                registrado.NumeroDocumento == datos.NumeroDocumento)
                            {
                                pasajero = registrado;
                                break;
                            }
                        }

                        if (pasajero != null)
                        {
                            if (!string.Equals(pasajero.Nombres.Trim(), datos.Nombres, StringComparison.OrdinalIgnoreCase) ||
                                !string.Equals(pasajero.Apellidos.Trim(), datos.Apellidos, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("El documento de un pasajero ya existe con otros nombres. Verifica sus datos.");
                        }
                        else
                        {
                            pasajero = new Pasajero
                            {
                                TipoDocumentoCodigo = datos.TipoDocumentoCodigo,
                                NumeroDocumento = datos.NumeroDocumento,
                                Nombres = datos.Nombres,
                                Apellidos = datos.Apellidos,
                                FechaNacimiento = datos.FechaNacimiento,
                                Correo = datos.Correo,
                                Telefono = datos.Telefono,
                                FechaRegistro = DateTime.Now
                            };
                            db.Pasajero.Add(pasajero);
                            db.SaveChanges();
                        }

                        byte numeroAsiento = ObtenerNumeroAsiento(vm.VueloId, codigos[i]);
                        decimal importe = i == vm.CantidadPasajeros - 1 ?
                            total - acumulado : basePorPersona;
                        acumulado += importe;

                        db.ReservaPasajeroVuelo.Add(new ReservaPasajeroVuelo
                        {
                            ReservaId = reserva.ReservaId,
                            PasajeroId = pasajero.PasajeroId,
                            VueloId = vm.VueloId,
                            NumeroAsiento = numeroAsiento,
                            ImporteTramo = importe,
                            EstadoAsignacionCodigo = "PENDIENTE",
                            FechaAsignacion = DateTime.Now
                        });
                    }
                    db.SaveChanges();
                    transaccion.Commit();
                    TempData["Mensaje"] = "La reserva fue registrada correctamente. Continua con el proceso de compra.";
                    return RedirectToAction("Create", "Compras", new { reservaId = reserva.ReservaId });
                }
                catch (InvalidOperationException ex)
                {
                    transaccion.Rollback();
                    ModelState.AddModelError("", ex.Message);
                }
                catch (DbUpdateException)
                {
                    transaccion.Rollback();
                    ModelState.AddModelError("", "No se pudo registrar la reserva. Es posible que un asiento ya haya sido reservado. Actualiza la disponibilidad.");
                }
                catch (Exception)
                {
                    transaccion.Rollback();
                    ModelState.AddModelError("", "No se pudo completar el registro. Comprueba los datos e intenta de nuevo.");
                }
            }

            CargarTiposDocumento();
            return View(vm);
        }

        private bool PrepararYValidarVuelo(RegistroPasajerosVueloVM vm, out string error)
        {
            error = null;
            if (vm.VueloId <= 0 || vm.CantidadPasajeros < 1 || vm.CantidadPasajeros > 7)
            {
                error = "Selecciona entre 1 y 7 pasajeros.";
                return false;
            }

            Vuelo vuelo = db.Vuelo.Find(vm.VueloId);
            if (vuelo == null || vuelo.Aeronave == null || vuelo.Aeronave.ModeloAeronave == null ||
                (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO"))
            {
                error = "El vuelo no esta disponible para reservas.";
                return false;
            }
            if (vuelo.Frecuencia == null || vuelo.Frecuencia.Ruta == null)
            {
                error = "No se pudo obtener la ruta del vuelo.";
                return false;
            }

            var codigos = SepararCodigos(vm.CodigosAsientos);
            if (codigos.Count != vm.CantidadPasajeros)
            {
                error = "Selecciona un asiento distinto por cada pasajero.";
                return false;
            }
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var numeros = new HashSet<byte>();
            foreach (string codigo in codigos)
            {
                if (!vistos.Add(codigo))
                {
                    error = "No se puede seleccionar el mismo asiento dos veces.";
                    return false;
                }
                byte numero = 0;
                foreach (var asiento in db.Asiento)
                {
                    if (asiento.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId &&
                        string.Equals((asiento.Codigo ?? "").Trim(), codigo, StringComparison.OrdinalIgnoreCase))
                    {
                        numero = asiento.NumeroAsiento;
                        break;
                    }
                }
                if (numero == 0)
                {
                    error = "El asiento " + codigo + " no pertenece a esta aeronave.";
                    return false;
                }
                numeros.Add(numero);
            }
            foreach (var asignacion in db.ReservaPasajeroVuelo)
            {
                if (asignacion.VueloId == vm.VueloId &&
                    (asignacion.EstadoAsignacionCodigo == "PENDIENTE" ||
                     asignacion.EstadoAsignacionCodigo == "CONFIRMADA") &&
                    numeros.Contains(asignacion.NumeroAsiento))
                {
                    error = "Uno de los asientos ya fue reservado. Actualiza la disponibilidad.";
                    return false;
                }
            }

            string origen = vuelo.Frecuencia.Ruta.Aeropuerto != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto.CodigoIATA.Trim() : "";
            string destino = vuelo.Frecuencia.Ruta.Aeropuerto1 != null
                ? vuelo.Frecuencia.Ruta.Aeropuerto1.CodigoIATA.Trim() : "";
            vm.Ruta = origen + " - " + destino;
            vm.FechaOperacion = vuelo.FechaOperacion;

            // Misma calculadora utilizada por Buscar y Resultados.
            decimal? tarifa = TarifaCalculos.ObtenerTarifaPorTramo(origen, destino);
            bool descuento;
            decimal? total = TarifaCalculos.CalcularTotalReserva(tarifa, vm.CantidadPasajeros, out descuento);
            if (!tarifa.HasValue || !total.HasValue || total.Value < 0)
            {
                error = "No existe una tarifa valida para este vuelo.";
                return false;
            }
            vm.TarifaPorPasajero = tarifa.Value;
            vm.TotalEstimado = total.Value;
            return true;
        }

        private byte ObtenerNumeroAsiento(int vueloId, string codigo)
        {
            Vuelo vuelo = db.Vuelo.Find(vueloId);
            foreach (var asiento in db.Asiento)
            {
                if (asiento.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId &&
                    string.Equals((asiento.Codigo ?? "").Trim(), codigo, StringComparison.OrdinalIgnoreCase))
                    return asiento.NumeroAsiento;
            }
            throw new InvalidOperationException("Uno de los asientos ya no esta disponible.");
        }

        private bool ValidarPasaporte(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero)) return false;
            numero = numero.Trim();
            if (numero.Length < 5 || numero.Length > 20) return false;
            foreach (char caracter in numero)
            {
                if (!char.IsLetterOrDigit(caracter)) return false;
            }
            return true;
        }

        private bool ValidarCedulaEcuador(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero)) return false;
            numero = numero.Trim();
            if (numero.Length != 10) return false;
            for (int i = 0; i < numero.Length; i++)
                if (!char.IsDigit(numero[i])) return false;

            int provincia = int.Parse(numero.Substring(0, 2));
            int tercerDigito = int.Parse(numero.Substring(2, 1));
            if (provincia < 1 || provincia > 24 || tercerDigito > 5) return false;

            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int digito = int.Parse(numero.Substring(i, 1));
                if (i % 2 == 0)
                {
                    digito *= 2;
                    if (digito > 9) digito -= 9;
                }
                suma += digito;
            }
            int verificador = (10 - (suma % 10)) % 10;
            return verificador == int.Parse(numero.Substring(9, 1));
        }

        private List<string> SepararCodigos(string texto)
        {
            var lista = new List<string>();
            if (!string.IsNullOrWhiteSpace(texto))
            {
                foreach (string fragmento in texto.Split(','))
                {
                    string codigo = fragmento.Trim().ToUpperInvariant();
                    if (codigo.Length != 0) lista.Add(codigo);
                }
            }
            return lista;
        }

        private void CargarTiposDocumento()
        {
            var tipos = new List<SelectListItem>();
            foreach (var tipo in db.TipoDocumentoIdentidad)
                tipos.Add(new SelectListItem { Value = tipo.Codigo, Text = tipo.Descripcion });
            ViewBag.TiposDocumento = tipos;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
