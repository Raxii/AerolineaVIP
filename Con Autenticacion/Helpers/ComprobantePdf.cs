using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Con_Autenticacion.ViewModels;

namespace Con_Autenticacion.Helpers
{
    // Generador PDF sencillo para comprobantes de confirmacion.
    // No usa paquetes externos. Texto en ASCII para compatibilidad PDF basica.
    public static class ComprobantePdf
    {
        public static byte[] Generar(ComprobanteClienteVM comprobante, ReservaClienteVM reserva)
        {
            var lineas = new List<string>();
            lineas.Add("AEROLINEA VIP");
            lineas.Add("COMPROBANTE DE CONFIRMACION DE PAGO");
            lineas.Add("");
            lineas.Add("Numero: " + comprobante.NumeroComprobante);
            lineas.Add("Reserva: " + comprobante.CodigoReserva);
            lineas.Add("Fecha: " + comprobante.FechaEmision.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
            lineas.Add("Tipo: " + comprobante.Tipo);
            lineas.Add("Estado: " + comprobante.Estado);
            lineas.Add("Importe: " + comprobante.ImporteCompra.ToString("N2", new CultureInfo("es-EC")) + " " + comprobante.Moneda);
            lineas.Add("");
            lineas.Add("PASAJEROS Y VUELOS");
            if (reserva != null)
            {
                foreach (string descripcion in reserva.Descripciones)
                {
                    // El salto evita que las lineas extensas se salgan del papel.
                    string texto = Normalizar(descripcion);
                    while (texto.Length > 85)
                    {
                        int corte = texto.LastIndexOf(' ', 84);
                        if (corte < 30) corte = 85;
                        lineas.Add(texto.Substring(0, corte));
                        texto = texto.Substring(corte).TrimStart();
                    }
                    if (texto.Length > 0) lineas.Add(texto);
                    lineas.Add("");
                }
            }
            lineas.Add("Documento de confirmacion. No constituye factura tributaria.");

            // A4 (595 x 842 puntos), una pagina por cada 39 renglones.
            var paginas = new List<string>();
            for (int indice = 0; indice < lineas.Count; indice += 39)
            {
                StringBuilder contenido = new StringBuilder();
                int limite = Math.Min(indice + 39, lineas.Count);
                int y = 790;
                for (int i = indice; i < limite; i++)
                {
                    int fuente = i == 0 ? 19 : (i == 1 ? 13 : 10);
                    string texto = Escapar(Normalizar(lineas[i]));
                    contenido.Append("BT /F1 ").Append(fuente)
                        .Append(" Tf 48 ").Append(y)
                        .Append(" Td (").Append(texto).Append(") Tj ET\n");
                    y -= i < 2 ? 30 : 19;
                }
                paginas.Add(contenido.ToString());
            }
            return CrearPdf(paginas);
        }

        private static string Normalizar(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            string descompuesto = valor.Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder();
            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;
                resultado.Append(c >= 32 && c <= 126 ? c : '-');
            }
            return resultado.ToString();
        }

        private static string Escapar(string texto)
        {
            return texto.Replace("\\", "\\\\")
                        .Replace("(", "\\(")
                        .Replace(")", "\\)");
        }

        private static byte[] CrearPdf(List<string> paginas)
        {
            int totalPaginas = paginas.Count;
            int numeroObjetos = 3 + 2 * totalPaginas;
            var posiciones = new List<long>();
            posiciones.Add(0);
            using (MemoryStream memoria = new MemoryStream())
            {
                Action<string> escribir = s =>
                {
                    byte[] datos = Encoding.ASCII.GetBytes(s);
                    memoria.Write(datos, 0, datos.Length);
                };
                Action<int, string> objeto = (numero, cuerpo) =>
                {
                    posiciones.Add(memoria.Position);
                    escribir(numero + " 0 obj\n" + cuerpo + "\nendobj\n");
                };
                escribir("%PDF-1.4\n");
                objeto(1, "<< /Type /Catalog /Pages 2 0 R >>");
                StringBuilder hijos = new StringBuilder();
                for (int p = 0; p < totalPaginas; p++)
                    hijos.Append((4 + 2 * p) + " 0 R ");
                objeto(2, "<< /Type /Pages /Kids [ " + hijos + "] /Count " + totalPaginas + " >>");
                objeto(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                for (int p = 0; p < totalPaginas; p++)
                {
                    int paginaId = 4 + 2 * p;
                    int contenidoId = paginaId + 1;
                    objeto(paginaId, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents " + contenidoId + " 0 R >>");
                    string contenido = paginas[p];
                    int largo = Encoding.ASCII.GetByteCount(contenido);
                    objeto(contenidoId, "<< /Length " + largo + " >>\nstream\n" + contenido + "endstream");
                }
                long xref = memoria.Position;
                escribir("xref\n0 " + (numeroObjetos + 1) + "\n0000000000 65535 f \n");
                foreach (long posicion in posiciones.GetRange(1, posiciones.Count - 1))
                    escribir(posicion.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
                escribir("trailer\n<< /Size " + (numeroObjetos + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF");
                return memoria.ToArray();
            }
        }
    }
}
