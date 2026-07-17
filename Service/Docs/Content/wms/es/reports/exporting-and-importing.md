---
title: Exportar e Importar Informes
description: Mover definiciones de informe entre instalaciones de WMS como un paquete JSON portátil. Solo Super Usuario.
---

Las definiciones de informe se pueden exportar a un archivo JSON e importar en otra instalación de WMS. Esto le permite crear y probar informes contra una base de datos de staging, luego moverlos a producción, o compartir un conjunto de informes entre instalaciones de clientes.

## Acceso a la Página

Ruta: `/settings/reports`

Navegación: Menú lateral → **Definiciones de Informes**

Solo los **Super Usuarios** pueden exportar o importar definiciones de informe.

## Exportar

Desde la lista de definiciones:

- **Exportar todo** (en la parte superior) — descarga todas las definiciones como un solo archivo llamado `ezy-wms-reports.json`.
- **Exportar** en una fila — descarga solo ese informe como un archivo con el nombre de su slug.

Una exportación contiene solo los campos creados — el nombre, slug, SQL, columnas, variables y ajustes de paginación. Omite deliberadamente el ID interno del informe, las marcas de tiempo, el autor y las concesiones de Grupos de Autorización, porque son específicos de la instalación que los creó.

## Importar

1. Haga clic en **Importar** para abrir el diálogo de importación.
2. Elija un archivo JSON — ya sea un paquete completo o un arreglo simple exportado antes.
3. El sistema ejecuta de inmediato una **simulación** que compila el SQL de cada informe contra la base de datos de SAP de **esta** instalación.

La vista previa lista cada informe con dos badges:

| Badge | Significado |
|-------|-------------|
| **Nuevo** / **Reemplaza existente** | Si el slug del informe ya existe en esta instalación. Un reemplazo conserva el ID del informe existente, de modo que sus concesiones de Grupos de Autorización sobreviven. |
| **Listo** / **No válido aquí** | Si el SQL compiló y devolvió columnas en esta base de datos, con el mensaje textual de la base de datos cuando no fue así. |

> **Advertencia:** La importación es **todo o nada**. Un solo informe inválido bloquea todo el paquete — no se escribe nada hasta que todos los informes sean válidos en este sistema. Esto evita dejar un conjunto importado a medias.

4. Cuando todos los informes estén **Listos**, haga clic en **Importar** para aplicar. La lista se actualiza con los informes importados.

> **Sugerencia:** La simulación verifica el SQL contra el SAP de esta instalación, que puede diferir del origen (base de datos de empresa distinta, códigos de artículo distintos). Lea siempre la vista previa antes de aplicar — un informe válido en staging puede ser **No válido aquí** en un cliente cuyo esquema difiere.

## Páginas Relacionadas

- [Crear una Definición de Informe](/docs/reports/authoring-a-report-definition)
- [Resumen de Informes](/docs/reports)
