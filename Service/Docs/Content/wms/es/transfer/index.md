---
title: Transferencia
description: Resumen del módulo de transferencia de inventario.
---

El módulo de Transferencia se utiliza para mover mercancía de una ubicación a otra dentro de la bodega. Es una herramienta clave para reorganizar el inventario y optimizar la distribución del almacén.

## Flujo de Trabajo

1. El **supervisor** crea una nueva entrada de transferencia con un nombre interno y comentario opcional.
2. Los **operarios** acceden a la transferencia y escanean ubicaciones de origen y artículos.
3. Los operarios seleccionan ubicaciones de destino y escanean los artículos nuevamente para confirmar su colocación.
4. Se pueden usar múltiples ubicaciones de origen y destino dentro de la misma transferencia.
5. El **supervisor** finaliza la transferencia para actualizar el inventario en SAP y WMS.

## Páginas

| Página | Ruta | Descripción |
|--------|------|-------------|
| [Lista de Transferencias](/docs/transfer/process) | `/transfer` | Lista de transferencias pendientes |
| [Proceso de Transferencia](/docs/transfer/process) | `/transfer/:scanCode/*` | Ejecutar la transferencia |
| [Supervisor](/docs/transfer/supervisor) | `/transferSupervisor` | Crear y monitorear transferencias |
| [Reporte Transferencia](/docs/transfer/summary-report) | `/transferSummaryReport/:id` | Revisar qué movió una transferencia |
| [Aprobación](/docs/transfer/approval) | `/transfer/approve` | Aprobar transferencias entre almacenes |
| [Solicitud de Transferencia](/docs/transfer/transfer-request) | `/transferRequest` | Crear solicitudes de transferencia |

## Pasos del Proceso de Transferencia

El proceso de transferencia tiene tres subpasos:

1. **Origen** — escanee la ubicación de recogida y los artículos a mover.
2. **Ubicaciones de Destino** — seleccione las ubicaciones de destino.
3. **Artículos de Destino** — escanee los artículos nuevamente en el destino para confirmar.

## Transferencias entre Almacenes

Las transferencias entre diferentes almacenes requieren aprobación del supervisor:

1. Seleccione el **almacén destino**.
2. El sistema marca la transferencia como "Esperando Aprobación".
3. Un supervisor con permisos de aprobación debe aprobar o rechazar la transferencia.

## Roles

| Rol | Permisos |
|-----|----------|
| Transferencia | Ejecutar transferencias |
| Supervisor de Transferencia | Crear transferencias, finalizar, ver reportes |
| Solicitud de Transferencia | Crear solicitudes de transferencia |
| Aprobación de Transferencia | Aprobar o rechazar transferencias entre almacenes |

## Páginas Relacionadas

- [Proceso de Transferencia](/docs/transfer/process)
- [Supervisor de Transferencia](/docs/transfer/supervisor)
- [Reporte Transferencia](/docs/transfer/summary-report)
