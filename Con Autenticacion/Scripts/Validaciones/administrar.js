document.addEventListener("DOMContentLoaded", function () {

    const fecha = document.getElementById("filtroFecha");
    const ruta = document.getElementById("filtroRuta");
    const estado = document.getElementById("filtroEstado");
    const filtrar = document.getElementById("btnFiltrar");
    const limpiar = document.getElementById("btnLimpiar");
    const conflictosBtn = document.getElementById("btnConflictos");
    const contador = document.getElementById("contadorVuelos");
    const resultado = document.getElementById("resultadoConflictos");

    if (!(fecha instanceof HTMLInputElement) ||
        !(ruta instanceof HTMLSelectElement) ||
        !(estado instanceof HTMLSelectElement) ||
        !(filtrar instanceof HTMLButtonElement) ||
        !(limpiar instanceof HTMLButtonElement) ||
        !(conflictosBtn instanceof HTMLButtonElement) ||
        !(contador instanceof HTMLElement) ||
        !(resultado instanceof HTMLElement)) {
        return;
    }

    const filas = document.querySelectorAll(
        "#tablaVuelos tbody tr[data-vuelo]"
    );

    const dias = document.querySelectorAll(
        ".dia-calendario"
    );

    // CAMBIAR COLOR DEL DIA SELECCIONADO

    function marcarDia(fechaSeleccionada = "") {

        for (let i = 0; i < dias.length; i++) {

            const dia = dias[i];

            if (!(dia instanceof HTMLButtonElement)) {
                continue;
            }

            const activo =
                dia.getAttribute("data-fecha") ===
                fechaSeleccionada;

            dia.classList.toggle("dia-hoy", activo);

            dia.setAttribute(
                "aria-pressed",
                activo ? "true" : "false"
            );
        }
    }

    // FILTRAR LOS VUELOS

    function aplicarFiltros() {

        if (!(fecha instanceof HTMLInputElement) ||
            !(ruta instanceof HTMLSelectElement) ||
            !(estado instanceof HTMLSelectElement)) {
            return;
        }

        let visibles = 0;

        for (let i = 0; i < filas.length; i++) {

            const fila = filas[i];

            if (!(fila instanceof HTMLTableRowElement)) {
                continue;
            }

            const mostrar =
                (!fecha.value ||
                    fecha.value === fila.getAttribute("data-fecha")) &&

                (!ruta.value ||
                    ruta.value === fila.getAttribute("data-ruta")) &&

                (!estado.value ||
                    estado.value === fila.getAttribute("data-estado"));

            fila.style.display = mostrar ? "" : "none";

            if (mostrar) {
                visibles++;
            }
        }

        if (contador instanceof HTMLElement) {
            contador.textContent = visibles + " vuelos";
        }
    }

    // BOTON FILTRAR

    filtrar.addEventListener("click", function () {

        if (!(fecha instanceof HTMLInputElement)) {
            return;
        }

        marcarDia(fecha.value);
        aplicarFiltros();
    });

    // BOTON LIMPIAR

    limpiar.addEventListener("click", function () {

        if (!(fecha instanceof HTMLInputElement) ||
            !(ruta instanceof HTMLSelectElement) ||
            !(estado instanceof HTMLSelectElement)) {
            return;
        }

        fecha.value = "";
        ruta.value = "";
        estado.value = "";

        marcarDia("");
        aplicarFiltros();
    });

    // CLIC EN LOS DIAS DEL CALENDARIO

    for (let i = 0; i < dias.length; i++) {

        const dia = dias[i];

        if (!(dia instanceof HTMLButtonElement)) {
            continue;
        }

        dia.addEventListener("click", function () {

            if (!(fecha instanceof HTMLInputElement)) {
                return;
            }

            const seleccion =
                dia.getAttribute("data-fecha") || "";

            fecha.value = seleccion;

            // El color se mueve al dia seleccionado
            marcarDia(seleccion);

            // La tabla se actualiza
            aplicarFiltros();
        });
    }

    // CONVERTIR HORA EN MINUTOS

    function minutos(hora = "") {

        const partes = hora.split(":");

        if (partes.length < 2) {
            return -1;
        }

        const h = Number(partes[0]);
        const m = Number(partes[1]);

        if (!Number.isFinite(h) ||
            !Number.isFinite(m)) {
            return -1;
        }

        return h * 60 + m;
    }

    // CONSULTAR POSIBLES CONFLICTOS

    conflictosBtn.addEventListener("click", function () {

        let cantidad = 0;

        for (let i = 0; i < filas.length; i++) {

            const fila = filas[i];

            if (fila instanceof HTMLTableRowElement) {
                fila.classList.remove("table-warning");
            }
        }

        for (let a = 0; a < filas.length; a++) {

            const uno = filas[a];

            if (!(uno instanceof HTMLTableRowElement) ||
                uno.getAttribute("data-estado") === "CANCELADO") {
                continue;
            }

            const avion =
                uno.getAttribute("data-aeronave") || "";

            const dia =
                uno.getAttribute("data-fecha") || "";

            const desde = minutos(
                uno.getAttribute("data-inicio") || ""
            );

            const hasta = minutos(
                uno.getAttribute("data-fin") || ""
            );

            if (!avion || desde < 0 || hasta <= desde) {
                continue;
            }

            for (let b = a + 1; b < filas.length; b++) {

                const dos = filas[b];

                if (!(dos instanceof HTMLTableRowElement) ||
                    dos.getAttribute("data-estado") === "CANCELADO" ||
                    dos.getAttribute("data-aeronave") !== avion ||
                    dos.getAttribute("data-fecha") !== dia) {
                    continue;
                }

                const desde2 = minutos(
                    dos.getAttribute("data-inicio") || ""
                );

                const hasta2 = minutos(
                    dos.getAttribute("data-fin") || ""
                );

                if (desde2 < 0 || hasta2 <= desde2) {
                    continue;
                }

                if (desde < hasta2 && desde2 < hasta) {

                    cantidad++;

                    uno.classList.add("table-warning");
                    dos.classList.add("table-warning");
                }
            }
        }

        resultado.style.display = "block";

        resultado.className = cantidad > 0
            ? "alert alert-warning mt-3"
            : "alert alert-info mt-3";

        resultado.textContent = cantidad > 0
            ? "Se detectaron " + cantidad +
            " posibles conflictos. Revisa las filas resaltadas."
            : "No se detectaron coincidencias de horario " +
            "en vuelos del mismo dia y aeronave.";
    });
});