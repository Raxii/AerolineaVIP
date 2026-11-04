
// =====================================
// VALIDACION DE ORIGEN Y DESTINO
// =====================================

(function () {

    
    var form = document.getElementById("form-buscar-home");
    if (!form) return;

    form.addEventListener("submit", function (e) {

        var origen = document.getElementById("home-origen").value;
        var destino = document.getElementById("home-destino").value;
        if (origen && destino && origen === destino) {

            e.preventDefault();
            alert("El aeropuerto de origen y destino deben ser diferentes.");
        }
    });
})();