---
title: Configuración de Paquetes
description: Configura el etiquetado, generación de códigos de barras y metadatos de paquetes en EZY WMS.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

El archivo de configuración `Package.yaml` define cómo se etiquetan los paquetes, se generan códigos de barras y se enriquecen con campos de metadatos personalizados. Esta guía cubre las tres subsecciones principales: Label, Barcode y MetadataDefinition.

## Label

La sección `Label` controla el comportamiento de impresión automática cuando se crean paquetes.

### AutoPrint

- **Tipo:** Booleano
- **Predeterminado:** `false`
- **Descripción:** Cuando se establece en `true`, las etiquetas de paquete se imprimen automáticamente inmediatamente después de la creación del paquete. Requiere una impresora configurada y una plantilla de etiqueta.

## Barcode

La sección `Barcode` define el formato y las reglas de generación para códigos de barras de paquetes.

### Prefix

- **Tipo:** Cadena
- **Predeterminado:** `""` (vacío)
- **Descripción:** Un prefijo fijo que se antepone a todos los códigos de barras generados. Se utiliza comúnmente para identificar el tipo de código de barras (por ejemplo, `PKG` para paquetes).

### Length

- **Tipo:** Entero
- **Predeterminado:** Requerido (sin valor predeterminado)
- **Descripción:** La longitud total del código de barras generado, incluyendo prefijo y sufijo. La porción numérica se rellena con ceros para llenar el espacio restante.

### Suffix

- **Tipo:** Cadena
- **Predeterminado:** `""` (vacío)
- **Descripción:** Un sufijo fijo que se añade al final de todos los códigos de barras generados. Puede usarse para dígitos de verificación o identificación adicional.

### StartNumber

- **Tipo:** Entero
- **Predeterminado:** `1`
- **Descripción:** El primer número en la secuencia de códigos de barras. Útil cuando se migra desde un sistema existente y se continúa una secuencia anterior.

### Regla de Generación

Los códigos de barras se generan utilizando el patrón:

```
[Prefix][NúmeroRelleno][Suffix]
```

El `NúmeroRelleno` se rellena con ceros para asegurar que la longitud total del código de barras sea igual al valor configurado en `Length`.

### Ejemplos

Dado `Length=14`, `Prefix="PKG"`, `Suffix=""` y `StartNumber=1`:

- **Primer paquete:** `PKG00000000001` (11 ceros entre prefijo y número)
- **Paquete 100:** `PKG00000000100`
- **Paquete 10,000:** `PKG00010000000`

Otro ejemplo con `Length=12`, `Prefix="A"`, `StartNumber=99`:

- **Primer paquete:** `A00000000099`
- **Segundo paquete:** `A00000000100`

## MetadataDefinition

La sección `MetadataDefinition` es un arreglo de campos de metadatos personalizados. Cada campo se utiliza durante la creación del paquete y la entrada de datos, y se valida según los estándares de integración con SAP B1.

### Propiedades del Campo

Cada definición de campo de metadatos admite las siguientes propiedades:

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `Id` | Cadena | Sí | Identificador único del campo dentro de los metadatos del paquete. No puede duplicarse. |
| `Description` | Cadena | Sí | Etiqueta legible por humanos mostrada en formularios y reportes de la interfaz. |
| `Type` | Enumeración | Sí | Tipo de dato para validación y renderizado de la interfaz: `String`, `Decimal`, `Date` o `Integer`. |
| `Step` | Decimal | No | Incremento de paso para campos de tipo `Decimal` (por ejemplo, `0.01` para moneda). |
| `Required` | Booleano | No | Si el campo debe completarse durante la creación del paquete. Predeterminado: `false`. |
| `ReadOnly` | Booleano | No | Si el campo es de solo lectura y no puede editarse. Predeterminado: `false`. |
| `Query` | Cadena | No | Consulta SQL para recuperar datos de desplegables o búsquedas. |
| `GroupBy` | Cadena | No | Expresión SQL para agrupar resultados de consultas. |
| `Calculated` | Objeto | No | Configuración para campos calculados o derivados. |

### Campos de Ejemplo

La configuración incluye tres campos de ejemplo:

**Volume** — Un campo decimal para volumen de paquete:
```yaml
- Type: Decimal
  Id: Volume
  Description: "Volume (m³)"
```

**Note** — Un campo de texto para notas especiales:
```yaml
- Type: String
  Id: Note
  Description: "Special Notes"
```

**ExpiryDate** — Un campo de fecha para seguimiento de expiración:
```yaml
- Type: Date
  Id: ExpiryDate
  Description: "Expiry Date"
```

## Notas

- **AutoPrint** requiere una impresora configurada y una plantilla de etiqueta en tu configuración de WMS. Verifica la conectividad de la impresora antes de habilitar.
- **La configuración de código de barras** debe finalizarse antes de crear el primer paquete. Cambiar prefijo, longitud o sufijo después de que existan paquetes puede causar conflictos o problemas de duplicación de códigos de barras.
- **Los campos de metadatos** se validan contra tu sistema SAP B1 durante la integración. Asegúrate de que las definiciones de campos personalizados se alineen con tu esquema SAP.
- La **unicidad del código de barras está garantizada** por el sistema; el parámetro `StartNumber` solo debe ajustarse durante la migración del sistema o la configuración inicial.

## Ejemplo Completo de Configuración

A continuación se muestra un ejemplo completamente anotado de un archivo `Package.yaml`:

```yaml
# Package Configuration
# Defines package labeling, barcode generation, and metadata settings
# Maps to Settings.Package in the application

Package:
  # Label configuration for package printing
  Label:
    # Automatically print labels when packages are created.
    # Requires printer configuration and label templates.
    AutoPrint: true
  
  # Barcode generation configuration for packages
  Barcode:
    # Prefix for all package barcodes (identifies barcode type)
    Prefix: "PKG"
    
    # Total barcode length including prefix and suffix
    # Numeric portion is zero-padded to reach this length
    # Example: with Prefix="PKG" and Length=14, first barcode is "PKG00000000001"
    Length: 14
    
    # Suffix appended to all barcodes (optional, can be empty)
    Suffix: ""
    
    # Starting number for the sequence (useful for migrations)
    # Each new package increments from this value
    StartNumber: 1
  
  # Metadata field definitions for packages
  # Array of custom fields validated during data entry
  MetadataDefinition:
    # Example: Volume measurement
    - Type: Decimal
      Id: Volume
      Description: "Volume (m³)"
      Step: 0.01          # Optional: step increment for UI spinners
      Required: false     # Optional: field is optional during entry
      ReadOnly: false     # Optional: field is editable
    
    # Example: Special notes or comments
    - Type: String
      Id: Note
      Description: "Special Notes"
      Required: false
      ReadOnly: false
    
    # Example: Product expiry tracking
    - Type: Date
      Id: ExpiryDate
      Description: "Expiry Date"
      Required: true      # Optional: field is mandatory
      ReadOnly: false

# File location: ezy-wms-backend/Service/config/Package.yaml
```

---

**Ubicación del archivo de configuración:** `ezy-wms-backend/Service/config/Package.yaml`
