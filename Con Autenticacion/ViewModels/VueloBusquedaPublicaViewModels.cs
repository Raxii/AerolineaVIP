using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Con_Autenticacion.ViewModels
{
    public class BusquedaVueloViewModel
    {
        [Display(Name = "Aeropuerto de origen")]
        [Required(ErrorMessage = "Seleccione el aeropuerto de origen.")]
        public int? AeropuertoOrigenId { get; set; }

        [Display(Name = "Aeropuerto de destino")]
        [Required(ErrorMessage = "Seleccione el aeropuerto de destino.")]
        public int? AeropuertoDestinoId { get; set; }

        [Display(Name = "Fecha de vuelo")]
        [Required(ErrorMessage = "Seleccione la fecha de vuelo.")]
        [DataType(DataType.Date)]
        public DateTime? Fecha { get; set; }

        [Display(Name = "Cantidad de pasajeros")]
        [Range(1, 20, ErrorMessage = "La cantidad de pasajeros debe estar entre 1 y 20.")]
        public int CantidadPasajeros { get; set; } = 1;

        public IEnumerable<SelectListItem> Aeropuertos { get; set; }
        public IEnumerable<VueloBusquedaViewModel> Resultados { get; set; }
        public bool BusquedaRealizada { get; set; }
    }

    public class VueloBusquedaViewModel
    {
        public int VueloId { get; set; }
        public DateTime FechaOperacion { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public DateTime SalidaProgramada { get; set; }
        public DateTime LlegadaProgramada { get; set; }
        public string EstadoVueloCodigo { get; set; }
        public string Matricula { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; }
        public int Disponibles { get; set; }

        public string RutaDescripcion
        {
            get { return Origen + " → " + Destino; }
        }
    }
}