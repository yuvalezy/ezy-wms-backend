---
title: Configuración de Campos Personalizados
description: Configura campos personalizados para Artículos, DetallesPickings, Almacenes y Proveedores en EZY WMS.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

## Descripción General

Los Campos Personalizados permiten extender los modelos de datos estándar en EZY WMS mediante la asignación de columnas de bases de datos SAP y expresiones SQL calculadas a identificadores de campo. Estos campos están disponibles para mostrar y usar en diferentes módulos.

Plantilla de inicialización/migración:
```
ezy-wms-backend/Service/init/CustomFields.yaml
```

La sección `CustomFields` almacenada en la base de datos es la fuente en
ejecución. Edítela desde Configuración para superusuarios o
`ConfigurationController`; las actualizaciones se recargan en vivo después de
validarse.

## Estructura

La sección `CustomFields` es un diccionario indexado por nombre de colección. Cada colección contiene un array de definiciones de campos personalizados:

```yaml
CustomFields:
  Items:
    - Key: FieldId
      Description: Etiqueta de Pantalla
      Type: Text
      Query: EXPRESION_SQL
      GroupBy: EXPRESION_GRUPO_SQL  # opcional
  PickingDetails:
    # ...
```

## Propiedades de CustomField

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `Key` | string | Sí | Identificador único del campo dentro de la colección. |
| `Description` | string | Sí | Etiqueta legible mostrada en la interfaz de usuario. |
| `Type` | enum | Sí | Tipo de dato: `Text` (0), `Number` (1) o `Date` (2). |
| `Query` | string | Sí | Expresión SQL sin procesar que recupera el valor del campo. Se ignora en la serialización JSON. |
| `GroupBy` | string | No | Expresión SQL de agrupación utilizada en consultas agregadas. Se ignora en la serialización JSON. |

## Notas Importantes

- **Fragmentos SQL**: `Query` y `GroupBy` son fragmentos SQL sin procesar incrustados en consultas más grandes por el servicio. Deben ser válidos para el tipo de base de datos SAP que utilice (SQL Server, etc.).
- **Serialización JSON**: Las propiedades `Query` y `GroupBy` están marcadas con `[JsonIgnore]` y no se envían a los clientes.
- **Unicidad**: Cada `Key` debe ser único dentro de su colección.

## Ejemplos

### Colección Items

Define campos personalizados para operaciones relacionadas con artículos:

```yaml
CustomFields:
  Items:
    - Key: FrgnName
      Description: Nombre Secundario
      Type: Text
      Query: OITM.FrgnName
    
    - Key: ItmsGrpNam
      Description: Grupo de Artículos
      Type: Text
      Query: (SELECT [ItmsGrpNam] FROM OITB WHERE ItmsGrpCod = OITM.ItmsGrpCod)
      GroupBy: OITM.ItmsGrpCod
    
    - Key: UpdateDate
      Description: Fecha de Actualización
      Type: Date
      Query: OITM.UpdateDate
```

### Colección PickingDetails

Incluye un campo con agrupación SQL para operaciones agregadas:

```yaml
CustomFields:
  PickingDetails:
    - Key: Marca
      Description: Marca
      Type: Text
      Query: COALESCE(ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA)
      GroupBy: ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA
```

## Colecciones Adicionales (Opcional)

Puedes extender campos personalizados a otras colecciones. Descomenta y personaliza según sea necesario:

```yaml
CustomFields:
  # Warehouses:
  #   - Key: WarehouseCode
  #     Description: Código de Almacén
  #     Type: Text
  #     Query: OWHS.WhsCode
  #
  # Vendors:
  #   - Key: VendorCode
  #     Description: Código de Proveedor
  #     Type: Text
  #     Query: OCRD.CardCode
```

## Ejemplo Completo

A continuación se muestra una configuración completa y anotada de `CustomFields.yaml`:

```yaml
# Configuración de Campos Personalizados
# Define campos personalizados que se pueden mostrar y usar en diferentes módulos
# Se asigna a Dictionary<string, CustomField[]> en Settings.CustomFields

CustomFields:
  # Campos personalizados de Items - Campos disponibles para operaciones relacionadas con artículos
  Items:
    # Nombre Secundario - una referencia de columna simple
    - Key: FrgnName
      Description: Nombre Secundario
      Type: Text
      Query: OITM.FrgnName
      # GroupBy es opcional; omitido aquí ya que no se necesita agregación

    # Grupo de Artículos - utiliza una subconsulta y agrupación para resultados agregados
    - Key: ItmsGrpNam
      Description: Grupo de Artículos
      Type: Text
      Query: (SELECT [ItmsGrpNam] FROM OITB WHERE ItmsGrpCod = OITM.ItmsGrpCod)
      GroupBy: OITM.ItmsGrpCod

    # Nombre de Firma - otra subconsulta con agrupación
    - Key: FirmName
      Description: Nombre de Firma
      Type: Text
      Query: (SELECT [FirmName] FROM OMRC WHERE FirmCode = OITM.FirmCode)
      GroupBy: OITM.FirmCode

    # Campo numérico - identificador de entrada de artículo
    - Key: DocEntry
      Description: Entrada de Artículo
      Type: Number
      Query: OITM.DocEntry

    # Campo de fecha - marca de tiempo de última actualización
    - Key: UpdateDate
      Description: Fecha de Actualización
      Type: Date
      Query: OITM.UpdateDate

    # Volumen en metros cúbicos
    - Key: Volume
      Description: Volumen (m³)
      Type: Number
      Query: OITM.BVolume

    # Peso en kilogramos
    - Key: Weight
      Description: Peso (kg)
      Type: Number
      Query: OITM.BVolume

    # Factor de compra
    - Key: Factor
      Description: Factor
      Type: Number
      Query: OITM.PurFactor1

  # Campos personalizados de PickingDetails
  PickingDetails:
    # Campo Marca con lógica de fallback COALESCE y agrupación
    - Key: Marca
      Description: Marca
      Type: Text
      Query: COALESCE(ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA)
      GroupBy: ORDR.U_MARCA, OINV.U_MARCA, OWTQ.U_MARCA

  # Descomenta para agregar campos personalizados para Almacenes
  # Warehouses:
  #   - Key: WarehouseCode
  #     Description: Código de Almacén
  #     Type: Text
  #     Query: OWHS.WhsCode
  #
  #   - Key: WarehouseName
  #     Description: Nombre de Almacén
  #     Type: Text
  #     Query: OWHS.WhsName

  # Descomenta para agregar campos personalizados para Proveedores
  # Vendors:
  #   - Key: VendorCode
  #     Description: Código de Proveedor
  #     Type: Text
  #     Query: OCRD.CardCode
  #
  #   - Key: VendorName
  #     Description: Nombre de Proveedor
  #     Type: Text
  #     Query: OCRD.CardName
  #
  #   - Key: CreditLimit
  #     Description: Límite de Crédito
  #     Type: Number
  #     Query: OCRD.CreditLine
```

## Validación

- Asegúrate de que todos los valores de `Key` sean únicos dentro de cada colección.
- Verifica que las expresiones SQL en `Query` y `GroupBy` sean válidas para tu base de datos.
- Confirma que las tablas y columnas referenciadas existan en tu esquema de base de datos SAP.
