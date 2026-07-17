---
title: Informes
description: Resumen del módulo de Informes — ejecute informes SQL de solo lectura creados por administradores sobre sus datos de SAP.
---

El módulo de Informes le permite ejecutar informes en vivo y de solo lectura sobre sus datos de SAP Business One sin salir de WMS. Cada informe es una consulta SQL guardada que un administrador ha creado, probado y compartido con su equipo. Usted elige un informe del menú, define sus filtros y lee los resultados — nunca escribe SQL usted mismo.

Los informes no pertenecen a un solo flujo de bodega. Úselos para preguntas puntuales como *¿cuánto stock tengo por almacén?*, *¿qué ubicaciones contienen este artículo?* o *¿cuáles son las dimensiones por pieza y por caja?*

## Acceso a los Informes

Cada informe que se le ha concedido aparece como su propia entrada en el menú lateral, bajo un icono violeta de informe. El menú lista solo los informes compartidos con usted — si no ve ningún informe, pida a un Super Usuario que los conceda a través de su Grupo de Autorización (vea [Compartir Informes](/docs/settings/authorization-groups)).

También puede abrir la página de índice **Informes** para ver todos los informes disponibles como una cuadrícula de tarjetas.

Ruta: `/reports`

Navegación: Menú lateral → el nombre de un informe, o **Informes** para la lista completa

## Cómo Funciona el Acceso

Los informes usan un modelo de permisos distinto al resto de WMS:

- **Ejecutar un informe** — no existe un rol dedicado de "Informes". El acceso se concede **por informe** a través de su Grupo de Autorización. Una concesión surte efecto de inmediato, sin necesidad de iniciar sesión de nuevo, y el informe aparece en su menú al instante.
- **Crear un informe** — solo los **Super Usuarios** pueden crear, editar, probar, importar o eliminar definiciones de informe.

> **Nota:** Un informe solo aparece en su menú una vez que un administrador lo ha compartido con uno de sus Grupos de Autorización. Un WMS recién instalado no trae ningún informe — existen solo cuando un administrador los crea o importa.

## Qué ve en la Página de Índice

La página de índice **Informes** muestra una tarjeta por cada informe compartido con usted, con su nombre y descripción. Haga clic en una tarjeta para abrir el [ejecutor de informes](/docs/reports/running-a-report) y empezar a trabajar con él.

Si la cuadrícula está vacía, aún no se ha compartido ningún informe con su grupo.

## Páginas del Módulo

| Página | Descripción | Ruta |
|--------|-------------|------|
| [Ejecutar un Informe](/docs/reports/running-a-report) | Filtrar, ordenar, paginar y exportar un informe | `/reports/:slug` |
| [Crear una Definición de Informe](/docs/reports/authoring-a-report-definition) | Crear y editar definiciones de informe (Super Usuario) | `/settings/reports` |
| [Exportar e Importar Informes](/docs/reports/exporting-and-importing) | Mover definiciones de informe entre instalaciones (Super Usuario) | `/settings/reports` |

## Páginas Relacionadas

- [Ejecutar un Informe](/docs/reports/running-a-report)
- [Grupos de Autorización](/docs/settings/authorization-groups)
