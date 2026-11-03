/* Aerolinea VIP - seleccion de pasajeros y asientos; sin jQuery ni LINQ. */
(function () {
    "use strict";
    document.addEventListener("DOMContentLoaded", function () {
        var selector = document.getElementById("cantidadPasajerosSelector");
        var cantidadOculta = document.getElementById("cantidadPasajerosHidden");
        var codigosOcultos = document.getElementById("codigosAsientosInput");
        var continuar = document.getElementById("btnContinuar");
        var resumen = document.getElementById("asientosSeleccionados") || document.getElementById("resumenAsientos");
        var contador = document.getElementById("contadorSeleccion");
        var estado = document.getElementById("estadoSeleccion");
        var mensaje = document.getElementById("mensajeSeleccion");
        var formulario = document.getElementById("formContinuar") || document.getElementById("formSeleccion");
        var pantallaDisponibilidad = !!document.getElementById("configuracionDisponibilidad");
        if (!selector || !cantidadOculta || !codigosOcultos || !continuar || !formulario || !resumen) return;

        var botones = document.querySelectorAll(".js-asiento");
        var limite = parseInt(String(selector.getAttribute("data-maximo") || selector.querySelectorAll("option").length), 10);
        var seleccionados = [];
        botones.forEach(function (boton) {
            if (boton.classList.contains("vip-elegido")) {
                var codigo = boton.getAttribute("data-codigo") || boton.getAttribute("data-asiento");
                if (codigo && seleccionados.indexOf(codigo) < 0) seleccionados.push(codigo);
            }
        });
        function cantidad() {
            var opcion = selector.querySelector("option:checked");
            var n = opcion ? parseInt(opcion.getAttribute("value") || "1", 10) : 1;
            return isNaN(n) ? 1 : n;
        }
        function actualizar() {
            var n = cantidad();
            cantidadOculta.setAttribute("value", String(n));
            codigosOcultos.setAttribute("value", seleccionados.join(","));
            resumen.textContent = "";
            if (seleccionados.length === 0) {
                var vacio = document.createElement("span");
                vacio.className = "text-muted";
                vacio.textContent = "Ninguno";
                resumen.appendChild(vacio);
            } else {
                seleccionados.forEach(function (codigo) {
                    var etiqueta = document.createElement("span");
                    etiqueta.className = "badge bg-success me-2 mb-1 px-3 py-2";
                    etiqueta.textContent = "Asiento " + codigo;
                    resumen.appendChild(etiqueta);
                });
            }
            if (contador) contador.textContent = seleccionados.length + " de " + n + " asiento(s) seleccionados";
            var completo = n >= 1 && n <= limite && seleccionados.length === n;
            var bloqueado = limite < 1 || !completo;
            if (bloqueado) continuar.setAttribute("disabled", "disabled");
            else continuar.removeAttribute("disabled");
            if (estado) {
                estado.className = "small mt-1 fw-semibold " + (completo ? "text-success" : "text-warning");
                estado.textContent = completo ? "Seleccion completa" : "Selecciona " + n + " asiento(s) en total";
            }
            if (mensaje) mensaje.textContent = "";
        }
        selector.addEventListener("change", function () {
            var n = cantidad();
            while (seleccionados.length > n) {
                var quitado = seleccionados.pop();
                botones.forEach(function (boton) {
                    var codigo = boton.getAttribute("data-codigo") || boton.getAttribute("data-asiento");
                    if (codigo === quitado) {
                        boton.classList.remove("vip-elegido", "vip-seleccionado");
                        boton.classList.add("vip-libre");
                        boton.setAttribute("aria-pressed", "false");
                    }
                });
            }
            actualizar();
        });
        botones.forEach(function (boton) {
            if (boton.hasAttribute("disabled")) return;
            boton.addEventListener("click", function () {
                var codigo = boton.getAttribute("data-codigo") || boton.getAttribute("data-asiento");
                if (!codigo) return;
                var pos = seleccionados.indexOf(codigo);
                if (pos >= 0) {
                    seleccionados.splice(pos, 1);
                    boton.classList.remove("vip-elegido", "vip-seleccionado");
                    boton.classList.add("vip-libre");
                    boton.setAttribute("aria-pressed", "false");
                } else {
                    if (seleccionados.length >= cantidad()) {
                        if (mensaje) mensaje.textContent = "Ajusta el numero de pasajeros si necesitas mas asientos.";
                        return;
                    }
                    seleccionados.push(codigo);
                    boton.classList.remove("vip-libre");
                    boton.classList.add(pantallaDisponibilidad ? "vip-seleccionado" : "vip-elegido");
                    boton.setAttribute("aria-pressed", "true");
                }
                actualizar();
            });
        });
        formulario.addEventListener("submit", function (evento) {
            var n = cantidad();
            if (n < 1 || n > limite || seleccionados.length !== n) {
                evento.preventDefault();
                actualizar();
                return;
            }
            cantidadOculta.setAttribute("value", String(n));
            codigosOcultos.setAttribute("value", seleccionados.join(","));
        });
        // Si la busqueda inicial pedia mas que los asientos disponibles,
        // ajustar la cantidad visual a una opcion real, sin alterar SQL Server.
        var inicial = parseInt(selector.getAttribute("data-inicial") || "1", 10);
        var opciones = selector.querySelectorAll("option");
        var valida = false;
        opciones.forEach(function (o) {
            if (parseInt(o.getAttribute("value") || "0", 10) === inicial) valida = true;
        });
        if (!valida && opciones.length > 0) {
            opciones.forEach(function (o) { o.removeAttribute("selected"); });
            opciones[0].setAttribute("selected", "selected");
        }
        actualizar();
    });
})();
