/* =====================================================================================
   MatrículaUCR — cliente asíncrono
   Sin bibliotecas de terceros. Tres comportamientos:
     1. Canje de la sesión por un token JWT que firma las llamadas a la API interna.
     2. Filtrado y paginación del catálogo sin recargar la página.
     3. Matrícula y retiro de grupos con actualización inmediata del panel lateral.
   ===================================================================================== */

(function () {
    'use strict';

    // ---------------------------------------------------------------------------------
    // Token de la API. Vive solo en memoria: no se guarda en almacenamiento local ni en
    // una cookie, así que se pierde al cerrar la pestaña, que es justo lo deseable.
    // ---------------------------------------------------------------------------------
    var credencial = { token: null, expiraEn: null };

    function obtenerToken() {
        if (credencial.token && credencial.expiraEn && new Date(credencial.expiraEn) > new Date(Date.now() + 30000)) {
            return Promise.resolve(credencial.token);
        }

        return fetch('/api/autenticacion/token', {
            method: 'POST',
            headers: { 'Accept': 'application/json' }
        }).then(function (respuesta) {
            if (!respuesta.ok) {
                throw new Error('No fue posible obtener el token de la API.');
            }
            return respuesta.json();
        }).then(function (datos) {
            credencial.token = datos.token;
            credencial.expiraEn = datos.expiraEn;
            return credencial.token;
        });
    }

    function llamarApi(ruta, opciones) {
        opciones = opciones || {};

        return obtenerToken().then(function (token) {
            var encabezados = Object.assign({
                'Accept': 'application/json',
                'Authorization': 'Bearer ' + token
            }, opciones.headers || {});

            if (opciones.body) {
                encabezados['Content-Type'] = 'application/json';
            }

            return fetch(ruta, {
                method: opciones.method || 'GET',
                headers: encabezados,
                body: opciones.body ? JSON.stringify(opciones.body) : undefined
            });
        }).then(function (respuesta) {
            return respuesta.json().then(function (datos) {
                return { ok: respuesta.ok, estado: respuesta.status, datos: datos };
            });
        });
    }

    // ---------------------------------------------------------------------------------
    // Mensajes de la interfaz. Se dibujan en el mismo contenedor que usa el servidor
    // para los avisos, de modo que la apariencia sea idéntica venga de donde venga.
    // ---------------------------------------------------------------------------------
    function mostrarAviso(mensaje, tono) {
        var contenedor = document.getElementById('avisos-dinamicos');
        if (!contenedor) { return; }

        var icono = tono === 'error' ? '#icono-alerta' : '#icono-verificado';

        contenedor.innerHTML =
            '<div class="aviso aviso--' + (tono === 'error' ? 'error' : 'exito') + '" role="status">' +
            '<svg class="icono" aria-hidden="true"><use href="' + icono + '"></use></svg>' +
            '<p>' + escapar(mensaje) + '</p></div>';

        contenedor.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function escapar(texto) {
        var caja = document.createElement('span');
        caja.textContent = texto == null ? '' : String(texto);
        return caja.innerHTML;
    }

    function formatearColones(monto) {
        return new Intl.NumberFormat('es-CR', {
            style: 'currency', currency: 'CRC', maximumFractionDigits: 0
        }).format(monto || 0);
    }

    // =================================================================================
    //  1. Catálogo de cursos: filtros y paginación sin recargar
    // =================================================================================

    function iniciarCatalogo() {
        var formulario = document.getElementById('filtros-catalogo');
        var destino = document.getElementById('resultado-catalogo');

        if (!formulario || !destino) { return; }

        var temporizador = null;

        function pedir(direccion) {
            destino.classList.add('cargando');

            fetch(direccion, { headers: { 'X-Solicitud-Asincrona': '1' } })
                .then(function (respuesta) {
                    if (!respuesta.ok) { throw new Error('Error al consultar el catálogo.'); }
                    return respuesta.text();
                })
                .then(function (html) {
                    destino.innerHTML = html;
                    destino.classList.remove('cargando');
                    history.replaceState(null, '', direccion);
                    enlazarPaginacion();
                    enlazarBotonesMatricula();
                })
                .catch(function (error) {
                    destino.classList.remove('cargando');
                    mostrarAviso(error.message, 'error');
                });
        }

        function componerDireccion(pagina) {
            var datos = new FormData(formulario);
            var parametros = new URLSearchParams();

            datos.forEach(function (valor, clave) {
                if (valor !== '' && valor !== 'false') {
                    parametros.append(clave, valor);
                }
            });

            if (pagina && pagina > 1) {
                parametros.set('pagina', pagina);
            }

            var consulta = parametros.toString();
            return formulario.getAttribute('action') + (consulta ? '?' + consulta : '');
        }

        function enlazarPaginacion() {
            destino.querySelectorAll('.paginador__paginas a').forEach(function (enlace) {
                enlace.addEventListener('click', function (evento) {
                    evento.preventDefault();
                    pedir(enlace.getAttribute('href'));
                });
            });
        }

        formulario.addEventListener('submit', function (evento) {
            evento.preventDefault();
            pedir(componerDireccion(1));
        });

        // El campo de búsqueda dispara solo, con un respiro de 350 ms para no lanzar una
        // petición por cada tecla.
        formulario.querySelectorAll('input[type="search"], input[type="text"]').forEach(function (campo) {
            campo.addEventListener('input', function () {
                clearTimeout(temporizador);
                temporizador = setTimeout(function () { pedir(componerDireccion(1)); }, 350);
            });
        });

        formulario.querySelectorAll('select, input[type="checkbox"]').forEach(function (campo) {
            campo.addEventListener('change', function () { pedir(componerDireccion(1)); });
        });

        var limpiar = document.getElementById('limpiar-filtros');
        if (limpiar) {
            limpiar.addEventListener('click', function (evento) {
                evento.preventDefault();
                formulario.reset();
                formulario.querySelectorAll('input[type="checkbox"]').forEach(function (casilla) {
                    casilla.checked = false;
                });
                pedir(componerDireccion(1));
            });
        }

        enlazarPaginacion();
        enlazarBotonesMatricula();
    }

    // =================================================================================
    //  2. Matrícula asíncrona
    // =================================================================================

    function enlazarBotonesMatricula() {
        document.querySelectorAll('[data-agregar-grupo]').forEach(function (boton) {
            if (boton.dataset.enlazado === '1') { return; }
            boton.dataset.enlazado = '1';

            boton.addEventListener('click', function (evento) {
                evento.preventDefault();
                var grupoId = parseInt(boton.getAttribute('data-agregar-grupo'), 10);

                boton.disabled = true;
                boton.textContent = 'Agregando...';

                llamarApi('/api/matricula/detalle', { method: 'POST', body: { grupoId: grupoId } })
                    .then(function (resultado) {
                        mostrarAviso(resultado.datos.mensaje, resultado.ok ? 'exito' : 'error');
                        actualizarPanel(resultado.datos);

                        if (resultado.ok) {
                            marcarComoAgregado(boton);
                        } else {
                            boton.disabled = false;
                            boton.textContent = 'Matricular';
                        }
                    })
                    .catch(function (error) {
                        mostrarAviso(error.message, 'error');
                        boton.disabled = false;
                        boton.textContent = 'Matricular';
                    });
            });
        });

        document.querySelectorAll('[data-quitar-detalle]').forEach(function (boton) {
            if (boton.dataset.enlazado === '1') { return; }
            boton.dataset.enlazado = '1';

            boton.addEventListener('click', function (evento) {
                evento.preventDefault();
                var detalleId = parseInt(boton.getAttribute('data-quitar-detalle'), 10);

                boton.disabled = true;

                llamarApi('/api/matricula/detalle/' + detalleId, { method: 'DELETE' })
                    .then(function (resultado) {
                        mostrarAviso(resultado.datos.mensaje, resultado.ok ? 'exito' : 'error');
                        actualizarPanel(resultado.datos);

                        if (resultado.ok) {
                            var fila = boton.closest('[data-fila-detalle]');
                            if (fila) { fila.remove(); }
                        } else {
                            boton.disabled = false;
                        }
                    })
                    .catch(function (error) {
                        mostrarAviso(error.message, 'error');
                        boton.disabled = false;
                    });
            });
        });
    }

    function marcarComoAgregado(boton) {
        var contenedor = boton.parentElement;
        if (!contenedor) { return; }

        contenedor.innerHTML = '<span class="indicador indicador--exito">En su matrícula</span>';
    }

    function actualizarPanel(datos) {
        if (!datos || typeof datos.creditos === 'undefined') { return; }

        asignarTexto('panel-creditos', datos.creditos);
        asignarTexto('panel-tope', datos.topeCreditos);
        asignarTexto('panel-cursos', datos.cursos);
        asignarTexto('panel-monto', formatearColones(datos.montoEstimado));

        var lista = document.getElementById('panel-detalle');

        if (lista && datos.detalle) {
            if (datos.detalle.length === 0) {
                lista.innerHTML = '<li class="tenue diminuto">Todavía no ha agregado cursos.</li>';
            } else {
                lista.innerHTML = datos.detalle.map(function (linea) {
                    return '<li data-fila-detalle>' +
                        '<strong>' + escapar(linea.codigo) + '</strong> ' +
                        escapar(linea.nombre) +
                        '<span class="celda-secundaria">' + escapar(linea.horario) +
                        ' · ' + linea.creditos + ' créditos</span>' +
                        '<button type="button" class="boton boton--sutil boton--pequeno" ' +
                        'data-quitar-detalle="' + linea.detalleId + '">Quitar</button></li>';
                }).join('');

                enlazarBotonesMatricula();
            }
        }

        var confirmar = document.getElementById('boton-confirmar');
        if (confirmar) {
            confirmar.disabled = !datos.puedeConfirmar;
        }
    }

    function asignarTexto(id, valor) {
        var elemento = document.getElementById(id);
        if (elemento) { elemento.textContent = valor; }
    }

    // =================================================================================
    //  3. Panel administrativo: indicadores y gráficos
    // =================================================================================

    function iniciarPanel() {
        var contenedor = document.getElementById('panel-administrativo');
        if (!contenedor) { return; }

        var boton = document.getElementById('actualizar-panel');
        var selector = document.getElementById('periodo-panel');

        function refrescar() {
            var periodoId = selector ? selector.value : '';
            contenedor.classList.add('cargando');

            llamarApi('/api/estadisticas/panel' + (periodoId ? '?periodoId=' + periodoId : ''))
                .then(function (resultado) {
                    contenedor.classList.remove('cargando');

                    if (!resultado.ok) {
                        mostrarAviso(resultado.datos.detalle || 'No se pudo actualizar el panel.', 'error');
                        return;
                    }

                    var datos = resultado.datos;
                    asignarTexto('indicador-confirmadas', datos.indicadores.matriculasConfirmadas);
                    asignarTexto('indicador-proceso', datos.indicadores.matriculasEnProceso);
                    asignarTexto('indicador-creditos', datos.indicadores.creditosTotales);
                    asignarTexto('indicador-ingreso', formatearColones(datos.indicadores.ingresoProyectado));
                    asignarTexto('indicador-promedio', datos.indicadores.promedioCreditos);
                    asignarTexto('indicador-grupos', datos.indicadores.gruposAbiertos);
                    asignarTexto('sello-actualizacion',
                        'Actualizado a las ' + new Date().toLocaleTimeString('es-CR'));

                    dibujarBarras('grafico-carreras', datos.matriculaPorCarrera);
                    dibujarOcupacion('grafico-ocupacion', datos.ocupacion);
                })
                .catch(function (error) {
                    contenedor.classList.remove('cargando');
                    mostrarAviso(error.message, 'error');
                });
        }

        if (boton) { boton.addEventListener('click', refrescar); }
        if (selector) { selector.addEventListener('change', refrescar); }
    }

    /**
     * Dibuja un gráfico de barras horizontales en SVG. Se genera aquí en lugar de usar una
     * biblioteca externa para que la página no dependa de recursos de terceros.
     */
    function dibujarBarras(id, series) {
        var destino = document.getElementById(id);
        if (!destino) { return; }

        if (!series || series.length === 0) {
            destino.innerHTML = '<p class="tenue diminuto sin-margen">Sin datos para el periodo.</p>';
            return;
        }

        var maximo = Math.max.apply(null, series.map(function (s) { return s.valor; })) || 1;
        var altoFila = 30;
        var alto = series.length * altoFila + 10;
        var anchoEtiqueta = 190;

        var partes = ['<svg class="grafico" viewBox="0 0 620 ' + alto + '" role="img">'];

        series.forEach(function (serie, indice) {
            var y = indice * altoFila + 6;
            var ancho = Math.max(2, Math.round((serie.valor / maximo) * (600 - anchoEtiqueta - 46)));

            partes.push('<text class="rotulo-eje" x="0" y="' + (y + 13) + '">' +
                escapar(recortar(serie.etiqueta, 30)) + '</text>');
            partes.push('<rect class="barra" x="' + anchoEtiqueta + '" y="' + y + '" width="' +
                ancho + '" height="16"></rect>');
            partes.push('<text class="valor" x="' + (anchoEtiqueta + ancho + 8) + '" y="' +
                (y + 13) + '">' + serie.valor + '</text>');
        });

        partes.push('</svg>');
        destino.innerHTML = partes.join('');
    }

    function dibujarOcupacion(id, grupos) {
        var destino = document.getElementById(id);
        if (!destino) { return; }

        if (!grupos || grupos.length === 0) {
            destino.innerHTML = '<p class="tenue diminuto sin-margen">Sin grupos en el periodo.</p>';
            return;
        }

        var filas = grupos.map(function (grupo) {
            var lleno = grupo.porcentajeOcupacion >= 100 ? ' ocupacion__relleno--lleno'
                : grupo.porcentajeOcupacion <= 50 ? ' ocupacion__relleno--holgado' : '';

            return '<tr><td><span class="celda-principal">' + escapar(grupo.etiqueta) + '</span>' +
                '<span class="celda-secundaria">' + escapar(grupo.nombreCurso) + '</span></td>' +
                '<td><span class="ocupacion"><span class="ocupacion__pista">' +
                '<span class="ocupacion__relleno' + lleno + '" style="width:' +
                Math.min(100, grupo.porcentajeOcupacion) + '%"></span></span>' +
                '<span class="ocupacion__texto">' + grupo.inscritos + ' de ' + grupo.cupoMaximo +
                '</span></span></td>' +
                '<td class="derecha numero">' + grupo.porcentajeOcupacion + ' %</td></tr>';
        });

        destino.innerHTML =
            '<div class="tabla-envoltura"><table class="tabla"><thead><tr>' +
            '<th>Grupo</th><th>Ocupación</th><th class="derecha">Porcentaje</th>' +
            '</tr></thead><tbody>' + filas.join('') + '</tbody></table></div>';
    }

    function recortar(texto, largo) {
        return texto && texto.length > largo ? texto.substring(0, largo - 1) + '…' : texto;
    }

    // =================================================================================
    //  Arranque
    // =================================================================================

    document.addEventListener('DOMContentLoaded', function () {
        iniciarCatalogo();
        enlazarBotonesMatricula();
        iniciarPanel();

        // Los formularios con acciones irreversibles muestran su propia página de confirmación,
        // así que aquí solo se evita el doble envío por doble clic.
        document.querySelectorAll('form[data-evitar-doble-envio]').forEach(function (formulario) {
            formulario.addEventListener('submit', function () {
                var boton = formulario.querySelector('button[type="submit"]');
                if (boton) {
                    setTimeout(function () { boton.disabled = true; }, 0);
                }
            });
        });
    });
})();
