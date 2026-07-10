---
title: Proceso de Entrada
description: Escanear y recibir mercancía entrante.
---

La página de Proceso de Entrada es donde los operarios escanean y registran la mercancía entrante.

## Acceso a la Página

Ruta: `/goodsReceipt/:scanCode`

Navegación: Panel Principal → Entrada de Mercancía → Seleccionar Entrada

## Cómo Usar

1. Desde la lista de Entradas, haga clic en el número de entrada que desea procesar.
2. Escanee el **código de barra** de cada artículo que está recibiendo.
3. El sistema muestra la información del artículo y solicita confirmación de cantidad.

## Escaneo de Artículos

Después de escanear un código:

1. El sistema muestra el nombre, código y cantidad abierta del artículo.
2. Confirme o modifique la **cantidad escaneada**.
3. Seleccione el **tipo de unidad**:
   - Caja
   - Docena
   - Unidad
4. El sistema selecciona **Caja** por defecto, pero puede cambiarlo.
5. Haga clic en **Aceptar** para registrar el artículo.

## Gestión de Paquetes

Durante la recepción, puede asociar artículos con paquetes:

- **Modo Escanear Paquete** — escanee un paquete primero, luego escanee artículos en él.
- **Modo Escanear Artículo** — escanee artículos individualmente sin paquete.
- **Crear Nuevo Paquete** — genere un nuevo paquete para los artículos actuales.
- **Limpiar Paquete** — desasocie el paquete actual.

## Opciones de Destino

Al escanear artículos, el sistema puede solicitar el destino:

| Opción | Descripción |
|--------|-------------|
| Almacenar en Bodega | Colocar en almacenamiento regular |
| Separar para Entrega Cliente | Reservar para una orden específica |
| Enviar a Showroom | Mover a ubicación de showroom |
| Separar para Supervisor | Retener para revisión del supervisor |

## Comentarios

Puede agregar comentarios a cualquier línea:

1. Haga clic en **Agregar Comentario**.
2. Ingrese su observación (ej. empaque dañado, discrepancia de cantidad).
3. Guarde.

## Modificar Cantidades

Si necesita cambiar una cantidad después de escanear:

1. Encuentre el artículo en la lista.
2. Haga clic en **Editar Cantidad**.
3. Ingrese la cantidad corregida.
4. Guarde.

## Mensajes de Error

| Mensaje | Causa |
|---------|-------|
| Entrada no válida | El número escaneado no existe |
| Entrada cerrada | La entrada ya ha sido completada |
| Artículo no encontrado en documentos | El artículo escaneado no está en el documento fuente |
| Cantidad excede disponible | La cantidad escaneada es mayor que la cantidad abierta |
| Código de barra no encontrado | El código escaneado no está registrado |

## Páginas Relacionadas

- [Supervisor de Entradas](/docs/goods-receipt/supervisor)
- [Reportes](/docs/goods-receipt/reports)
