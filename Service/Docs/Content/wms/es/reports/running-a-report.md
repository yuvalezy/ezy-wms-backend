---
title: Ejecutar un Informe
description: Filtrar, ordenar, paginar, exportar y profundizar en los resultados de un informe.
---

El ejecutor de informes es donde trabaja con los resultados de un informe. Usted define los filtros, ejecuta la consulta y lee las filas que devuelve SAP. Ordene por cualquier columna, navegue por los resultados, exporte a Excel o siga el enlace de una celda hacia otra página o un segundo informe.

## Acceso a la Página

Ruta: `/reports/:slug`

Navegación: Menú lateral → el nombre del informe

Cualquier usuario autenticado puede abrir un informe compartido con su Grupo de Autorización. Si un informe no se ha compartido con usted, no aparece en su menú.

## Ejecutar el Informe

1. Abra el informe desde el menú lateral.
2. Defina los filtros que necesite (vea **Barra de Filtros** más abajo).
3. Haga clic en **Ejecutar**.

La página se ejecuta automáticamente al cargar cuando todos los filtros requeridos ya tienen un valor — proveniente de su valor predeterminado o de los filtros enviados por un enlace de profundización. Si un filtro requerido está vacío, la página espera a que lo complete y haga clic en **Ejecutar**.

Mientras se ejecuta, el botón muestra **Ejecutando...**. Al terminar, el tiempo transcurrido se muestra al final ("Ejecutado en N ms").

## Barra de Filtros

La barra de filtros muestra un control por cada filtro declarado por el autor del informe. Cada filtro tiene su etiqueta y los obligatorios se marcan con `*`.

| Tipo de filtro | Control |
|----------------|---------|
| Texto / número entero / decimal | Una entrada de texto o número |
| Fecha | Un selector de fecha de calendario |
| Fecha y hora | Una entrada de fecha y hora |
| Rango de fechas | Dos selectores — **Desde** y **Hasta** |
| Sí/No (bit) o Sí/No (S/N) | Una lista desplegable Sí/No/**Cualquiera** |
| Lista desplegable (fija) | Una lista desplegable con búsqueda, o una ventana de selección múltiple cuando **Permitir múltiples** está activo |
| Lista desplegable (consulta SQL) | Una lista desplegable con búsqueda cuyas opciones provienen de una consulta en vivo — por ejemplo, la lista de almacenes. Estas se actualizan cuando cambia otro filtro, de modo que un filtro puede limitar el siguiente (almacén → ubicaciones). |

Haga clic en **Restablecer filtros** para devolver cada filtro a los valores con los que se abrió la página. Restablecer no borra los valores de un enlace de profundización hacia los valores predeterminados del informe — restaura exactamente lo que el enlace solicitó.

> **Nota:** Los filtros requeridos vacíos, y los rangos de fechas donde **Desde** es posterior a **Hasta**, bloquean la ejecución con un mensaje de error.

## Tabla de Resultados

Las filas aparecen en una tabla ordenable. Haga clic en cualquier encabezado de columna marcado como ordenable para volver a ejecutar desde la primera página en ese orden — la flecha de orden solo se mueve una vez que el servidor confirma el criterio.

| Comportamiento | Detalle |
|----------------|---------|
| Formato de celda | Sigue el formato de la columna — números, moneda, porcentajes, fechas y horas se muestran en consecuencia. Las celdas vacías (NULL) muestran un guion largo (—). |
| Valores de fecha y hora | Se muestran en UTC, tal como los almacena SAP. |
| Enlaces de columna | Una celda puede ser un hiperenlace. Vea **Enlaces de Profundización** más abajo. |

## Paginación

No hay un conteo total de filas por defecto — el informe obtiene una página a la vez y le indica si hay más filas. Use **Anterior** y **Siguiente** para moverse entre páginas. Una etiqueta muestra el rango de filas de la página actual ("Filas X–Y").

Si el autor del informe habilitó el conteo total **Exacto**, aparece un botón **Mostrar total** que calcula el conteo completo bajo demanda y luego muestra "N filas en total".

## Exportar a Excel

Haga clic en **Exportar a Excel** en el encabezado de la página para descargar todas las filas coincidentes como un archivo `.xlsx` con el nombre del informe.

- Las filas se obtienen en segundo plano en bloques, respetando sus filtros y orden actuales.
- Las columnas numéricas se escriben como números reales para que Excel pueda sumarlas y ordenarlas.
- Las exportaciones están limitadas a 50.000 filas. Si se alcanza el límite, una advertencia le indica que la exportación fue truncada.

## Enlaces de Profundización

Una columna puede ser un enlace. El autor decide a dónde lleva cada enlace:

- Una **página interna** abre otra pantalla de WMS sin recargar — por ejemplo, un código de artículo que abre **Revisión de Artículo**.
- Un **sitio externo** se abre en una pestaña nueva — por ejemplo, una búsqueda web usando el valor de una columna.
- Un enlace a **otro informe** transporta los valores de la fila actual como filtros, de modo que el informe de destino se abre ya filtrado. Por ejemplo, una cantidad *Disponible* puede enlazar a un informe *Stock por Ubicación* para el mismo artículo y almacén.

Si el valor de la fila detrás de un enlace está vacío, el enlace se oculta y la celda muestra texto sin formato.

> **Advertencia:** Cuando un enlace de profundización solicita filtros que el informe de destino no puede aplicar — un parámetro desconocido, un valor inválido o un duplicado — un banner ámbar lista los problemas y el informe **no** se ejecuta automáticamente. Esto evita que un enlace muestre silenciosamente un conjunto de resultados más amplio de lo prometido.

## Páginas Relacionadas

- [Resumen de Informes](/docs/reports)
- [Crear una Definición de Informe](/docs/reports/authoring-a-report-definition)
- [Grupos de Autorización](/docs/settings/authorization-groups)
