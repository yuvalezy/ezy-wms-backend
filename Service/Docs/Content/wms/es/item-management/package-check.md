---
title: Revisión de Paquete
description: Consultar contenido, ubicación y metadatos de paquetes.
---

La Revisión de Paquete permite ver el contenido, ubicación actual y metadatos de cualquier paquete en la bodega.

## Acceso a la Página

Ruta: `/packageCheck`

Navegación: Panel Principal → Revisión de Paquete

También puede acceder directamente con parámetros: `/packageCheck/:id/:barcode`

## Cómo Usar

1. Escanee o ingrese el **código de barra del paquete**.
2. El sistema busca el paquete y muestra:
   - Estado del paquete (Inicializado, Activo, Cerrado, Cancelado, Bloqueado)
   - Ubicación y almacén actuales
   - Lista de todos los artículos dentro del paquete
   - Total de artículos únicos y cantidad total
   - Valores de metadatos (si están configurados)

## Estados del Paquete

| Estado | Descripción |
|--------|-------------|
| Inicializado | Paquete creado pero vacío |
| Activo | Paquete con artículos y en uso |
| Cerrado | Paquete sellado y completo |
| Cancelado | Paquete cancelado |
| Bloqueado | Paquete bloqueado temporalmente |

## Contenido del Paquete

La tabla de contenidos muestra:

| Campo | Descripción |
|-------|-------------|
| Código Artículo | Código del producto |
| Nombre Artículo | Descripción del producto |
| Cantidad | Cantidad en el paquete |
| Tipo de Unidad | Unidad de medida |
| Código Ubicación | Ubicación de almacenamiento |

## Metadatos

Si el paquete tiene campos de metadatos configurados, aparecen en una sección dedicada. Haga clic en **Editar Metadatos** para modificar valores.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Paquete no encontrado | El código no coincide con ningún paquete |
| No se encontró contenido | El paquete existe pero está vacío |

## Páginas Relacionadas

- [Revisión de Artículo](/docs/item-management/item-check)
- [Revisión de Ubicación](/docs/item-management/bin-check)
