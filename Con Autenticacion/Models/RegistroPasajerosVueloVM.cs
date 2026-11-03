using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Con_Autenticacion.ViewModels
{
    public class DatosViajeroVM
    {
        [Required(ErrorMessage = "Selecciona el tipo de documento.")]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "Ingresa el numero de documento.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre 5 y 20 caracteres.")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Ingresa los nombres.")]
        [StringLength(100)]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingresa los apellidos.")]
        [StringLength(100)]
        public string Apellidos { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(256)]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido.")]
        public string Correo { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        public string CodigoAsiento { get; set; }
    }

    public class RegistroPasajerosVueloVM
    {
        public int VueloId { get; set; }
        public int CantidadPasajeros { get; set; }
        public string CodigosAsientos { get; set; }
        public string Ruta { get; set; }
        public DateTime FechaOperacion { get; set; }
        public decimal TarifaPorPasajero { get; set; }
        public decimal TotalEstimado { get; set; }
        public List<DatosViajeroVM> Pasajeros { get; set; }

        public RegistroPasajerosVueloVM()
        {
            Pasajeros = new List<DatosViajeroVM>();
        }
    }
}
