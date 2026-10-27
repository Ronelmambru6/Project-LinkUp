// Mostrar u ocultar contraseñas y medir su fortaleza.
// Se usa en registro, restablecimiento de contraseña y Mi perfil.
//
// Uso en la vista:
//   <input id="Password" data-password-strength="#fortaleza" ... />
//   <button type="button" data-toggle-password="#Password">Mostrar</button>
//   <div id="fortaleza"></div>

(function () {
    // Mostrar / ocultar: cambia el tipo del campo sin modificar su contenido
    document.addEventListener('click', function (evento) {
        const boton = evento.target.closest('[data-toggle-password]');
        if (!boton) return;

        const campo = document.querySelector(boton.dataset.togglePassword);
        if (!campo) return;

        const mostrar = campo.type === 'password';
        campo.type = mostrar ? 'text' : 'password';
        boton.textContent = mostrar ? 'Ocultar' : 'Mostrar';
        boton.setAttribute('aria-pressed', String(mostrar));
    });

    // Indicador de fortaleza: Débil (2 criterios o menos), Media (3–4), Fuerte (5)
    document.querySelectorAll('[data-password-strength]').forEach(function (campo) {
        const indicador = document.querySelector(campo.dataset.passwordStrength);
        if (!indicador) return;

        const actualizar = function () {
            const valor = campo.value;
            if (!valor) {
                indicador.textContent = '';
                indicador.className = 'form-text';
                return;
            }

            const cumplidos = [
                valor.length >= 8,
                /[A-Z]/.test(valor),
                /[a-z]/.test(valor),
                /\d/.test(valor),
                /[^A-Za-z0-9]/.test(valor)
            ].filter(Boolean).length;

            let texto, color, ancho;
            if (cumplidos <= 2) { texto = 'Débil'; color = 'danger'; ancho = 33; }
            else if (cumplidos <= 4) { texto = 'Media'; color = 'warning'; ancho = 66; }
            else { texto = 'Fuerte'; color = 'success'; ancho = 100; }

            indicador.className = 'form-text';
            indicador.replaceChildren();

            const progreso = document.createElement('div');
            progreso.className = 'progress mt-1';
            progreso.style.height = '6px';

            const barra = document.createElement('div');
            barra.className = 'progress-bar bg-' + color;
            barra.style.width = ancho + '%';
            progreso.appendChild(barra);

            const etiqueta = document.createElement('span');
            etiqueta.className = 'fw-semibold text-' + color;
            etiqueta.textContent = 'Fortaleza: ' + texto;

            indicador.append(progreso, etiqueta);
        };

        campo.addEventListener('input', actualizar);
        actualizar();
    });
})();
