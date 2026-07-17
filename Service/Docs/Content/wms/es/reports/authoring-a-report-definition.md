---
title: Crear una Definición de Informe
description: Crear, probar y editar el SQL y el diseño de una definición de informe. Solo Super Usuario.
---

Una definición de informe es la consulta y el diseño guardados detrás de cada informe. Como Super Usuario usted escribe el SQL, lo prueba contra su base de datos de SAP en vivo, declara sus columnas y filtros, y luego lo comparte con sus equipos a través de sus Grupos de Autorización. Los usuarios finales nunca ven el SQL — solo ejecutan el informe terminado.

## Acceso a la Página

Ruta: `/settings/reports` (lista), `/settings/reports/add` (nuevo), `/settings/reports/:id` (editar)

Navegación: Menú lateral → **Definiciones de Informes**

Solo los **Super Usuarios** pueden abrir las páginas de definiciones de informe.

## La Lista de Definiciones

La lista muestra todas las definiciones de informe — habilitadas o deshabilitadas — con su nombre, descripción y conteos de columnas y filtros. Cada fila tiene acciones: **Ejecutar** (abrir el informe), **Exportar** (descargar esta definición), **Editar** y **Eliminar**. En la parte superior hay botones de **Importar** y **Exportar todo** (vea [Exportar e Importar Informes](/docs/reports/exporting-and-importing)).

Haga clic en **+** (o **Añadir informe**) para crear una nueva definición. Haga clic en **Editar** en cualquier fila para abrir el editor.

## Cómo Funciona la Creación

Solo su base de datos de SAP puede validar de verdad el SQL de un informe, por lo que el editor no permite escribir las columnas a mano. En su lugar usted escribe el SQL, hace clic en **Probar consulta**, y el sistema compila la consulta dentro de una transacción que siempre se revierte — descubre las columnas exactas que su SQL devuelve y las fusiona en la tarjeta **Columnas**. Luego usted las etiqueta y formatea.

Guardar se bloquea hasta que la consulta se haya probado y haya descubierto al menos una columna.

## General

| Campo | Requerido | Notas |
|-------|-----------|-------|
| **Nombre** | Sí | Hasta 100 caracteres. Se muestra en el menú y como título del informe. |
| **Slug** | Sí | El identificador de URL — solo letras minúsculas, números y guiones (`stock-on-hand`). Se autocompleta desde el nombre al crear, pero nunca se reescribe al editar porque es una URL en vivo. |
| **Descripción** | No | Hasta 500 caracteres. Se muestra bajo el título del informe y en las tarjetas del índice. |
| **Habilitado** | No | Interruptor. Un informe deshabilitado permanece en la lista de definiciones pero se oculta de los usuarios finales y no se puede ejecutar. |
| **Añadir OPTION (RECOMPILE)** | No | Interruptor. Recomendado para el patrón `(@Var IS NULL OR col = @Var)`. Nunca escriba `OPTION (RECOMPILE)` a mano — use este interruptor. |

## SQL

La tarjeta **SQL** contiene el cuerpo de la consulta. Dos tokens son obligatorios y deben aparecer cada uno exactamente una vez:

- `{{orderBy}}` — se reemplaza por la columna por la que el usuario está ordenando.
- `{{paging}}` — se reemplaza por la cláusula de paginación `OFFSET … FETCH NEXT`.

Escriba los filtros como parámetros con el patrón `(@Name IS NULL OR col = @Name)` para que un filtro vacío devuelva todas las filas. Las listas desplegables de selección múltiple llegan como un solo parámetro CSV — divídalas con `STRING_SPLIT(@Name, ',')`.

> **Sugerencia:** Añada siempre un desempate único después de `{{orderBy}}` para que la paginación sea determinista — por ejemplo `ORDER BY {{orderBy}}, T0."DocEntry"`. Declare ese desempate como una columna oculta y no ordenable (vea **Columnas**).

Reglas que la consulta debe seguir:

- Debe empezar con `SELECT` o `WITH`.
- Debe contener `{{orderBy}}` y `{{paging}}` exactamente una vez cada uno (en minúsculas — `{{OrderBy}}` se rechaza).
- Debe ser una sola sentencia sin punto y coma (`;`) al final.

Haga clic en **Probar consulta** para validar el SQL y descubrir sus columnas. El resultado muestra "La consulta es válida" con el número de columnas descubiertas, o "La consulta falló" con el mensaje textual de la base de datos y cualquier advertencia.

## Paginación y Ordenamiento

| Campo | Notas |
|-------|-------|
| **Filas por página** | 1–200, por defecto 50. |
| **Tiempo de espera (segundos)** | 5–300, por defecto 60. Una ejecución que exceda esto se cancela. |
| **Columna de orden por defecto** | Cualquier columna ordenable, o "Sin orden por defecto". |
| **Dirección por defecto** | Ascendente o Descendente. |
| **Conteo total de filas** | **Ninguno (más rápido)** o **Exacto (bajo demanda)**. La paginación nunca necesita un total. Elija **Exacto** solo si los usuarios deben ver el conteo total, que luego solicitan bajo demanda con **Mostrar total**. |
| **Consulta de conteo personalizada** | Aparece solo para el modo **Exacto**. Una consulta `COUNT` opcional y más barata escrita a mano, usada por el botón de conteo total. |

