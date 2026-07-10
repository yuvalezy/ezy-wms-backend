---
title: Proceso de Transferencia
description: Ejecutar transferencias de inventario entre ubicaciones.
---

La página de Proceso de Transferencia guía a los operarios en el movimiento de mercancía desde ubicaciones de origen a ubicaciones de destino.

## Acceso a la Página

Ruta: `/transfer/:scanCode/*`

Navegación: Panel Principal → Transferencia → Seleccionar Número de Transferencia

## Cómo Usar

### Paso 1: Escanear Ubicación de Origen

1. Seleccione el número de transferencia de la lista.
2. Escanee el **código de barra de la ubicación de origen**.
3. Esta es la ubicación desde donde retirará la mercancía.

### Paso 2: Escanear Artículos de Origen

1. Escanee el **código de barra del artículo** de cada producto retirado.
2. Puede escanear varios artículos siempre que provengan de la misma ubicación de origen escaneada.
3. Confirme o modifique la **cantidad**.
4. Seleccione el **tipo de unidad**.
5. Haga clic en **Aceptar**.

### Paso 3: Seleccionar Ubicaciones de Destino

1. Una vez finalizada la recolección de la ubicación de origen, haga clic en **Elegir Ubicaciones de Destino**.
2. Escanee o seleccione la **ubicación de destino**.
3. Aquí es donde se colocará la mercancía.

### Paso 4: Escanear Artículos en Destino

1. Escanee los **artículos nuevamente** que se colocarán en la ubicación de destino.
2. Este paso verifica que toda la mercancía retirada se esté colocando correctamente.
3. Confirme en pantalla que los artículos escaneados corresponden a los retirados.

### Continuar la Transferencia

Para continuar con una nueva ubicación de origen o destino:

- Haga clic en **Transferencia** nuevamente.
- Escanee una nueva ubicación de origen, o
- Seleccione una nueva ubicación de destino según el caso.

Puede mover mercancía desde una ubicación de origen hacia diferentes destinos, o desde varios orígenes a un mismo destino.

## Transferencia entre Almacenes

Si la transferencia es entre almacenes:

1. Seleccione el **almacén destino**.
2. El sistema valida que sea diferente del almacén origen.
3. Complete la transferencia normalmente.
4. La transferencia se envía para aprobación.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Transferencia no válida | El número de transferencia no existe |
| Transferencia cerrada | La transferencia ya ha sido completada |
| Artículo no encontrado en origen | El artículo no está en los documentos de origen |
| Cantidad mayor que disponible | La cantidad excede el stock disponible |
| Paquete ya agregado como origen | El paquete ya es parte de esta transferencia |

## Páginas Relacionadas

- [Supervisor de Transferencia](/docs/transfer/supervisor)
- [Aprobación de Transferencia](/docs/transfer/approval)
