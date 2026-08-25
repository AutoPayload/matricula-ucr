# Guía de publicación en GitHub y de los Pull Requests

> **Antes de nada.** La rama activa es `validacion-en-el-navegador` y es la única que contiene el
> proyecto completo. La rama `main` está a propósito en el primer commit, para que las seis
> restantes puedan convertirse en Pull Requests. **No ejecute `git checkout main` mientras
> trabaja**: el árbol de trabajo quedaría con solo el andamiaje y parecería que se perdieron los
> archivos. Vuelva con `git checkout validacion-en-el-navegador` si le ocurre.

Esta carpeta ya es un repositorio Git con el historial organizado en siete ramas temáticas, una
por capa del sistema. Falta publicarlo y abrir los Pull Requests, que es la parte que depende de
su cuenta de GitHub.

---

## Situación actual del repositorio

    df92c55  Validación en el navegador con jQuery Validation      ← validacion-en-el-navegador
    fea42b8  Pruebas automatizadas de las reglas de negocio        ← pruebas-automatizadas
    8c01be7  Portal docente y mantenimientos de la oficina         ← docencia-y-administracion
    1c018bd  Flujo de matrícula del estudiantado                   ← flujo-de-matricula
    6fae9cd  Identidad visual y seis componentes propios           ← identidad-visual
    f363aca  Seguridad: tres mecanismos de autenticación           ← seguridad
    9995ce2  Modelo de dominio y acceso a datos                    ← modelo-de-dominio
    f2cf64e  Andamiaje de la solución                              ← main

Cada rama es descendiente de la anterior. `main` está en el andamiaje, de modo que las seis ramas
restantes pueden convertirse en seis Pull Requests que se integran en orden.

**Nota sobre las fechas.** Los commits llevan la fecha real en que se organizó el historial. No se
alteraron las marcas de tiempo: la separación semanal entre Pull Requests que pide la rúbrica se
consigue abriéndolos e integrándolos con esa cadencia a partir de ahora, no reescribiendo el
pasado.

---

## Paso 1 — Crear el repositorio remoto

Con la interfaz web de GitHub: cree un repositorio vacío llamado `matricula-ucr`, **sin** README,
sin `.gitignore` y sin licencia, para que no haya conflicto con el historial local.

O con la herramienta de línea de comandos de GitHub:

    gh repo create matricula-ucr --private --source . --remote origin

Si lo creó por la web, enlace el remoto a mano:

    cd Proyecto
    git remote add origin https://github.com/SU-USUARIO/matricula-ucr.git

---

## Paso 2 — Publicar la rama principal

    git checkout main
    git push -u origin main

En GitHub, entre a **Settings → Branches** y proteja `main` marcando *Require a pull request
before merging*. Sin esa protección, la integración directa sigue siendo posible y el historial
de Pull Requests pierde sentido.

---

## Paso 3 — Publicar las ramas de trabajo

    git push -u origin modelo-de-dominio
    git push -u origin seguridad
    git push -u origin identidad-visual
    git push -u origin flujo-de-matricula
    git push -u origin docencia-y-administracion
    git push -u origin pruebas-automatizadas
    git push -u origin validacion-en-el-navegador

---

## Paso 4 — Abrir e integrar los Pull Requests

Uno por semana, en este orden. Cada uno debe integrarse antes de abrir el siguiente, porque las
ramas son sucesivas.

| Semana | Rama | Título sugerido |
|---|---|---|
| 1 | `modelo-de-dominio` | Modelo de dominio y acceso a datos |
| 2 | `seguridad` | Seguridad: autenticación por cookie, token y clave de aplicación |
| 3 | `identidad-visual` | Identidad visual y componentes propios |
| 4 | `flujo-de-matricula` | Flujo de matrícula del estudiantado |
| 5 | `docencia-y-administracion` | Portal docente y mantenimientos de registro |
| 6 | `pruebas-automatizadas` | Pruebas automatizadas de las reglas de negocio |
| 7 | `validacion-en-el-navegador` | Validación en el navegador |

Desde la línea de comandos:

    gh pr create --base main --head modelo-de-dominio \
      --title "Modelo de dominio y acceso a datos" \
      --body "Define las doce entidades del dominio, el contexto de EF Core y el sembrador de datos."

    # una semana después, tras revisarlo:
    gh pr merge modelo-de-dominio --merge

Repita con cada rama siguiente. Use `--merge` y no `--squash`: aplastar los commits borraría el
mensaje de cada uno, que es donde está explicado el porqué de cada decisión.

---

## Qué escribir en la descripción de cada Pull Request

Una descripción útil responde tres preguntas. Sirve de plantilla:

    ## Qué cambia
    Una o dos frases sobre el alcance.

    ## Por qué así
    La decisión de diseño que se tomó y la alternativa que se descartó.

    ## Cómo verificarlo
    Los pasos concretos para comprobar que funciona.

Por ejemplo, para el Pull Request de seguridad:

    ## Qué cambia
    Configura los tres mecanismos de autenticación del sistema y las políticas de autorización
    por rol.

    ## Por qué así
    Se registró el token JWT como esquema adicional en lugar de sustituir la cookie. El navegador
    sigue entrando con cookie, y la interfaz de programación queda utilizable desde una aplicación
    móvil sin cambiar el servidor.

    ## Cómo verificarlo
    1. `GET /api/matricula/resumen` sin token responde 401.
    2. `POST /api/autenticacion/token` con sesión iniciada devuelve un token válido por 60 minutos.
    3. Con rol de estudiante, `/Panel` redirige a la página de acceso denegado.

---

## Después de integrar todo

    git checkout main
    git pull
    git branch -d modelo-de-dominio seguridad identidad-visual flujo-de-matricula \
                  docencia-y-administracion pruebas-automatizadas validacion-en-el-navegador

---

## Sobre la coautoría declarada en los commits

Los mensajes de commit incluyen la línea `Co-Authored-By: Claude Opus 5`, que deja constancia de
que el desarrollo se hizo con asistencia de una herramienta de inteligencia artificial. Es
información honesta sobre cómo se produjo el código.

Si su curso pide declararlo de otra forma, o prefiere no incluirlo en el historial, puede quitar
esas líneas antes de publicar:

    git filter-branch --msg-filter 'grep -v "^Co-Authored-By: Claude"' -- main..HEAD

Hágalo antes del primer `push`. Reescribir un historial ya publicado obliga a forzar la escritura
y rompe cualquier copia que otra persona haya clonado.
