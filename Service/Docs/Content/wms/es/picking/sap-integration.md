---
title: Integración SAP de Picking
description: Cómo se generan y sincronizan los pickings con SAP.
---

EZY WMS se integra directamente con SAP Business One para operaciones de picking. Los pickings se generan en SAP y se sincronizan automáticamente al WMS.

## Generar un Picking desde una Orden de Venta

1. En SAP, abra la orden de venta.
2. Copie el número de la orden de venta.
3. Navegue a **Inventario → Efectuar picking y embalar → Gerente de Picking**.
4. Pegue el número de orden tanto en el campo **De** como en **Hasta**.
5. Haga clic en **OK**.
6. Haga doble clic en los cuadros para seleccionar todas las líneas.
7. Verifique que haya suficiente inventario disponible para cada artículo.
8. Copie la fecha de entrega desde la orden de venta original.
9. Haga clic en **Generar**.
10. Continúe por el asistente, pegando la fecha de entrega cuando se solicite.
11. Haga clic en **Generar** para finalizar.

El picking ahora aparece automáticamente en el WMS como pendiente.

## Sincronización en Tiempo Real

A medida que se separan artículos en el WMS:

- SAP se actualiza en tiempo real.
- El Departamento de Tráfico ve el progreso actual del picking.
- Los pickings completados desaparecen del WMS y se marcan como completos en SAP.

## Entrega Manual

Una vez que la bodega completa el picking:

1. El Departamento de Tráfico regresa a SAP.
2. Desde la orden de venta o lista de picking, realice una **entrega manual**.
3. Esto registra la salida del inventario.

⚠️ **Nota:** Este paso es obligatorio para la salida de inventario pero no afecta la facturación ni la contabilidad.

## Páginas Relacionadas

- [Proceso de Picking](/docs/picking/process)
- [Supervisor de Picking](/docs/picking/supervisor)
