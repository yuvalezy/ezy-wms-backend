---
title: Metadatos de Artículos (Item.yaml)
description: Referencia para configurar definiciones de campos de metadatos de artículos en EZY WMS.
---

> **Nota para la versión actual:** La configuración activa se guarda en la base de datos y se administra desde Configuración para superusuarios. Los archivos YAML en `Service/init` son plantillas iniciales; no son la fuente de configuración en tiempo de ejecución.

Este documento describe cómo configurar definiciones de campos de metadatos para artículos en el archivo de configuración de EZY WMS.

## Descripción general

La plantilla de inicialización/migración `Item.yaml` está ubicada en
`ezy-wms-backend/Service/init/Item.yaml`. La sección `Item` almacenada en la base
de datos es la fuente en ejecución y define campos de metadatos que pueden
editarse, validarse y calcularse dentro de EZY WMS.

La configuración utiliza un formato estructurado bajo `Item.MetadataDefinition`, que es un array de definiciones de campos. Cada definición se asigna a un campo de artículo compatible de SAP y especifica reglas de validación, tipos de datos y cálculos opcionales.

## Estructura de definición de campo

Cada definición de campo de metadatos contiene las siguientes propiedades:

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `Id` | String | Sí | Identificador único del campo. Debe coincidir con el nombre de un campo de artículo compatible de SAP. |
| `Description` | String | Sí | Etiqueta legible que se muestra en la interfaz de usuario para los usuarios finales. |
| `Type` | Enum | Sí | Tipo de dato para validación y renderizado. Valores permitidos: `String`, `Decimal`, `Date`, `Integer`. |
| `ReadOnly` | Boolean | No | Indica si el campo no puede editarse después de su creación. Predeterminado: `false`. Nota: `ItemCode` e `ItemName` siempre son de solo lectura. |
| `Required` | Boolean | No | Indica si el campo debe tener un valor durante actualizaciones. Predeterminado: `false`. No puede ser true si `ReadOnly` es true. |
| `Step` | Integer | No | Incremento de paso para campos decimales en la interfaz de usuario. Solo se aplica cuando `Type` es `Decimal`. |
| `MirrorTo` | String | No | Copia el valor del campo a otro campo automáticamente. Ejemplo: `PurchaseUnitLength` → `SalesUnitLength`. |
| `ScaleByField` | String | No | Campo SAP usado como factor de escala en la frontera con SAP (ej. `PurchaseQtyPerPackUnit`). Al cargar: valor SAP × factor. Al guardar: valor ÷ factor. No se aplica a valores guardados en un UDF JSON. |
| `StoreIn` | String | No | Nombre de un UDF de texto del artículo SAP (ej. `U_EZY_WMS_DATA`) que contiene un objeto JSON. El valor se guarda con la clave `Id` dentro de ese JSON en lugar del campo SAP. Ver [Almacenamiento JSON](#almacenamiento-json-storein). |
| `SyncToSap` | Boolean | No | Solo con `StoreIn`. Escribe además el valor en el campo SAP llamado `Id` (escalado y reflejado). Predeterminado: `false`. |
| `ClearSapField` | Boolean | No | Solo con `StoreIn` (no con `SyncToSap`). En cada guardado escribe `0` en el campo SAP llamado `Id` y en su campo `MirrorTo`. Necesario para dimensiones — ver abajo. Predeterminado: `false`. |
| `Hidden` | Boolean | No | Nunca se envía al frontend (no se muestra ni se edita) pero participa en la escritura a SAP. Predeterminado: `false`. |
| `FixedValue` | String | No | Constante (interpretada según `Type`) que se escribe en SAP en cada guardado. Los campos con valor fijo nunca son editables. |
| `Query` | String | No | Fragmento SQL opcional para recuperar datos adicionales. |
| `GroupBy` | String | No | Expresión SQL opcional para agrupar resultados de consultas. |
| `Calculated` | Object | No | Configuración para campos calculados con fórmulas. Ver sección [Campos calculados](#campos-calculados). |

## Reglas de validación

- **Validación del nombre del campo**: SAP valida los nombres de campos. Solo se aceptan campos de artículos compatibles en SAP.
- **Restricción de solo lectura**: `Required` no puede ser true cuando `ReadOnly` es true.
- **Propiedad Step**: La propiedad `Step` solo se aplica a campos con `Type: Decimal`.
- **Campos calculados**: Cuando un campo tiene una definición `Calculated`, su valor se calcula automáticamente según las dependencias.
- **Campos integrados de solo lectura**: `ItemCode` e `ItemName` siempre son de solo lectura independientemente de la configuración.

## Almacenamiento JSON (`StoreIn`)

Algunos campos SAP no pueden contener los valores que WMS necesita. Las dimensiones del artículo son el
caso típico: WMS trabaja con dimensiones por bulto, SAP guarda dimensiones por unidad con 6 decimales y
deriva el volumen a partir de ellas — al dividir cada dimensión por la cantidad por bulto el volumen
derivado se redondea a `0`.

Con `StoreIn`, el campo vive dentro de un objeto JSON contenido en un UDF de texto (memo) del artículo:

- **Carga**: gana la entrada del JSON; si el JSON no tiene entrada se lee el campo SAP llamado `Id` como
  respaldo (escalado por `ScaleByField`), así los artículos existentes conservan sus valores hasta el
  primer guardado.
- **Guardado**: el valor va al JSON (sin escalar, tal como se ingresó). El campo SAP llamado `Id` **no**
  se escribe salvo que `SyncToSap` sea `true`.
- Las claves desconocidas ya presentes en el JSON se conservan.
- El UDF debe existir en `OITM`; la verificación de estado del sistema lo reporta si falta.
- **SAP deriva el volumen de largo × ancho × alto e ignora un volumen enviado mientras alguna dimensión
  sea distinta de cero.** Por eso los campos de dimensión guardados en JSON necesitan `ClearSapField: true`,
  para que sus copias en SAP (compra y venta) se pongan en cero en el mismo PATCH que escribe el volumen.

Configuración típica de dimensiones — dimensiones solo en JSON, volumen calculado en el backend,
guardado en JSON **y** escrito en SAP (÷ cantidad por bulto), más un código de unidad de volumen fijo y
oculto:

```yaml
    - Id: PurchaseUnitLength
      Description: Largo unidad
      Type: Decimal
      Step: 6
      StoreIn: U_EZY_WMS_DATA
      ScaleByField: PurchaseQtyPerPackUnit   # solo para la lectura de respaldo desde SAP
      ClearSapField: true                    # pone en cero PurchaseUnitLength + SalesUnitLength en SAP
      MirrorTo: SalesUnitLength
    # PurchaseUnitWidth / PurchaseUnitHeight idénticos
    - Id: PurchaseUnitVolume
      Description: Volumen unidad (m³)
      Type: Decimal
      Step: 6
      ReadOnly: true
      StoreIn: U_EZY_WMS_DATA
      SyncToSap: true
      ScaleByField: PurchaseQtyPerPackUnit
      MirrorTo: SalesUnitVolume
      Calculated:
        Formula: "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}"
        Dependencies: [PurchaseUnitLength, PurchaseUnitWidth, PurchaseUnitHeight]
        Precision: 6
        ClearDependenciesOnManualEdit: true
    - Id: PurchaseVolumeUnit          # OITM.BVolUnit — 4 = m³, 6 = ft³
      Description: Unidad de volumen
      Type: Integer
      ReadOnly: true
      Hidden: true
      FixedValue: "4"
      MirrorTo: SalesVolumeUnit
```

Los campos calculados se evalúan en el backend cada vez que se guarda una de sus dependencias; el valor
enviado por el cliente no se considera confiable.

## Campos reflejados

Los campos reflejados copian automáticamente valores a campos relacionados. Esto es útil para mantener consistencia entre unidades de compra y venta.

Ejemplo: Si estableces `MirrorTo: SalesUnitLength` en `PurchaseUnitLength`, el valor de la longitud de la unidad de compra se copia automáticamente al campo de longitud de la unidad de venta.

## Campos calculados

Los campos calculados calculan sus valores utilizando una fórmula basada en otros campos. Soportan expresiones complejas con múltiples dependencias.

### Propiedades de campos calculados

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Formula` | String | Expresión que calcula el valor del campo. Utiliza la sintaxis `{FieldName}` para marcadores de posición. Requerido. |
| `Dependencies` | Array | Lista de IDs de campos de los que depende este campo calculado. Requerido. |
| `Precision` | Integer | Número de decimales para redondear el resultado. Predeterminado: `0`. |
| `ClearDependenciesOnManualEdit` | Boolean | Cuando es true, los campos de dependencia se borran si el campo calculado se edita manualmente. Predeterminado: `false`. |

### Ejemplo: Cálculo de volumen

Un campo de volumen puede calcularse como el producto de largo, ancho y alto:

```yaml
- Id: PurchaseUnitVolume
  Description: Unit Volume (m³)
  Type: Decimal
  Step: 4
  ReadOnly: false
  Required: false
  MirrorTo: SalesUnitVolume
  Calculated:
    Formula: "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}"
    Dependencies:
      - PurchaseUnitLength
      - PurchaseUnitWidth
      - PurchaseUnitHeight
    Precision: 4
    ClearDependenciesOnManualEdit: true
```

## Ejemplo completo anotado

A continuación se presenta un ejemplo de configuración completa con casos de uso comunes:

```yaml
Item:
  MetadataDefinition:
    # Campos de identificador del sistema (siempre de solo lectura)
    - Id: ItemCode
      Description: Item Code
      Type: String
      ReadOnly: true
      Required: false

    - Id: ItemName
      Description: Item Name
      Type: String
      ReadOnly: true
      Required: false

    # Campos dimensionales con reflejo a unidades de venta
    - Id: PurchaseUnitLength
      Description: Unit Length
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitLength

    - Id: PurchaseUnitWidth
      Description: Unit Width
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitWidth

    - Id: PurchaseUnitHeight
      Description: Unit Height
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitHeight

    # Campo calculado que depende de dimensiones
    - Id: PurchaseUnitVolume
      Description: Unit Volume (m³)
      Type: Decimal
      Step: 4
      ReadOnly: false
      Required: false
      MirrorTo: SalesUnitVolume
      Calculated:
        Formula: "{PurchaseUnitLength} * {PurchaseUnitWidth} * {PurchaseUnitHeight}"
        Dependencies:
          - PurchaseUnitLength
          - PurchaseUnitWidth
          - PurchaseUnitHeight
        Precision: 4
        ClearDependenciesOnManualEdit: true

    # Campo editable requerido
    - Id: PurchaseUnitWeight
      Description: Unit Weight (KG)
      Type: Decimal
      ReadOnly: false
      Required: true
      MirrorTo: SalesUnitWeight

    # Campo personalizado definido por el usuario de SAP
    - Id: U_B1SStdTP
      Description: Template Name
      Type: String
      ReadOnly: false
      Required: true

    # Campos de seguimiento de auditoría
    - Id: U_LW_UPDATE_TIMESTAMP
      Description: Last Update Time
      Type: Date
      ReadOnly: false
      Required: false

    - Id: U_LW_UPDATE_USER
      Description: Last Update ID
      Type: Integer
      ReadOnly: false
      Required: false
```

## Mejores prácticas

- **Mantener campos requeridos mínimos**: Marca solo los campos esenciales para la lógica empresarial como requeridos.
- **Usar reflejos para consistencia**: Utiliza `MirrorTo` para mantener sincronizados los campos de compra y venta relacionados.
- **Validar dependencias**: Asegúrate de que todos los campos referenciados en una fórmula calculada existan y estén definidos antes del campo calculado.
- **Establecer precisión apropiada**: Para campos decimales calculados, establece `Precision` según tus requisitos empresariales.
- **Usar etiquetas descriptivas**: Escribe valores claros de `Description`; estos aparecen a los usuarios finales en la interfaz de usuario.
