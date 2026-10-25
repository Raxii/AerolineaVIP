using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Con_Autenticacion.ViewModels
{
    public class VueloAdminIndexViewModel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string Estado { get; set; }
        public IEnumerable<SelectListItem> Estados { get; set; }
        public IEnumerable<VueloBusquedaViewModel> Vuelos { get; set; }
    }

    public class VueloFormViewModel
    {
        public int VueloId { get; set; }

        [Display(Name = "Frecuencia")]
        [Required(ErrorMessage = "Seleccione una frecuencia.")]
        public int FrecuenciaId { get; set; }

        [Display(Name = "Fecha de operación")]
        [Required(ErrorMessage = "Seleccione la fecha de operación.")]
        [DataType(DataType.Date)]
        public DateTime? FechaOperacion { get; set; }

        [Display(Name = "Aeronave")]
        public int? AeronaveId { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "Seleccione el estado del vuelo.")]
        public string EstadoVueloCodigo { get; set; }
    }

    public class FrecuenciaOpcionViewModel
    {
        public int FrecuenciaId { get; set; }
        public int RutaId { get; set; }
        public byte DiaSemana { get; set; }
        public string HoraSalida { get; set; }
        public string HoraLlegada { get; set; }
        public string Origen { get; set; }
        public string Destino { get; set; }
        public string Descripcion { get; set; }
    }
}