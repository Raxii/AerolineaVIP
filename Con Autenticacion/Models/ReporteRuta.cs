using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Con_Autenticacion.Models
{
    public class ReporteRuta
    {
        public string Ruta { get; set; }
        public int Vuelos { get; set; }
        public int Pasajeros { get; set; }
        public decimal Ocupacion { get; set; }
        public decimal Ingresos { get; set; }
        public string Estado { get; set; }
    }
}