## Columnas

Las columnas provienen de **Probar consulta** — no se pueden añadir a mano. Cada columna descubierta muestra el nombre exacto que devuelve el SQL (solo lectura) y le permite configurar cómo se muestra.

| Campo | Notas |
|-------|-------|
| **Etiqueta** | Texto del encabezado mostrado al usuario. |
| **Formato** | Texto, Número, Moneda, Porcentaje, Fecha, Fecha y hora, Hora, Booleano o Binario. |
| **Alineación** | Automática (derivada del formato), Izquierda, Centro o Derecha. |
| **Decimales** | 0–10, para formatos numéricos. |
| **Visible** | Interruptor. Desactívelo para columnas auxiliares que el SQL necesita pero el usuario no debe ver (por ejemplo, un desempate de paginación o un valor oculto usado solo por un enlace). |
| **Ordenable** | Interruptor. Se bloquea con una nota cuando el tipo SQL de la columna no se puede ordenar. Cada columna ordenable se valida al guardar, no solo la por defecto. |

Use los botones de subir y bajar para fijar el orden de visualización.

### Enlaces de Columna

Cada columna puede mostrarse opcionalmente como un hiperenlace. Establezca **Enlace** a uno de:

- **Sin enlace** — texto sin formato (por defecto).
- **Página interna** — una ruta de WMS navegada sin recargar. La plantilla debe empezar con una sola `/`, por ejemplo `/itemCheck/{ItemCode}`.
- **Sitio externo** — una URL absoluta `http`/`https` que se abre en una pestaña nueva, por ejemplo `https://www.google.com/search?q={WhsName}`.

El **Destino del enlace** es una plantilla con marcadores `{ColumnKey}` rellenados desde la fila actual. Para profundizar en otro informe, enlace a `/reports/su-slug?NombreVariable={ColumnKey}` — por ejemplo, `/reports/bin-stock?ItemCode={ItemCode}&WhsCode={WhsCode}`. Los nombres de los filtros del informe destino son el contrato; renombrar un filtro en el destino rompe el enlace silenciosamente.

> **Nota:** Si el marcador de un enlace resuelve a un valor vacío, el enlace se oculta y la celda muestra texto sin formato. Una plantilla sobrante en una columna **Sin enlace** se descarta al guardar, así que no tiene que vaciar la caja antes de desactivar un enlace.

## Variables

Las variables son los filtros que los usuarios finales completan. Haga clic en **Añadir variable** para declarar una. Cada una es una tarjeta:

| Campo | Notas |
|-------|-------|
| **Nombre** | El nombre del parámetro sin `@` — por ejemplo `FromDate`. Letras, números y guiones bajos; no debe empezar con `__` (reservado para paginación). Único, sin distinción de mayúsculas. |
| **Etiqueta** | El texto mostrado junto al filtro. |
| **Tipo** | Vea la tabla siguiente. |
| **Decimales** | Solo para el tipo Decimal. |
| **Requerido** | Interruptor. Un filtro requerido vacío bloquea la ejecución. |
| **Permitir múltiples** | Interruptor, solo para los dos tipos de lista desplegable. Muestra una selección múltiple; los valores se vinculan como un solo parámetro CSV. |
| **Valor predeterminado** | Se usa cuando el usuario no define el filtro y en la primera ejecución automática. Para un **Rango de fechas** está deshabilitado. |

### Tipos de Variable

| Tipo | Se vincula como | Notas |
|------|-----------------|-------|
| Texto | Texto | Filtro de cadena general. |
| Número entero | Entero | Mantiene una columna entera en su índice. |
| Número decimal | Decimal | Usa los **Decimales** de la variable. |
| Fecha | Fecha | Fecha ISO. |
| Fecha y hora | Fecha y hora | Fecha y hora ISO. |
| Rango de fechas | Dos fechas | Emite `@{Name}From` y `@{Name}To`. Desde debe ser igual o anterior a Hasta. |
| Sí/No (bit) | Bit | Solo columnas bit genuinas. |
| Sí/No (S/N) | Char(1) | Columnas de marca `Y`/`N` de SAP Business One. |
| Lista desplegable (fija) | El valor seleccionado | Una lista corta que usted define con **Opciones** (cada una tiene un **Valor** enviado a SQL y una **Etiqueta** mostrada al usuario). |
| Lista desplegable (consulta SQL) | El valor seleccionado | Una **Consulta de lookup** que debe devolver exactamente dos columnas llamadas `Value` y `Label`. Todas las variables del informe están disponibles como parámetros, de modo que un lookup puede depender de otro filtro (ubicaciones del almacén seleccionado, por ejemplo). Limitado a 1.000 filas. |

## Guardar

Haga clic en **Guardar** para crear o actualizar la definición. Guardar se bloquea si aún no hay columnas ("Ejecute **Probar consulta** primero") o si alguna variable tiene un error de validación.

Una vez guardado, comparta el informe con sus equipos desde la pantalla de [Grupos de Autorización](/docs/settings/authorization-groups) — un informe es invisible para los usuarios finales hasta que se concede a un grupo.

## Páginas Relacionadas

- [Ejecutar un Informe](/docs/reports/running-a-report)
- [Exportar e Importar Informes](/docs/reports/exporting-and-importing)
- [Grupos de Autorización](/docs/settings/authorization-groups)
