using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Con_Autenticacion.ViewModels
{
    public class RutaListaViewModel
    {
        public int RutaId { get; set; }
        public string OrigenCodigo { get; set; }
        public string OrigenNombre { get; set; }
        public string DestinoCodigo { get; set; }
        public string DestinoNombre { get; set; }
        public bool Activa { get; set; }

        public string Origen
        {
            get { return OrigenCodigo + " - " + OrigenNombre; }
        }

        public string Destino
        {
            get { return DestinoCodigo + " - " + DestinoNombre; }
        }
    }

    public class RutaFormViewModel
    {
        public int RutaId { get; set; }

        [Display(Name = "Aeropuerto de origen")]
        [Required(ErrorMessage = "Seleccione el aeropuerto de origen.")]
        public int? AeropuertoOrigenId { get; set; }

        [Display(Name = "Aeropuerto de destino")]
        [Required(ErrorMessage = "Seleccione el aeropuerto de destino.")]
        public int? AeropuertoDestinoId { get; set; }

        [Display(Name = "Ruta activa")]
        public bool Activa { get; set; }

        public IEnumerable<SelectListItem> Aeropuertos { get; set; }
    }

    public class RutaOpcionViewModel
    {
        public int RutaId { get; set; }
        public string Descripcion { get; set; }
    }
}