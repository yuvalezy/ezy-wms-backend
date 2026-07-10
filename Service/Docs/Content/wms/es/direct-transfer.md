---
title: Transferencia Directa
description: Transferencias rápidas de un solo artículo entre ubicaciones.
---

El módulo de Transferencia Directa permite a los operarios transferir rápidamente un solo artículo de una ubicación a otra sin crear una entrada de transferencia formal.

## Acceso a la Página

Ruta: `/directTransfer`

Navegación: Panel Principal → Transferencia Directa

## Cómo Usar

1. Escanee la **ubicación de origen**.
2. Escanee el **código de barra del artículo** a transferir.
3. Ingrese la **cantidad**.
4. Seleccione el **tipo de unidad**.
5. Escanee la **ubicación de destino**.
6. Haga clic en **Aceptar** para ejecutar la transferencia.

## Transferir Todo el Stock

Puede transferir todo el stock disponible de un artículo desde la ubicación de origen a la de destino seleccionando la opción **Transferir Todo el Stock**.

## Confirmación

Después de escanear la ubicación de destino, el sistema solicita confirmación antes de ejecutar la transferencia.

## Éxito

Una vez confirmada:

- La transferencia se ejecuta inmediatamente.
- El inventario se actualiza tanto en SAP como en WMS.
- Un mensaje de éxito confirma la operación.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Ubicación origen no encontrada | La ubicación de origen escaneada no existe |
| Ubicación destino no encontrada | La ubicación de destino escaneada no existe |
| Artículo no encontrado | El artículo escaneado no está en la ubicación de origen |
| Cantidad mayor que disponible | La cantidad excede el stock disponible |
| Ejecutando transferencia directa | El sistema está procesando la transferencia |

## Páginas Relacionadas

- [Transferencia](/docs/transfer/index)
- [Proceso de Transferencia](/docs/transfer/process)
