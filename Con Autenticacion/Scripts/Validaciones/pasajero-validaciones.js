
// Validación de cédula ecuatoriana.
function validarCedula(numero) {

    numero = String(numero || "").trim();

    if (!/^\d{10}$/.test(numero)) return false;

    var provincia = parseInt(numero.substring(0, 2), 10);
    var tercerDigito = parseInt(numero.charAt(2), 10);

    if (provincia < 1 || provincia > 24 || tercerDigito > 5) {
        return false;
    }

    var suma = 0;

    for (var i = 0; i < 9; i++) {

        var digito = parseInt(numero.charAt(i), 10);

        if (i % 2 === 0) {
            digito *= 2;
            if (digito > 9) digito -= 9;
        }

        suma += digito;
    }

    var verificador = (10 - (suma % 10)) % 10;

    return verificador === parseInt(numero.charAt(9), 10);
}


// Validación de formularios de pasajeros.
$(function () {

    $("#formPasajero").on("submit", function (evento) {

        var tipo = $("#TipoDocumentoCodigo").val() || "";

        var nombreTipo = $("#TipoDocumentoCodigo option:selected")
            .text().toUpperCase();

        var esCedula =
            tipo.toUpperCase() === "CEDULA" ||
            tipo.toUpperCase() === "CI" ||
            nombreTipo.indexOf("CEDULA") >= 0 ||
            nombreTipo.indexOf("CÉDULA") >= 0;

        var numero = $("#NumeroDocumento").val() || "";

        $("#errorCedula").text("");

        if (esCedula && !validarCedula(numero)) {

            $("#errorCedula").text(
                "Ingrese una cédula ecuatoriana válida."
            );

            evento.preventDefault();
        }
    });

});
