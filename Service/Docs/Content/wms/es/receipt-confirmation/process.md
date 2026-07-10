---
title: Proceso de Confirmación de Entrada
description: Escanear y confirmar mercancía recibida.
---

La página de Proceso de Confirmación de Entrada funciona idénticamente al Proceso de Entrada de Mercancía, pero se utiliza para validar mercancía ya recibida.

## Acceso a la Página

Ruta: `/goodsReceiptConfirmation/:scanCode`

Navegación: Panel Principal → Confirmación de Entrada → Seleccionar Entrada

## Cómo Usar

1. Desde la lista de Confirmaciones, haga clic en el número de entrada.
2. Escanee el **código de barra** de cada artículo a confirmar.
3. Confirme o modifique la **cantidad**.
4. Seleccione el **tipo de unidad**.
5. Haga clic en **Aceptar**.

## Características

Todas las características del Proceso de Entrada aplican:

- Gestión de paquetes (modo escanear paquete, modo escanear artículo)
- Opciones de destino (bodega, entrega, showroom, supervisor)
- Comentarios en líneas
- Edición de cantidades

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Entrada no válida | El número de confirmación no existe |
| Entrada cerrada | La confirmación ya ha sido completada |
| Artículo no encontrado en documentos | El artículo no está en el documento fuente |

## Páginas Relacionadas

- [Supervisor de Confirmación de Entrada](/docs/receipt-confirmation/supervisor)
- [Proceso de Entrada de Mercancía](/docs/goods-receipt/process)
