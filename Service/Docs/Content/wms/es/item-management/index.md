---
title: Gestión de Artículos
description: Resumen de operaciones de gestión de artículos.
---

El módulo de Gestión de Artículos proporciona herramientas para consultar y verificar datos de inventario en tiempo real. Incluye tres operaciones de revisión: Revisión de Artículo, Revisión de Ubicación y Revisión de Paquete.

## Operaciones

| Operación | Descripción | Ruta |
|-----------|-------------|------|
| [Revisión de Artículo](/docs/item-management/item-check) | Consultar información de stock por código o barcode | `/itemCheck` |
| [Revisión de Ubicación](/docs/item-management/bin-check) | Consultar todos los artículos en una ubicación | `/binCheck` |
| [Revisión de Paquete](/docs/item-management/package-check) | Consultar contenido y ubicación de paquetes | `/packageCheck` |

## Autorización

El acceso a Gestión de Artículos requiere uno de los siguientes roles:

- Supervisor de Entradas
- Supervisor de Picking
- Supervisor de Conteo
- Supervisor de Transferencia
- Gestión de Paquetes
- Supervisor de Gestión de Paquetes

## Características Comunes

Todas las operaciones de revisión admiten:

- **Escaneo de códigos de barra** — escanee códigos para llenar campos automáticamente.
- **Datos de stock en tiempo real** — vea cantidades actuales en todas las ubicaciones.
- **Cambio de unidad** — muestre cantidades en unidades, docenas o cajas.
- **Visualización de metadatos** — vea y edite campos de metadatos personalizados (si están configurados).

## Páginas Relacionadas

- [Revisión de Artículo](/docs/item-management/item-check)
- [Revisión de Ubicación](/docs/item-management/bin-check)
- [Revisión de Paquete](/docs/item-management/package-check)
