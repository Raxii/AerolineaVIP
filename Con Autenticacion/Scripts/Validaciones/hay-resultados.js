
// =====================================
// FILTRO DE VUELOS POR HORARIO DE SALIDA
// =====================================


(function () {
    var select = document.getElementById('filtroHorario');
    var limpiar = document.getElementById('limpiarFiltroHorario');

    var tarjetas = document.querySelectorAll('.tarjeta-vuelo');


    // =====================================
    // FUNCION PARA APLICAR EL FILTRO
    // =====================================
    function aplicarFiltro() {
        var valor = select.value;
        for (var i = 0; i < tarjetas.length; i++) {

            var tarjeta = tarjetas[i];
            var minutos = parseInt(tarjeta.getAttribute('data-hora-salida'), 10);

            var visible = true;
            if (!isNaN(minutos) && minutos >= 0) {

                if (valor === 'manana')
                    visible = minutos < 12 * 60;

                else if (valor === 'tarde')
                    visible = minutos >= 12 * 60 && minutos < 18 * 60;

                else if (valor === 'noche')
                    visible = minutos >= 18 * 60;
            }

            tarjeta.style.display = visible ? '' : 'none';
        }
    }


    if (select) {
      select.addEventListener('change', aplicarFiltro);
    }

    if (limpiar) {

        limpiar.addEventListener('click', function () {
            select.value = 'todos';
            aplicarFiltro();
        });
    }

})();
