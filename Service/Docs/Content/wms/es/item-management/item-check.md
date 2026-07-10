---
title: Revisión de Artículo
description: Consultar stock y detalles de artículos por código o barcode.
---

La Revisión de Artículo permite consultar información detallada de stock de cualquier artículo escaneando su código de barra o ingresando su código.

![Formulario vacío de Revisión de Artículo](/docs/wms/item-management/item-check-empty-es.png)

## Acceso a la Página

Ruta: `/itemCheck`

Navegación: Panel Principal → Revisión de Artículo

También puede acceder directamente con un parámetro de código: `/itemCheck/:code`

## Cómo Usar

1. Ingrese o escanee el **código de artículo** o **código de barra**.
2. El sistema busca el artículo y muestra:
   - Nombre y descripción del artículo
   - Todos los códigos de barra asociados
   - Cantidades de stock por ubicación
   - Desglose por tipo de unidad (cajas, docenas, unidades)
3. Si hay múltiples coincidencias, el sistema muestra un error y solicita corrección.

![Búsqueda con código ingresado](/docs/wms/item-management/item-check-search-es.png)

## Visualización de Resultados

Después de ingresar un código válido y presionar **Aceptar**, el sistema muestra los detalles del artículo con tres pestañas: **Inventario**, **Códigos de Barra** y **Metadatos**.

![Resultados de Revisión de Artículo](/docs/wms/item-management/item-check-results-es.png)

### Pestaña Inventario

Muestra las cantidades de stock por ubicación con enlaces a los detalles de la ubicación.

![Pestaña de inventario](/docs/wms/item-management/item-check-stock-tab-es.png)

| Campo | Descripción |
|-------|-------------|
| Ubicación | Ubicación de almacén donde se encuentra el stock (enlace a Revisión de Ubicación) |
| Cantidad | Cantidad disponible en la ubicación con unidad de medida |
| Total Ubicaciones | Número de ubicaciones distintas que contienen el artículo |
| Total Cajas | Total de cajas en todas las ubicaciones |

#### Indicadores de Unidades (C / D / U)

Cada fila de stock muestra tres insignias de color que desglosan la cantidad total en niveles jerárquicos:

| Insignia | Color | Etiqueta | Descripción |
|----------|-------|----------|-------------|
| **C** | Azul | Caja | Número de cajas completas (unidades de envío) |
| **D** | Verde | Docena | Número de grupos de docena restantes después de las cajas |
| **U** | Ámbar | Unidad | Unidades individuales restantes después de las docenas |

Una insignia gris significa que el conteo para ese nivel es cero.

El desglose se calcula como:
- **Cajas** = `cantidad total / (unidades por compra × unidades de paquete de compra)`
- **Docenas** = `restante / unidades por compra`
- **Unidades** = piezas individuales sobrantes

:::note
La insignia **U** (Unidad) solo se muestra cuando la configuración **Habilitar Unidades Base** está activa para su usuario.
:::

### Pestaña Códigos de Barra

Muestra todos los códigos de barra registrados para el artículo.

![Pestaña de códigos de barra](/docs/wms/item-management/item-check-barcodes-tab-es.png)

Los artículos pueden tener múltiples códigos de barra. El sistema muestra todos los códigos registrados y permite validar si un código escaneado pertenece al artículo.

#### Agregar un Código de Barra

Ingrese un nuevo código en el campo **Nuevo Código** y haga clic en **Actualizar**.

![Agregar un nuevo código de barra](/docs/wms/item-management/item-check-add-barcode-es.png)

#### Eliminar un Código de Barra

Marque la casilla junto a un código existente y haga clic en **Actualizar** para eliminarlo.

![Eliminar un código de barra](/docs/wms/item-management/item-check-remove-barcode-es.png)

### Pestaña Metadatos

Muestra los campos de metadatos personalizados configurados para artículos. Haga clic en **Editar metadatos** para modificar valores.

![Pestaña de metadatos](/docs/wms/item-management/item-check-metadata-tab-es.png)

#### Editar Metadatos

Haga clic en **Editar metadatos** para abrir el diálogo de edición. Los campos de metadatos soportan los siguientes tipos:

| Tipo | Descripción |
|------|-------------|
| Texto | Valor de texto libre |
| Número | Valor numérico decimal |
| Entero | Valor numérico entero |
| Fecha | Selector de fecha |

![Diálogo de edición de metadatos](/docs/wms/item-management/item-check-edit-metadata-es.png)

:::note
El botón Editar solo es visible para usuarios con el rol **Gestión de Artículos** o permisos de **Super Usuario**, y solo cuando al menos un campo de metadatos no es de solo lectura.
:::

Los campos calculados se computan automáticamente a partir de otros valores de metadatos. Editar manualmente un campo calculado borrará todos los campos calculados dependientes.

## Mensajes de Error

Si un código no se encuentra, el sistema muestra una notificación de error.

![Estado de error](/docs/wms/item-management/item-check-error-es.png)

| Mensaje | Causa |
|---------|-------|
| Código no encontrado | El código ingresado no existe |
| Código de barra no encontrado | El código escaneado no está registrado |
| Múltiples artículos detectados | El código devuelve múltiples códigos de artículo |
| Artículo no habilitado en almacén | El artículo no está activo en el almacén actual |
| No es artículo de inventario | El artículo no es gestionado por inventario |

## Páginas Relacionadas

- [Revisión de Ubicación](/docs/item-management/bin-check)
