---
title: Package Configuration (legacy)
description: Historical Package.yaml reference; package configuration is no longer a standalone WMS configuration section.
---

> **Current WMS status:** `Package.yaml` is not a current configuration template
> or catalogued database section. This page is retained to identify an obsolete
> source-wiki reference.

Current package-related settings are part of picking configuration and are managed
from the super-user Configuration screen:

| Setting | Effect |
| --- | --- |
| `Options:EnablePickingPackageLabels` | Enables package-label selection and creation in picking |
| `Options:PickingPackageLabelPrefix` | Prefix for generated picking package labels |
| `Options:EnablePostPickRepack` | Enables the supervisor repack workflow |

Use [Backend Configuration](index), [Picking Process](/docs/picking/process), and
[Picking Supervisor](/docs/picking/supervisor) for the current implementation.
