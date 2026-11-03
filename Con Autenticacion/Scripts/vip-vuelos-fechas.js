// @ts-nocheck
// Aerolinea VIP - Fechas de operacion por dia de frecuencia.
// Archivo JavaScript independiente para evitar conflictos con Razor y su analizador TypeScript.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var ruta = document.getElementById("RutaSeleccionada");
        var frecuencia = document.getElementById("FrecuenciaId");
        var fecha = document.getElementById("FechaOperacion");
        var resumen = document.getElementById("avResumen");
        var aviso = document.getElementById("avAvisoFecha");
        if (!ruta || !frecuencia || !fecha || !resumen || !aviso) { return; }
        var formulario = fecha.form;
        var fechaOriginal = fecha.getAttribute("data-fecha-inicial") || "";
        var dias = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

        function dos(n) { return n < 10 ? "0" + n : String(n); }
        function iso(d) {
            return d.getFullYear() + "-" + dos(d.getMonth() + 1) + "-" + dos(d.getDate());
        }
        function fechaTexto(d) {
            return dias[d.getDay()] + " " + dos(d.getDate()) + "/" +
                dos(d.getMonth() + 1) + "/" + d.getFullYear();
        }
        function elegida() {
            return frecuencia.selectedIndex > 0 ? frecuencia.options[frecuencia.selectedIndex] : null;
        }
        function actualizarResumen() {
            var f = elegida();
            if (ruta.value && f && f.value) {
                var textoRuta = ruta.options[ruta.selectedIndex].text;
                var textoFecha = fecha.value && fecha.selectedIndex >= 0 ?
                    fecha.options[fecha.selectedIndex].text : "";
                resumen.textContent = textoRuta + " | " + f.text.trim() +
                    (textoFecha ? " | " + textoFecha : "");
            } else {
                resumen.textContent = "Selecciona una ruta y frecuencia para ver el horario.";
            }
        }
        function cargarFechas() {
            var anterior = fecha.value || fechaOriginal;
            var f = elegida();
            fecha.innerHTML = "";
            fecha.add(new Option("-- Seleccione una fecha --", ""));
            fecha.disabled = !f || !f.value;
            aviso.textContent = "";
            if (!f || !f.value) { actualizarResumen(); return; }

            var diaPermitido = Number(f.getAttribute("data-dia"));
            if (diaPermitido < 1 || diaPermitido > 7) {
                aviso.textContent = "La frecuencia tiene un día de semana inválido.";
                actualizarResumen();
                return;
            }
            var diaJS = diaPermitido === 7 ? 0 : diaPermitido;
            var hoy = new Date();
            hoy.setHours(12, 0, 0, 0);
            var fechaLimite = new Date(hoy.getFullYear() + 1, hoy.getMonth(), hoy.getDate(), 12);
            var siguiente = new Date(hoy.getTime());
            siguiente.setDate(siguiente.getDate() + (diaJS - siguiente.getDay() + 7) % 7);
            while (siguiente <= fechaLimite) {
                fecha.add(new Option(fechaTexto(siguiente), iso(siguiente)));
                siguiente.setDate(siguiente.getDate() + 7);
            }

            // Al editar, respetar la fecha registrada incluso si ya ha pasado.
            if (fechaOriginal && anterior === fechaOriginal) {
                var partes = fechaOriginal.split("-");
                if (partes.length === 3) {
                    var original = new Date(Number(partes[0]), Number(partes[1]) - 1, Number(partes[2]), 12);
                    var existe = false;
                    for (var j = 0; j < fecha.options.length; j++) {
                        if (fecha.options[j].value === fechaOriginal) { existe = true; break; }
                    }
                    if (original.getDay() === diaJS && !existe) {
                        fecha.add(new Option(fechaTexto(original) + " (fecha registrada)", fechaOriginal), 1);
                    }
                }
            }
            for (var i = 0; i < fecha.options.length; i++) {
                if (fecha.options[i].value === anterior) { fecha.value = anterior; break; }
            }
            if (!fecha.value) { aviso.textContent = "Elige una fecha permitida para este día."; }
            actualizarResumen();
        }
        function filtrarFrecuencias() {
            var anterior = frecuencia.value;
            var sigueValida = false;
            for (var i = 1; i < frecuencia.options.length; i++) {
                var opcion = frecuencia.options[i];
                var habilitada = ruta.value !== "" && opcion.getAttribute("data-ruta") === ruta.value;
                opcion.hidden = !habilitada;
                opcion.disabled = !habilitada;
                if (habilitada && opcion.value === anterior) { sigueValida = true; }
            }
            if (!sigueValida) { frecuencia.value = ""; fechaOriginal = ""; }
            cargarFechas();
        }
        ruta.addEventListener("change", filtrarFrecuencias);
        frecuencia.addEventListener("change", function () { fechaOriginal = ""; cargarFechas(); });
        fecha.addEventListener("change", actualizarResumen);
        if (formulario) {
            formulario.addEventListener("submit", function (evento) {
                var f = elegida();
                if (!f || !fecha.value) { evento.preventDefault(); return; }
                var d = new Date(fecha.value + "T12:00:00");
                var dia = d.getDay() === 0 ? 7 : d.getDay();
                if (dia !== Number(f.getAttribute("data-dia"))) {
                    evento.preventDefault();
                    aviso.textContent = "La fecha no corresponde al día de la frecuencia.";
                }
            });
        }
        filtrarFrecuencias();
    });
})();
