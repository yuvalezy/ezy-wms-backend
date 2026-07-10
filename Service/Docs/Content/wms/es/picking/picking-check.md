---
title: Verificación de Picking
description: Verificar y validar artículos separados para precisión.
---

La página de Verificación de Picking permite a los supervisores o verificadores designados comprobar que los artículos separados coincidan con los requisitos de la orden antes del despacho.

## Acceso a la Página

Ruta: `/pick/:id/check`

Navegación: Supervisor de Picking → Seleccionar Picking → Iniciar Verificación

## Cómo Usar

1. Desde la página de Supervisor de Picking, encuentre el picking a verificar.
2. Haga clic en **Iniciar Verificación**.
3. La página de verificación muestra todos los artículos del picking.
4. Para cada artículo, verifique:
   - El código de artículo coincide con la orden.
   - La cantidad es correcta.
   - La ubicación es correcta.
   - Las asignaciones de paquete son correctas.
5. Marque los artículos como verificados o note discrepancias.

## Estado de Verificación

| Estado | Descripción |
|--------|-------------|
| No Verificado | El artículo no ha sido verificado aún |
| Verificado | El artículo ha sido verificado |
| Discrepancia | El artículo tiene discrepancia de cantidad o producto |

## Completar la Verificación

1. Revise todos los artículos.
2. Si se encuentran discrepancias, el sistema muestra una advertencia con el conteo.
3. Haga clic en **Completar Verificación** para finalizar.
4. El sistema registra los resultados y actualiza el estado del picking.

## Reiniciar una Verificación

Si es necesario, puede reiniciar una verificación:

1. Haga clic en **Reiniciar Verificación**.
2. Confirme la acción.
3. Todos los estados de verificación se reinician.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Verificación iniciada | La verificación ya ha sido iniciada |
| Verificación de picking completada | La verificación se completó exitosamente |
| Discrepancias encontradas | Hay discrepancias de cantidad o producto |

## Páginas Relacionadas

- [Proceso de Picking](/docs/picking/process)
- [Supervisor de Picking](/docs/picking/supervisor)
