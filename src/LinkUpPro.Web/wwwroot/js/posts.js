// Formulario de publicaciones: muestra solo el campo del tipo elegido (imagen o enlace de YouTube)
// y limpia el otro, para que nunca se envíen los dos a la vez.
(function () {
    'use strict';

    function actualizar(formulario, limpiar) {
        var tipo = formulario.querySelector('[data-post-type]');
        var grupoImagen = formulario.querySelector('[data-post-image-group]');
        var grupoEnlace = formulario.querySelector('[data-post-link-group]');
        if (!tipo || !grupoImagen || !grupoEnlace) return;

        var esImagen = tipo.value === 'Imagen';
        var esVideo = tipo.value === 'YouTube';

        grupoImagen.hidden = !esImagen;
        grupoEnlace.hidden = !esVideo;

        if (!limpiar) return;

        if (!esImagen) {
            var archivo = grupoImagen.querySelector('input[type="file"]');
            if (archivo) archivo.value = '';
        }
        if (!esVideo) {
            var enlace = grupoEnlace.querySelector('input[type="text"], input:not([type])');
            if (enlace) enlace.value = '';
        }
    }

    document.querySelectorAll('[data-post-form]').forEach(function (formulario) {
        var tipo = formulario.querySelector('[data-post-type]');
        if (!tipo) return;

        // Al cargar solo se muestra el grupo correcto; no se borra nada (puede venir de un error de validación)
        actualizar(formulario, false);
        tipo.addEventListener('change', function () { actualizar(formulario, true); });
    });
})();
