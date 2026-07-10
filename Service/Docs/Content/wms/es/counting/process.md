---
title: Proceso de Conteo
description: Escanear ubicaciones y contar artículos para inventario físico.
---

La página de Proceso de Conteo es donde los operarios realizan el conteo físico de inventario escaneando ubicaciones y códigos de barra de artículos.

## Acceso a la Página

Ruta: `/counting/:scanCode`

Navegación: Panel Principal → Conteo → Seleccionar Número de Conteo

## Cómo Usar

### Paso 1: Escanear la Ubicación

1. Ingrese el número de conteo o seleccione de la lista.
2. Escanee el **código de barra de la ubicación** (ej. rack o estante).
3. Verifique en pantalla que la ubicación escaneada coincide con la ubicación física.
4. Confirme antes de continuar.

### Paso 2: Escanear Artículos

1. Escanee el **código de barra del artículo**.
2. Verifique que la información del artículo coincida con lo físicamente presente.
3. Ingrese o confirme la **cantidad contada**.
4. Presione **Aceptar** para registrar el artículo en el conteo.

### Paso 3: Continuar Contando

- Siga escaneando más artículos en la misma ubicación.
- Para cambiar a otra ubicación dentro del mismo conteo:
  1. Haga clic en **Conteo** para salir de la ubicación actual.
  2. Vuelva a ingresar al mismo número de conteo.
  3. Escanee la nueva ubicación.
  4. Repita el proceso de escaneo de artículos.

## Múltiples Ubicaciones por Conteo

Puede contar varias ubicaciones dentro de una misma entrada de conteo, lo que permite realizar inventarios parciales o completos de diferentes zonas en una sola operación.

## Cantidad Contada vs Documento

El sistema muestra:

| Campo | Descripción |
|-------|-------------|
| Cantidad Documento | Cantidad esperada de SAP |
| Cantidad Contada | Cantidad físicamente contada |
| Diferencia | Variación entre esperado y contado |

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Conteo no válido | El número de conteo no existe |
| Conteo cerrado | El conteo ya ha sido completado |
| Ubicación no encontrada | La ubicación escaneada no existe |
| Artículo no en almacén | El artículo no está habilitado en el almacén actual |

## Páginas Relacionadas

- [Supervisor de Conteo](/docs/counting/supervisor)
- [Reporte Resumen](/docs/counting/summary-report)
